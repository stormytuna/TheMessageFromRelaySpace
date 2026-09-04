using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TRFDS.Helpers;
using UnityEngine;

namespace TRFDS.UI;

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

	[HarmonyPatch(typeof(UserDictionary), nameof(UserDictionary.LoadPopulate))]
	[HarmonyPrefix]
	public static void SortDictionaryKeys(ref Dictionary<int, string> lWordDict) {
		var keys = lWordDict.Keys.ToArray();	
		var terms = lWordDict.Values.ToArray();
		Array.Sort(keys, terms, new ReverseComparer());
		lWordDict = keys.Zip(terms, (k, v) => new {k, v}).ToDictionary(x => x.k, x => x.v);
	}
}

class ReverseComparer : IComparer
{
	int IComparer.Compare(object x, object y) {
		return Comparer.Default.Compare(y, x);
	}
}
