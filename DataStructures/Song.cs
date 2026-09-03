using System.Collections.Generic;
using System.Linq;

namespace TRFDS.DataStructures;

// Adapted, with permission, from electraminer's implementation
// https://github.com/electraminer/relay3544/tree/e0f8073786fad6b5e9c930dd207f52518a8db962
// To respect the intention written in electraminer's README, the code is not directly linked here
// It can be found under src/spoilers/Song.ts

public record struct MusicNote(double StartTime, double Duration, double Frequency);

public class Song
{
    private const double ASecondsToSeconds = 0.8069224;

    public List<MusicNote> Notes;

    private Song(List<MusicNote> notes) {
        this.Notes = notes;
    }

    public double TotalLength {
        get {
            if (Notes.Count == 0) {
				return 0.0;
			}

			return Notes.Max(x => x.StartTime + x.Duration);
        }
    }

    private static Song Combine(Song a, Song b) {
        var result = new List<MusicNote>(a.Notes.Count + b.Notes.Count);
        result.AddRange(a.Notes);
        result.AddRange(b.Notes);
        return new Song(result);
    }

    private Song Append(Song other) {
        var result = new List<MusicNote>(Notes.Count + other.Notes.Count);
        result.AddRange(Notes);

        foreach (var note in other.Notes) {
            result.Add(note with { StartTime = note.StartTime + TotalLength });
        }

        return new Song(result);
    }

    private Song ConvertUnits() {
        var result = new List<MusicNote>(Notes.Count);

        foreach (var note in Notes) {
            result.Add(note with {
                StartTime = note.StartTime * ASecondsToSeconds,
                Duration = note.Duration * ASecondsToSeconds,
                Frequency = note.Frequency / ASecondsToSeconds
            });
        }

        return new Song(result);
    }

    public static bool TryParse(int[] signals, out Song song) {
        song = null;

        for (int i = 0; i < signals.Length; i++) {
            if (signals[i] != -577) {
                continue;
			}

            int cursor = i + 1;
            var variables = new Dictionary<double, Song>();

            if (!TryParseGroup(signals, ref cursor, variables, out Song parsedSong)) {
                continue;
            }

            parsedSong = parsedSong.ConvertUnits();
            song = parsedSong;

            return true;
        }

        return false;
    }

    private static bool TryParseGroup(int[] signals, ref int cursor, Dictionary<double, Song> variables, out Song song) {
        song = null;

        int originalCursor = cursor;
        var originalVariables = new Dictionary<double, Song>(variables);

        if (cursor >= signals.Length) {
            return false;
		}

        int openBracket = signals[cursor];
        if (openBracket != -14 && openBracket != -140412 && openBracket != -140414) {
            return false;
        }

        cursor++;

        if (!TryParseSequence(signals, ref cursor, variables, out song)) {
            cursor = originalCursor;
            RestoreVariables(variables, originalVariables);
            return false;
        }

        int closeBracket = openBracket - 1;

        if (cursor >= signals.Length || signals[cursor] != closeBracket) {
            cursor = originalCursor;
            RestoreVariables(variables, originalVariables);
            return false;
        }

        cursor++;
        return true;
    }

	private static bool TryParseSequence(int[] signals, ref int cursor, Dictionary<double, Song> variables, out Song song) {
		song = null;

		if (!TryParseChord(signals, ref cursor, variables, out song)) {
			return false;
		}

		while (cursor < signals.Length && signals[cursor] == -122) {
			cursor++;

			if (!TryParseChord(signals, ref cursor, variables, out Song next)) {
				return true;
			}

			song = song.Append(next);
		}

		return true;
	}

	private static bool TryParseChord(int[] signals, ref int cursor, Dictionary<double, Song> variables, out Song song) {
		song = null;

		if (!TryParseItem(signals, ref cursor, variables, out song)) {
			return false;
		}

		while (true) {
			TryConsumeSignal(signals, ref cursor, -3);

			if (!TryParseItem(signals, ref cursor, variables, out Song next)) {
				return true;
			}

			song = Combine(song, next);
		}
	}

    private static bool TryParseItem(int[] signals, ref int cursor, Dictionary<double, Song> variables, out Song song) {
        song = null;

        int originalCursor = cursor;
        var originalVariables = new Dictionary<double, Song>(variables);

        if (TryParseNote(signals, ref cursor, out MusicNote note)) {
            song = new Song(new List<MusicNote> { note });
            return true;
        }

        cursor = originalCursor;

        if (TryParseVariableDeclaration(signals, ref cursor, variables, out song)) {
            return true;
        }

        cursor = originalCursor;
        RestoreVariables(variables, originalVariables);

        if (TryParseVariableReference(signals, ref cursor, variables, out song)) {
            return true;
        }

        cursor = originalCursor;
        RestoreVariables(variables, originalVariables);

        if (TryParseGroup(signals, ref cursor, variables, out song)) {
            return true;
        }

        cursor = originalCursor;
        RestoreVariables(variables, originalVariables);

        return false;
    }

    private static bool TryParseNote(int[] signals, ref int cursor, out MusicNote note) {
        note = default;

        int originalCursor = cursor;

        if (!TryConsumeSignal(signals, ref cursor, -605003)) {
            return false;
		}

        double time = 0;
        double length = 0;
        double frequency = 0;

        if (TryParseNumber(signals, ref cursor, out double parsed)) {
            time = parsed;
        }

        if (!TryConsumeSignal(signals, ref cursor, -3)) {
            cursor = originalCursor;
            return false;
        }

        if (TryParseNumber(signals, ref cursor, out parsed)) {
            length = parsed;
        }

        if (!TryConsumeSignal(signals, ref cursor, -3)) {
            cursor = originalCursor;
            return false;
        }

        if (TryParseNumber(signals, ref cursor, out parsed)) {
            frequency = parsed;
        }

        note = new MusicNote(time, length, frequency);
        return true;
    }

    private static bool TryParseVariableDeclaration(int[] signals, ref int cursor, Dictionary<double, Song> variables, out Song song) {
        song = null;

        int originalCursor = cursor;
        var originalVariables = new Dictionary<double, Song>(variables);

        if (!TryConsumeSignal(signals, ref cursor, -11)) {
            return false;
		}

        if (!TryParseNumber(signals, ref cursor, out double number)) {
            cursor = originalCursor;
            return false;
        }

        if (!TryParseGroup(signals, ref cursor, variables, out song)) {
            cursor = originalCursor;
            RestoreVariables(variables, originalVariables);
            return false;
        }

        if (variables.ContainsKey(number)) {
            cursor = originalCursor;
            RestoreVariables(variables, originalVariables);
            return false;
        }

        variables.Add(number, song);
        return true;
    }

    private static bool TryParseVariableReference(int[] signals, ref int cursor, Dictionary<double, Song> variables, out Song song) {
        song = null;

        int originalCursor = cursor;

        if (!TryConsumeSignal(signals, ref cursor, -11)) {
            return false;
		}

        if (!TryParseNumber(signals, ref cursor, out double number)) {
            cursor = originalCursor;
            return false;
        }

        if (!variables.TryGetValue(number, out song)) {
            cursor = originalCursor;
            return false;
        }

        return true;
    }

	private static bool TryParseNumber(int[] signals, ref int cursor, out double number) {
		number = 0;

		int originalCursor = cursor;
		string value = "";

		bool isNegative = TryConsumeSignal(signals, ref cursor, -1);

		while (cursor < signals.Length && signals[cursor] >= 0) {
			value += signals[cursor].ToString();
			cursor++;
		}

		if (TryConsumeSignal(signals, ref cursor, -10)) {
			value += ".";

			while (cursor < signals.Length && signals[cursor] >= 0) {
				value += signals[cursor].ToString();
				cursor++;
			}
		}

		if (!double.TryParse(value, out number)) {
			cursor = originalCursor;
			return false;
		}

		if (isNegative) {
			number = -number;
		}

		return true;
	}

    private static bool TryConsumeSignal(int[] signals, ref int cursor, int signal) {
        if (cursor >= signals.Length || signals[cursor] != signal) {
            return false;
        }

        cursor++;
        return true;
    }

    private static void RestoreVariables(Dictionary<double, Song> variables, Dictionary<double, Song> original) {
        variables.Clear();

        foreach (var pair in original) {
            variables.Add(pair.Key, pair.Value);
		}
    }
}
