using DistTestCore;
using DistTestCore.Codex;

namespace CodexNetDeployer
{
    public class Configuration
    {
        public Configuration(
            string codexImage,
            string gethImage,
            string contractsImage,
            string kubeConfigFile,
            string kubeNamespace,
            int? numberOfCodexNodes,
            int? storageQuota,
            CodexLogLevel codexLogLevel,
            TestRunnerLocation runnerLocation)
        {
            CodexImage = codexImage;
            GethImage = gethImage;
            ContractsImage = contractsImage;
            KubeConfigFile = kubeConfigFile;
            KubeNamespace = kubeNamespace;
            NumberOfCodexNodes = numberOfCodexNodes;
            StorageQuota = storageQuota;
            CodexLogLevel = codexLogLevel;
            RunnerLocation = runnerLocation;
        }

        public string CodexImage { get; }
        public string GethImage { get; }
        public string ContractsImage { get; }
        public string KubeConfigFile { get; }
        public string KubeNamespace { get; }
        public int? NumberOfCodexNodes { get; }
        public int? StorageQuota { get; }
        public CodexLogLevel CodexLogLevel { get; }
        public TestRunnerLocation RunnerLocation { get; }

        public void PrintConfig()
        {
            ForEachProperty(onString: Print, onInt: Print);
        }

        public List<string> Validate()
        {
            var errors = new List<string>();

            ForEachProperty(
                onString: (n, v) => StringIsSet(n, v, errors),
                onInt: (n, v) => IntIsOverZero(n, v, errors));

            return errors;
        }

        private void ForEachProperty(Action<string, string> onString, Action<string, int?> onInt)
        {
            var properties = GetType().GetProperties();
            foreach (var p in properties)
            {
                if (p.PropertyType == typeof(string)) onString(p.Name, (string)p.GetValue(this)!);
                if (p.PropertyType == typeof(int?)) onInt(p.Name, (int?)p.GetValue(this)!);
            }
        }

        private static void IntIsOverZero(string variable, int? value, List<string> errors)
        {
            if (value == null || value.Value < 1)
            {
                errors.Add($"{variable} is must be set and must be greater than 0.");
            }
        }

        private static void StringIsSet(string variable, string value, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add($"{variable} is must be set.");
            }
        }

        private static void Print(string variable, string value)
        {
            Console.WriteLine($"\t{variable}: '{value}'");
        }

        private static void Print(string variable, int? value)
        {
            if (value != null) Print(variable, value.ToString()!);
            else Print(variable, "<NONE>");
        }
    }
}
