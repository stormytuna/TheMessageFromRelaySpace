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
}
