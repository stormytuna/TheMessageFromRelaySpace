using HarmonyLib;
using UnityEngine;

namespace TMFRS.UI;

[HarmonyPatch]
public static class DictionaryPatch
{
	[HarmonyPatch(typeof(DictionaryEntry), nameof(DictionaryEntry.Configure))]
	[HarmonyPostfix]
	public static void FixOverlappingDictDefinitions(DictionaryEntry __instance) {
		int budgeAmount = 0;
		if (TMFRSPlugin.DictionaryDynamicBudge.Value) {
			budgeAmount = __instance.idLabel.text.Length - 4;
		} else {
			budgeAmount = 3;
		}

		var pos = __instance.wordLabel.transform.position;
		__instance.wordLabel.transform.position = new Vector3(pos.x + (budgeAmount * 0.0539f), pos.y, pos.z);
	}
	
	[HarmonyPatch(typeof(TermNameInputValidator), nameof(TermNameInputValidator.WithinLengthLimit))]
	[HarmonyPostfix]
	public static void AllowSlightlyLongerTermNames(ref bool __result, string text) {
		__result = text.Length < 18;
	}
	
	[HarmonyPatch(typeof(DictionaryEntry), nameof(DictionaryEntry.TrashName))]
	[HarmonyPrefix]
	public static bool FullyDeleteCustomSignals(DictionaryEntry __instance) {
		if ((__instance.id < -245 || __instance.id > -1) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))) {
			UserDictionary.Instance.RemoveEntry(__instance.id);
			GameObject.Destroy(__instance.gameObject);
			UnityHelpers.FindSingleInstanceObject<Autosaver>().Autosave(PuzzleManager.Instance);
			return false;
		}

		return true;
	}
}
