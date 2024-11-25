namespace CodexUnitTestCrusher
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var p = new Program();
            p.Run();
        }

        private const bool AddRandomSleeps = true;
        private const bool GenerateLoopTestRunners = true;

        private readonly string Root = "C:\\Projects\\nim-codex";
        private readonly string[] Exclude =
        [
            "vendor"
        ];

        private readonly List<string> scriptPaths = new List<string>();
        private readonly string Include = "import std/random";
        private readonly string SleepLine = "await sleepAsync(rand(10))";
        private readonly int NumCompiles = 10;
        private readonly int NumRuns = 100;
        private readonly string[] TestRunner =
        [
            "set -e",
            "for i in {0..<NUMCOMPILES>}",
            "do",
            "    echo \"#1\" >> \"<TESTFILE>\"",
                        "    for j in {0..<NUMRUNS>}",
                        "    do",
            "        nim c -r \"<TESTFILE>\"",
            "    done",
            "done",
            "rm <SCRIPTFILE>",
        ];

        public void Run()
        {
            TraverseFolder(Root);
            CreateRunScripts();
        }

        private void CreateRunScripts()
        {
            var lineLines = new List<List<string>>();
            lineLines.Add(new List<string>());
            lineLines.Add(new List<string>());
            lineLines.Add(new List<string>());

            var i = 0;
            foreach (var script in scriptPaths)
            {
                lineLines[i].Add($"sh \"{script}\"");
                i = (i + 1) % lineLines.Count;
            }
            File.WriteAllLines(@"C:\Projects\nim-codex\runall1.sh", lineLines[0]);
            File.WriteAllLines(@"C:\Projects\nim-codex\runall2.sh", lineLines[1]);
            File.WriteAllLines(@"C:\Projects\nim-codex\runall3.sh", lineLines[2]);
        }

        private void TraverseFolder(string root)
        {
            if (Exclude.Any(x => root.Contains(x))) return;

            var folder = Directory.GetDirectories(root);
            foreach (var dir in folder) TraverseFolder(dir);

            var files = Directory.GetFiles(root);
            foreach (var file in files) ProcessFile(file);
        }

        private void ProcessFile(string file)
        {
            if (!file.EndsWith(".nim")) return;

            if (AddRandomSleeps) AddRandomSleepsToNimFile(file);
            if (GenerateLoopTestRunners) GenerateTestRunner(file);
        }

        private void GenerateTestRunner(string file)
        {
            var filename = Path.GetFileName(file);
            if (!filename.StartsWith("test")) return;
            var path = Path.GetDirectoryName(file);

            var testFile = file;
            var scriptFile = filename.Replace(".nim", ".sh");
            WriteScriptFile(path!, testFile, scriptFile);
        }

        private void WriteScriptFile(string path, string testFile, string scriptFile)
        {
            var lines = TestRunner.Select(l =>
            {
                return l
                    .Replace("<NUMCOMPILES>", NumCompiles.ToString())
                    .Replace("<NUMRUNS>", NumRuns.ToString())
                    .Replace("<TESTFILE>", testFile.ToString())
                    .Replace("<SCRIPTFILE>", scriptFile.ToString())
                ;

            }).ToArray();

            var fullPath = Path.Combine(path, scriptFile);
            File.WriteAllLines(fullPath, lines);
            scriptPaths.Add(fullPath);
        }

        private void AddRandomSleepsToNimFile(string file)
        {
            Console.WriteLine("Processing file: " + file);

            var lines = File.ReadAllLines(file).ToList();
            if (!lines.Any(l => l == Include))
            {
                AddInclude(lines);
            }

            var modified = false;
            for (int i = 0; i < lines.Count; i++)
            {
                if (ProcessLine(i, lines[i], lines))
                {
                    i++;
                    modified = true;
                }
            }

            if (modified) File.WriteAllLines(file, lines);
        }

        private bool ProcessLine(int i, string line, List<string> lines)
        {
            if (IsComment(line)) return false;

            if (line.Contains("await "))
            {
                if (!line.Contains("sleep"))
                {
                    var previous = GetPreviousLine(i, lines);
                    if (previous != null)
                    {
                        var trim = previous.Trim();
                        // previous line was "let" ???
                        if (trim == "let")
                        {
                            // insert before let.
                            InsertSleepLine(i - 1, lines);
                            return true;
                        }
                        // previous line was "without =?" ??
                        if (trim.StartsWith("without") && trim.EndsWith("=?"))
                        {
                            // insert before without.
                            InsertSleepLine(i - 1, lines);
                            return true;
                        }
                        // previous line was "const" ??
                        if (trim == "const") return false;
                    }

                    var indent = GetIndent(line);
                    if (indent.Length < 3) InsertSleepLine(i, lines);
                    return true;
                }
            }
            return false;
        }

        private void InsertSleepLine(int i, List<string> lines)
        {
            lines.Insert(i, GetIndent(lines[i]) + SleepLine);
        }

        private string? GetPreviousLine(int i, List<string> lines)
        {
            var idx = i - 1;
            if (idx < 0) return null;
            return lines[idx];
        }

        private bool IsComment(string line)
        {
            return line.Trim().StartsWith("#");
        }

        private string GetIndent(string line)
        {
            var result = "";

            while (line.StartsWith(" "))
            {
                result += " ";
                line = line.Substring(1);
            }

            return result;
        }

        private void AddInclude(List<string> lines)
        {
            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (line.StartsWith("import "))
                {
                    lines.Insert(i, Include);
                    return;
                }
            }
        }
    }
}
