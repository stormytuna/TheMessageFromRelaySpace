using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace TRFDS.UI;

// TODO: Save dictionary in signal order. New sigs get put at the bottom of the list and stay there, very sad

[HarmonyPatch]
public static class DictionaryPatch
{
	[HarmonyPatch(typeof(DictionaryEntry), nameof(DictionaryEntry.Configure))]
	[HarmonyPostfix]
	public static void FixOverlappingDictDefinitions(DictionaryEntry __instance) {
		int budgeAmount = 0;
		if (TRFDSPlugin.DictionaryDynamicBudge.Value) {
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

			var dictWindow = UnityHelpers.FindSingleInstanceObject<DictionaryWindow>();
			dictWindow.entries.Remove(__instance);
			dictWindow.entriesDict.Remove(__instance.id);
			dictWindow.totalEntries--;

			GameObject.DestroyImmediate(__instance.gameObject);
			dictWindow.dataEntryLayout.LayoutElements(true);

			UnityHelpers.FindSingleInstanceObject<Autosaver>().Autosave(PuzzleManager.Instance);

			return false;
		}

		return true;
	}
}
