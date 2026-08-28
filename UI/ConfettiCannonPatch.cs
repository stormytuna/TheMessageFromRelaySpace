using HarmonyLib;

namespace TRFDS.UI;

[HarmonyPatch]
public static class ConfettiCannonPatch
{
	[HarmonyPatch(typeof(ConfettiCannon), nameof(ConfettiCannon.FireConfetti))]
	[HarmonyPrefix]
	public static void ChangeConfettiColor(ConfettiCannon __instance) {
		if (TRFDSPlugin.MulticoloredConfetti.Value) {
			__instance.ConfettiMode = 1;
		} else {
			__instance.ConfettiMode = 0;
		}
	}
}

