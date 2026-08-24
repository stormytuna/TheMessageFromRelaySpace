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
}
