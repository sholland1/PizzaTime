using System.Diagnostics;

namespace Hollandsoft.OrderPizza;

public static class Fzf {
    public class FzfOptions {
        public string? Prompt { get; init; }
        public string? Preview { get; init; }
        public string? Query { get; init; }
        public bool PrintQuery { get; init; } = false;
        public bool? Extended { get; init; }
        public bool Exact { get; init; } = false;
        public bool? CaseSensitive { get; init; }
        public bool NoMouse { get; init; } = false;
        public bool Ansi { get; init; } = false;
        public bool Literal { get; init; } = false;
        public bool NoSort { get; init; } = false;
        public bool Track { get; init; } = false;
        public bool TAC { get; init; } = false;
        public bool Disabled { get; init; } = false;
        public bool Select1 { get; init; } = false;
        public bool Exit0 { get; init; } = false;

        public virtual IEnumerable<string> Arguments {
            get {
                if (Prompt != null) yield return $"--prompt=\"{Prompt}\"";
                if (Preview != null) yield return $"--preview=\"{Preview}\"";
                if (Query != null) yield return $"--query=\"{Query}\"";
                if (PrintQuery) yield return "--print-query";
                if (Extended is bool x) yield return x ? "-x" : "+x";
                if (Exact) yield return "--exact";
                if (CaseSensitive is bool cs) yield return cs ? "+i" : "-i";
                if (NoMouse) yield return "--no-mouse";
                if (Ansi) yield return "--ansi";
                if (Literal) yield return "--literal";
                if (NoSort) yield return "--no-sort";
                if (Track) yield return "--track";
                if (TAC) yield return "--tac";
                if (Disabled) yield return "--disabled";
                if (Select1) yield return "-1";
                if (Exit0) yield return "-0";
            }
        }
    }

    public class FzfMultiOptions : FzfOptions {
        public int? MaxSelect { get; init; }

        public FzfMultiOptions(int? maxSelect = null) => MaxSelect = maxSelect;

        public override IEnumerable<string> Arguments => base.Arguments
            .Prepend("--multi" + (MaxSelect == null ? "" : $"={MaxSelect}"));
    }

    public static string? ChooseWithFzf(this IEnumerable<string> source, FzfOptions? options = null) =>
        ChooseImpl(source, (options ?? new()).Arguments).SingleOrDefault();

    public static List<string> ChooseWithFzf(this IEnumerable<string> source, FzfMultiOptions options) =>
        ChooseImpl(source, options.Arguments);

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
}
// Console.WriteLine("Edit this line:");
// var result = ReadLine("Testing");
// Console.WriteLine("Result: " + result);
