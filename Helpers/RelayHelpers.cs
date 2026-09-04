using System.Linq;
using UnityEngine;

namespace TRFDS.Helpers;

public static class RelayHelpers
{
	public static int FindClosingBraceIndex(int[] signals, int startIndex) {
		int braceCount = 0;
		for (int i = startIndex; i < signals.Length; i++) {
			var sig = signals[i];
			if (sig == -14) {
				braceCount++;
			} else if (sig == -15) {
				braceCount--;
			}

			if (braceCount == 0) {
				return i;
			}
		}

		return -1;
	}

	public static void AddHotkey(GameObject obj, KeyCode modifier, KeyCode activator) {
		AddHotkey(obj.GetComponent<Button3D>(), modifier, activator);
	}

	public static void AddHotkey(Transform obj, KeyCode modifier, KeyCode activator) {
		AddHotkey(obj.GetComponent<Button3D>(), modifier, activator);
	}

	public static void AddHotkey(Button3D button, KeyCode modifier, KeyCode activator) {
		HotkeyManager.Instance.recipients = HotkeyManager.Instance.recipients.Append(new HotkeyRecipient {
			button = button.GetComponent<Button3D>(),
			modifierKey = modifier,
			activatorKey = activator,
		}).ToArray();
	}
}
