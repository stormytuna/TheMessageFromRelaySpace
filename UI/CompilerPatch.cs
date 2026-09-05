using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace TRFDS.UI;

[HarmonyPatch]
public static class CompilerPatch
{
	[HarmonyPatch(typeof(SignalCompiler), nameof(SignalCompiler.CompileStringToStringLines))]
	[HarmonyPostfix]
	public static void CollateSignalRepresentations(ref List<string> __result) {
		var newResult = new List<string>();
        for (int i = 0; i < __result.Count; i++) {
            string str = __result[i];
            string next = __result.ElementAtOrDefault(i + 1);
			string nextNext = __result.ElementAtOrDefault(i + 2);
			if (str == "|" && next == "-" && int.TryParse(nextNext, out var num)) {
				newResult.Add("-" + num);
				i += 2;
				continue;
			}

			newResult.Add(str);
        }

		__result = newResult;
	}

	[HarmonyPatch(typeof(C_WordCatcher), nameof(C_WordCatcher.CompileLine))]
	[HarmonyPrefix]
	public static bool AllowSignalRepresentations(string lineInput, ref List<int> __result) {
		if (lineInput.StartsWith('-') && int.TryParse(lineInput, out var num)) {
			__result = new List<int>() { num };
			return false;
		}

		return true;
	}
}
