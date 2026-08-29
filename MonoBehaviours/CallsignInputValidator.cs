using TMPro;

public class CallsignInputValidator : TMP_InputValidator
{
    public override char Validate(ref string text, ref int pos, char ch) {
		if (CharacterIsValid(ch) && WithinLengthLimit(text)) {
			text = text.Insert(pos, ch.ToString());
			pos++;
			return ch;
		}

		return '\0';
    }

	private bool WithinLengthLimit(string text) {
		return text.Length < 4;
	}

	private bool CharacterIsValid(char ch) {
		return (ch - '0') <= 7 && (ch - '0') >= 0;
	}
}
