using System.Diagnostics;

namespace Hollandsoft.OrderPizza;

public static class Fzf {
    public class FzfOptions {
        #region Search
        public bool? Extended { get; init; }
        public bool Exact { get; init; }
        public bool? CaseSensitive { get; init; }
        public FzfScheme Scheme { get; init; }
        public bool Literal { get; init; }
        public FzfAlgo? Algo { get; init; }
        // public ??? Nth { get; init; }
        // public ??? WithNth { get; init; }
        // public ??? Delimiter { get; init; }
        public bool NoSort { get; init; }
        public bool Track { get; init; }
        public bool TAC { get; init; }
        public bool Disabled { get; init; }
        public FzfTieBreak[] TieBreak { get; init; } = { FzfTieBreak.Length };
        #endregion

        #region Interface
        public bool NoMouse { get; init; }
        public string? Bind { get; init; }
        public bool Cycle { get; init; }
        public bool KeepRight { get; init; }
        public int ScrollOff { get; init; }
        public bool NoHScroll { get; init; }
        public int HScrollOff { get; init; } = 10;
        public bool FilepathWord { get; init; }
        public string JumpLabels { get; init; } = "";
        #endregion

        #region Layout
        // public ??? Height { get; init; }
        // public ??? MinHeight { get; init; }
        public FzfLayout Layout { get; init; }
        public FzfBorder Border { get; init; }
        public string? BorderLabel { get; init; }
        // public ??? BorderLabelPos { get; init; }
        // public ??? Margin { get; init; }
        // public ??? Padding { get; init; }
        // public ??? Info { get; init; }
        public string? Separator { get; init; }
        public bool NoSeparator { get; init; }
        // public ??? ScrollBar { get; init; }
        public bool NoScrollBar { get; init; }
        public string Prompt { get; init; } = "> ";
        public string Pointer { get; init; } = "> ";
        public string Marker { get; init; } = "> ";
        public string? Header { get; init; }
        public int? HeaderLines { get; init; }
        public bool HeaderFirst { get; init; }
        public string Ellipsis { get; init; } = "..";
        #endregion

        #region Display
        public bool Ansi { get; init; }
        public int Tabstop { get; init; } = 8;
        //FIXME: No custom colors
        public FzfColor? Color { get; init; }
        public bool NoBold { get; init; }
        #endregion

        #region History
        public string? History { get; init; }
        public int HistorySize { get; init; } = 1000;
        #endregion

        #region Preview
        public string? Preview { get; init; }
        public string PreviewWindow { get; init; } = "right:50%";
        public string? PreviewLabel { get; init; }
        // public ??? PreviewLabelPos { get; init; }
        #endregion

        #region Scripting
        public string? Query { get; init; }
        public bool Select1 { get; init; }
        public bool Exit0 { get; init; }
        // public ??? Filter { get; init; }
        public bool PrintQuery { get; init; }
        public string Expect { get; init; } = "";
        public bool Read0 { get; init; }
        public bool Print0 { get; init; }
        public bool Sync { get; init; }
        // public int Listen { get; init; }
        #endregion

        public virtual IEnumerable<string> Arguments {
            get {
                #region Search
                if (Extended is bool x) yield return x ? "-x" : "+x";
                if (Exact) yield return "--exact";
                if (CaseSensitive is bool cs) yield return cs ? "+i" : "-i";
                if (Scheme != FzfScheme.Default) yield return $"--scheme={Scheme.ToString().ToLower()}";
                if (Literal) yield return "--literal";
                if (Algo is not null) yield return $"--algo={Algo.ToString()?.ToLower()}";
                if (NoSort) yield return "--no-sort";
                if (Track) yield return "--track";
                if (TAC) yield return "--tac";
                if (Disabled) yield return "--disabled";
                if (TieBreak.Length > 1 || TieBreak.FirstOrDefault() != FzfTieBreak.Length) {
                    yield return $"--tiebreak={string.Join(',', TieBreak.Select(t => t.ToString().ToLower()))}";
                }
                #endregion

                #region Interface
                if (NoMouse) yield return "--no-mouse";
                if (Bind is not null) yield return $"--bind={Bind}";
                if (Cycle) yield return "--cycle";
                if (KeepRight) yield return "--keep-right";
                if (ScrollOff != 0) yield return $"--scroll-off={ScrollOff}";
                if (NoHScroll) yield return "--no-hscroll";
                if (HScrollOff != 10) yield return $"--hscroll-off={HScrollOff}";
                if (FilepathWord) yield return "--filepath-word";
                if (JumpLabels != "") yield return $"--jump-labels={JumpLabels}";
                #endregion

                #region Layout
                if (Layout != FzfLayout.Default) yield return $"--layout={Layout.ToString().Replace('_', '-').ToLower()}";
                if (Border != FzfBorder.Rounded) yield return $"--border={Border.ToString().ToLower()}";
                if (BorderLabel is not null) yield return $"--border-label=\"{BorderLabel}\"";
                if (Separator is not null) yield return $"--separator=\"{Separator}\"";
                if (NoSeparator) yield return "--no-separator";
                if (NoScrollBar) yield return "--no-scrollbar";
                if (Prompt != "> ") yield return $"--prompt=\"{Prompt}\"";
                if (Pointer != "> ") yield return $"--pointer=\"{Pointer}\"";
                if (Marker != "> ") yield return $"--marker=\"{Marker}\"";
                if (Header is not null) yield return $"--header=\"{Header}\"";
                if (HeaderLines != null) yield return $"--header-lines={HeaderLines}";
                if (HeaderFirst) yield return "--header-first";
                if (Ellipsis != "..") yield return $"--ellipsis=\"{Ellipsis}\"";
                #endregion

                #region Display
                if (Ansi) yield return "--ansi";
                if (Tabstop != 8) yield return $"--tabstop={Tabstop}";
                if (Color != null) yield return $"--color={Color?.ToString()?.ToLower()}";
                if (NoBold) yield return "--no-bold";
                #endregion

                #region History
                if (History is not null) yield return $"--history=\"{History}\"";
                if (HistorySize != 1000) yield return $"--history-size={HistorySize}";
                #endregion

                #region Preview
                if (Preview is not null) yield return $"--preview=\"{Preview}\"";
                if (PreviewWindow != "right:50%") yield return $"--preview-window={PreviewWindow}";
                if (PreviewLabel is not null) yield return $"--preview-label=\"{PreviewLabel}\"";
                #endregion

                #region Scripting
                if (Query is not null) yield return $"--query=\"{Query}\"";
                if (PrintQuery) yield return "--print-query";
                if (Select1) yield return "-1";
                if (Exit0) yield return "-0";
                if (Expect != "") yield return $"--expect=\"{Expect}\"";
                if (Read0) yield return "--read0";
                if (Print0) yield return "--print0";
                if (Sync) yield return "--sync";
                #endregion
            }
        }
    }

    public static string? ChooseWithFzf(this IEnumerable<string> source, FzfOptions? options = null) =>
        ChooseImpl(source, (options ?? new()).Arguments).SingleOrDefault();

    public static List<string> ChooseMultiWithFzf(this IEnumerable<string> source, FzfOptions? options = null, int? maxSelect = null) =>
        ChooseImpl(source, (options ?? new()).Arguments.Prepend("--multi" + (maxSelect == null ? "" : $"={maxSelect}")));

    private static List<string> ChooseImpl(IEnumerable<string> source, IEnumerable<string> arguments) {
        List<string> items = new();
        try {
            Process p = new() {
                StartInfo = new("fzf", string.Join(' ', arguments)) {
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true
                }
            };

            p.OutputDataReceived += (_, e) => {
                if (e.Data == null) return;
                items.Add(e.Data);
            };

            p.Start();
            p.BeginOutputReadLine();

            foreach (var s in source) {
                p.StandardInput.WriteLine(s);
            }

            p.WaitForExit();
        }
        catch (IOException ex) when (ex.Message.Contains("Broken pipe")) {
        }
        return items;
    }

    public enum FzfColor { Dark, Light, Sixteen, BW }
    public enum FzfBorder { Rounded, Sharp, Bold, Block, Thinblock, Double, Horizontal, Vertical, Top, Bottom, Left, Right, None }
    public enum FzfLayout { Default, Reverse, Reverse_List }
    public enum FzfTieBreak { Length, Chunk, Begin, End, Index }
    public enum FzfScheme { Default, Path, History }
    public enum FzfAlgo { V1, V2 }
}
