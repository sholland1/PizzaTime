namespace Hollandsoft.OrderPizza;
public static partial class Utils {
    //TODO: Implement word wrap
    //      I need to make changes to the string and position in the string,
    //      then write the wrapped string
    public static string EditLine(string lineToEdit, int? maxWidth = null) {
        var (hOffset, vOffset) = Console.GetCursorPosition();
        var stringIndex = lineToEdit.Length;
        var width = maxWidth ?? Console.WindowWidth;

        List<char> chars = new();
        if (!string.IsNullOrEmpty(lineToEdit)) {
            chars.AddRange(lineToEdit.ToCharArray());
        }

        WriteCharsAfter();
        SetCharsAfter();

        while (true) {
            var info = Console.ReadKey(true);
            if (info.Key == ConsoleKey.Enter) {
                Console.WriteLine();
                break;
            }

            if ((info.Modifiers & ConsoleModifiers.Control) != 0) {
                if (info.Key == ConsoleKey.LeftArrow && !AtBeginning()) {
                    //TODO: Move cursor to start of previous word
                }
                else if (info.Key == ConsoleKey.RightArrow && !AtEnd()) {
                    //TODO: Move cursor to start of next word
                }
                else if (info.Key == ConsoleKey.Home) {
                    stringIndex = 0;
                }
                else if (info.Key == ConsoleKey.End) {
                    stringIndex = chars.Count;
                }
            }
            else if (info.Key == ConsoleKey.Backspace && !AtBeginning()) {
                stringIndex--;
                chars.RemoveAt(stringIndex);
                WriteCharsAfter();
            }
            else if (info.Key == ConsoleKey.Delete && !AtEnd()) {
                chars.RemoveAt(stringIndex);
                WriteCharsAfter();
            }
            else if (info.Key == ConsoleKey.UpArrow && !AtTop()) {
                stringIndex = Math.Max(stringIndex - width, 0);
            }
            else if (info.Key == ConsoleKey.DownArrow && !AtBottom()) {
                stringIndex = Math.Min(stringIndex + width, chars.Count);
            }
            else if (info.Key == ConsoleKey.LeftArrow && !AtBeginning()) {
                stringIndex--;
            }
            else if (info.Key == ConsoleKey.RightArrow && !AtEnd()) {
                stringIndex++;
            }
            else if (info.Key == ConsoleKey.Home) {
                stringIndex = BeginningOfLine();
            }
            else if (info.Key == ConsoleKey.End) {
                stringIndex = EndOfLine();
            }
            else if (!char.IsControl(info.KeyChar)) {
                chars.Insert(stringIndex, info.KeyChar);
                stringIndex++;
                WriteCharsAfter();
            }

            SetCharsAfter();
        }

        return new(chars.ToArray());

        bool AtBeginning() => stringIndex == 0;
        bool AtEnd() => stringIndex >= chars.Count;

        bool AtTop() {
            var firstLineCharCount = width - hOffset - 1;
            return stringIndex <= firstLineCharCount;
        }
        bool AtBottom() {
            var firstLineCharCount = width - hOffset - 1;
            var rem = (hOffset + chars.Count) % width;
            return chars.Count <= firstLineCharCount
                || stringIndex <= chars.Count && stringIndex >= chars.Count - rem;
        }

        int BeginningOfLine() {
            if (AtTop()) return 0;
            var rem = (hOffset + stringIndex) % width;
            return stringIndex - rem;
        };
        int EndOfLine() {
            if (AtTop()) {
                return Math.Min(chars.Count, width - hOffset - 1);
            }
            if (AtBottom()) {
                return chars.Count;
            }
            var rem = (hOffset + stringIndex) % width;
            return stringIndex - rem + width - 1;
        }

        void SetCharsAfter() {
            var div = Math.DivRem(hOffset + stringIndex, width, out var rem);
            Console.SetCursorPosition(rem, div + vOffset);
        }

        //FIXME: Only write chars after cursor
        void WriteCharsAfter() {
            Console.SetCursorPosition(hOffset, vOffset);
            if (chars.Count - 1 + hOffset < width) {
                Console.WriteLine(new string(chars.ToArray()).PadRight(width - hOffset, ' '));
                Console.Write(' ');
                return;
            }
            var wrapped = new[] { new string(chars.ToArray()) }.Wrap(width, firstLineOffset: hOffset).ToList();
            wrapped[^1] = wrapped[^1].PadRight(width, ' ');
            foreach (var line in wrapped) {
                Console.WriteLine(line);
            }
            Console.Write(' ');
        }
    }
}
