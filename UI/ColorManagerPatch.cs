using HarmonyLib;
using UnityEngine;

namespace TMFRS.UI;

[HarmonyPatch]
public static class ColorManagerPatch
{
	[HarmonyPatch(typeof(ColorManager), nameof(ColorManager.ClearedID))]
	[HarmonyPrefix]
	public static void SetColorsByConfig(ref int id) {
		if (TMFRSPlugin.ConfigTheme.Value == ColorSetType.None) {
			return;
		}

		int chosenColors = (int)TMFRSPlugin.ConfigTheme.Value;
		if (TMFRSPlugin.ConfigTheme.Value == ColorSetType.Random) {
			chosenColors = Random.RandomRangeInt((int)ColorSetType.Visual, (int)ColorSetType.Random);
		}

		id = chosenColors;
	}
}

public enum ColorSetType : byte {
	None = 0,
	Visual,
	Atoms,
	Space,
	Life,
	AbstractDust,
	Vitality,
	Planet,
	ComplexCulture,
	Knowledge,
	RetroGreen,
	Random,
}
