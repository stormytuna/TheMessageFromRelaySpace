using HarmonyLib;
using UnityEngine;

namespace TMFRS.UI;

[HarmonyPatch]
public static class DictionaryPatch
{
	[HarmonyPatch(typeof(DictionaryEntry), nameof(DictionaryEntry.Configure))]
	[HarmonyPostfix]
	public static void FixOverlappingDictDefinitions(DictionaryEntry __instance) {
		var pos = __instance.wordLabel.transform.position;
		// TODO: Config option, let people have it dynamic or fixed width
		// Fixed width should adjust to the width of your widest signal, setting it here won't be good...
		int budgeAmount = __instance.idLabel.text.Length - 4;
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
