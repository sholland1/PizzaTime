using System.Diagnostics;

namespace Hollandsoft.PizzaTime;

public static class Fzf {
    public class FzfOptions {
        #region Search Mode
        public bool? Extended { get; init; }
        public bool? Exact { get; init; }
        public bool? CaseSensitive { get; init; }
        public bool? Literal { get; init; }
        public FzfScheme? Scheme { get; init; }
        public FzfAlgo? Algo { get; init; }
        // public ??? Nth { get; init; }
        // public ??? WithNth { get; init; }
        // public ??? Delimiter { get; init; }
        public bool? Disabled { get; init; }
        #endregion

        #region Search Result
        public bool? NoSort { get; init; }
        public bool? Track { get; init; }
        public bool? TAC { get; init; }
        public FzfTieBreak[] TieBreak { get; init; } = Array.Empty<FzfTieBreak>();
        #endregion

        #region Interface
        public bool NoMouse { get; init; }
        public string? Bind { get; init; }
        public bool? Cycle { get; init; }
        public bool? KeepRight { get; init; }
        public int? ScrollOff { get; init; }
        public bool? NoHScroll { get; init; }
        public int? HScrollOff { get; init; }
        public bool? FilepathWord { get; init; }
        public string? JumpLabels { get; init; }
        #endregion

        #region Layout
        // public ??? Height { get; init; }
        // public ??? MinHeight { get; init; }
        public FzfLayout? Layout { get; init; }
        public bool? Reverse { get; init; }
        public FzfBorder? Border { get; init; }
        public string? BorderLabel { get; init; }
        // public ??? BorderLabelPos { get; init; }
        public bool? NoUnicode { get; init; }
        // public ??? Margin { get; init; }
        // public ??? Padding { get; init; }
        // public ??? Info { get; init; }
        // public bool NoInfo { get; init; }
        public string? Separator { get; init; }
        public bool NoSeparator { get; init; }
        // public ??? ScrollBar { get; init; }
        public bool NoScrollBar { get; init; }
        public string? Prompt { get; init; }
#pragma warning disable CA1720 // Identifier contains type name
        public string? Pointer { get; init; }
#pragma warning restore CA1720 // Identifier contains type name
        public string? Marker { get; init; }
        public string? Header { get; init; }
        public int? HeaderLines { get; init; }
        public bool? HeaderFirst { get; init; }
        public string? Ellipsis { get; init; }
        #endregion

        #region Display
        public bool? Ansi { get; init; }
        public int? Tabstop { get; init; }
        //FIXME: No custom colors
        public FzfColor? Color { get; init; }
        public bool? NoBold { get; init; }
        public bool? Black { get; init; }
        #endregion

        #region History
        public string? History { get; init; }
        public int? HistorySize { get; init; }
        #endregion

        #region Preview
        public string? Preview { get; init; }
        public string? PreviewLabel { get; init; }
        // public ??? PreviewLabelPos { get; init; }
        public string? PreviewWindow { get; init; }
        #endregion

        #region Scripting
        public string? Query { get; init; }
        public bool? Select1 { get; init; }
        public bool? Exit0 { get; init; }
        // public ??? Filter { get; init; }
        public bool? PrintQuery { get; init; }
        public string? Expect { get; init; }
        public bool? Read0 { get; init; }
        public bool? Print0 { get; init; }
        public bool? NoClear { get; init; }
        public bool? Sync { get; init; }
        // public int Listen { get; init; }
        #endregion

        public IEnumerable<string> Arguments {
            get {
                #region Search Mode
                if (Extended is bool extended) yield return extended ? "-x" : "+x";
                if (Exact is bool exact) yield return exact ? "--exact" : "--no-exact";
                if (CaseSensitive is bool cs) yield return cs ? "+i" : "-i";
                if (Literal is bool l) yield return l ? "--literal" : "--no-literal";
                if (Scheme is not null) yield return $"--scheme={Scheme.ToString()?.ToLowerInvariant()}";
                if (Algo is not null) yield return $"--algo={Algo.ToString()?.ToLowerInvariant()}";
                if (Disabled is bool d) yield return d ? "--disabled" : "--enabled";
                #endregion

                #region Search Result
                if (NoSort is bool ns) yield return ns ? "--no-sort" : "--sort";
                if (Track is bool track) yield return track ? "--track" : "--no-track";
                if (TAC is bool tac) yield return tac ? "--tac" : "--no-tac";
                if (TieBreak.Length != 0)
                    yield return $"--tiebreak={string.Join(',', TieBreak.Select(t => t.ToString().ToLowerInvariant()))}";
                #endregion

                #region Interface
                if (NoMouse) yield return "--no-mouse";
                if (Bind is not null) yield return $"--bind={Bind}";
                if (Cycle is bool c) yield return c ? "--cycle" : "--no-cycle";
                if (KeepRight is bool kr) yield return kr ? "--keep-right" : "--no-keep-right";
                if (ScrollOff is not null) yield return $"--scroll-off={ScrollOff}";
                if (NoHScroll is bool nhs) yield return nhs ? "--no-hscroll" : "--hscroll";
                if (HScrollOff is not null) yield return $"--hscroll-off={HScrollOff}";
                if (FilepathWord is bool fpw) yield return fpw ? "--filepath-word" : "--no-filepath-word";
                if (JumpLabels is not null) yield return $"--jump-labels={JumpLabels}";
                #endregion

                #region Layout
                if (Layout is not null) yield return $"--layout={Layout.ToString()?.Replace('_', '-').ToLowerInvariant()}";
                if (Reverse is bool r) yield return r ? "--reverse" : "--no-reverse";
                if (Border is not null) yield return $"--border={Border.ToString()?.ToLowerInvariant()}";
                if (BorderLabel is not null) yield return $"--border-label=\"{BorderLabel}\"";
                if (NoUnicode is bool nu) yield return nu ? "--no-unicode" : "--unicode";
                if (Separator is not null) yield return $"--separator=\"{Separator}\"";
                if (NoSeparator) yield return "--no-separator";
                if (NoScrollBar) yield return "--no-scrollbar";
                if (Prompt is not null) yield return $"--prompt=\"{Prompt}\"";
                if (Pointer is not null) yield return $"--pointer=\"{Pointer}\"";
                if (Marker is not null) yield return $"--marker=\"{Marker}\"";
                if (Header is not null) yield return $"--header=\"{Header}\"";
                if (HeaderLines is not null) yield return $"--header-lines={HeaderLines}";
                if (HeaderFirst is bool hf) yield return hf ? "--header-first" : "--no-header-first";
                if (Ellipsis is not null) yield return $"--ellipsis=\"{Ellipsis}\"";
                #endregion

                #region Display
                if (Ansi is bool a) yield return a ? "--ansi" : "--no-ansi";
                if (Tabstop is not null) yield return $"--tabstop={Tabstop}";
                if (Color is not null) yield return $"--color={Color.ToString()?.Replace("_", "").ToLowerInvariant()}";
                if (NoBold is bool nb) yield return nb ? "--no-bold" : "--bold";
                if (Black is bool b) yield return b ? "--black" : "--no-black";
                #endregion

                #region History
                if (History is not null) yield return $"--history=\"{History}\"";
                if (HistorySize is not null) yield return $"--history-size={HistorySize}";
                #endregion

                #region Preview
                if (Preview is not null) yield return $"--preview=\"{Preview}\"";
                if (PreviewLabel is not null) yield return $"--preview-label=\"{PreviewLabel}\"";
                if (PreviewWindow is not null) yield return $"--preview-window={PreviewWindow}";
                #endregion

                #region Scripting
                if (Query is not null) yield return $"--query=\"{Query}\"";
                if (Select1 is bool s1) yield return s1 ? "-1" : "+1";
                if (Exit0 is bool e0) yield return e0 ? "-0" : "+0";
                if (PrintQuery is bool pq) yield return pq ? "--print-query" : "--no-print-query";
                if (Expect is not null) yield return $"--expect=\"{Expect}\"";
                if (Read0 is bool r0) yield return r0 ? "--read0" : "--no-read0";
                if (Print0 is bool p0) yield return p0 ? "--print0" : "--no-print0";
                if (NoClear is bool nc) yield return nc ? "--no-clear" : "--clear";
                if (Sync is bool sync) yield return sync ? "--sync" : "--no-sync";
                #endregion
            }
        }
    }

    public static string? ChooseWithFzf(this IEnumerable<string> source, FzfOptions? options = null) =>
        ChooseImpl(source, MakeNoMultiArgs(options)).SingleOrDefault();

    public static async Task<string?> ChooseWithFzf(this IAsyncEnumerable<string> source, FzfOptions? options = null) =>
        (await ChooseImpl(source, MakeNoMultiArgs(options))).SingleOrDefault();

    public static List<string> ChooseMultiWithFzf(this IEnumerable<string> source, FzfOptions? options = null, int? maxSelect = null) =>
        ChooseImpl(source, MakeMultiArgs(options, maxSelect));

    public static async Task<List<string>> ChooseMultiWithFzf(this IAsyncEnumerable<string> source, FzfOptions? options = null, int? maxSelect = null) =>
        await ChooseImpl(source, MakeMultiArgs(options, maxSelect));

    private static IEnumerable<string> MakeNoMultiArgs(FzfOptions? options) =>
        ["--no-multi", .. options?.Arguments ?? []];
    private static IEnumerable<string> MakeMultiArgs(FzfOptions? options, int? maxSelect) =>
        ["--multi" + (maxSelect == null ? "" : $"={maxSelect}"), .. options?.Arguments ?? []];

    private static async Task<List<string>> ChooseImpl(IAsyncEnumerable<string> source, IEnumerable<string> arguments) {
        List<string> items = [];
        try {
            var p = StartFzfProcess(arguments, items);

            await foreach (var s in source) {
                p.StandardInput.WriteLine(s);
            }

            p.WaitForExit();
        }
        catch (IOException ex) when (ex.Message.Contains("Broken pipe")) {
        }
        return items;
    }

    private static List<string> ChooseImpl(IEnumerable<string> source, IEnumerable<string> arguments) {
        List<string> items = [];
        try {
            var p = StartFzfProcess(arguments, items);

            foreach (var s in source) {
                p.StandardInput.WriteLine(s);
            }

            p.WaitForExit();
        }
        catch (IOException ex) when (ex.Message.Contains("Broken pipe")) {
        }
        return items;
    }

    private static Process StartFzfProcess(IEnumerable<string> arguments, List<string> items) {
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
        return p;
    }

    public enum FzfColor { Dark, Light, _16, BW }
#pragma warning disable CA1720 // Identifier contains type name
    public enum FzfBorder { Rounded, Sharp, Bold, Block, Thinblock, Double, Horizontal, Vertical, Top, Bottom, Left, Right, None }
#pragma warning restore CA1720 // Identifier contains type name
    public enum FzfLayout { Default, Reverse, Reverse_List }
    public enum FzfTieBreak { Length, Chunk, Begin, End, Index }
    public enum FzfScheme { Default, Path, History }
    public enum FzfAlgo { V1, V2 }
}
