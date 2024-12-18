using Logging;
using NUnit.Framework;

namespace DistTestCore
{
    public static class NameUtils
    {
        public static string GetTestMethodName(string name = "")
        {
            if (!string.IsNullOrEmpty(name)) return name;
            var test = TestContext.CurrentContext.Test;
            var args = FormatArguments(test);
            return ReplaceInvalidCharacters($"{test.MethodName}{args}");
        }

        public static string GetFixtureFullName(LogConfig config, DateTime start, string name)
        {
            var folder = DetermineFolder(config, start);
            var fixtureName = GetFixtureName(name, start);
            return Path.Combine(folder, fixtureName);
        }

        public static string GetRawFixtureName()
        {
            var test = TestContext.CurrentContext.Test;
            if (test.ClassName!.Contains("AdhocContext")) return "none";
            var className = test.ClassName!.Substring(test.ClassName.LastIndexOf('.') + 1);
            return className.Replace('.', '-');
        }

        public static string GetCategoryName()
        {
            var test = TestContext.CurrentContext.Test;
            if (test.ClassName!.Contains("AdhocContext")) return "none";
            return test.ClassName!.Substring(0, test.ClassName.LastIndexOf('.'));
        }

        public static string GetTestId()
        {
            return GetEnvVar("TESTID", "EnvVar-TESTID-NotSet");
        }

        public static string MakeDeployId()
        {
            return DateTime.UtcNow.ToString("yyyyMMdd-hhmmss");
        }

        private static string GetEnvVar(string name, string defaultValue)
        {
            var v = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrEmpty(v)) return defaultValue;
            return v;
        }

        private static string FormatArguments(TestContext.TestAdapter test)
        {
            if (test.Arguments == null || !test.Arguments.Any()) return "";
            return $"[{string.Join(',', test.Arguments.Select(FormatArgument).ToArray())}]";
        }
        
        private static string FormatArgument(object? obj)
        {
            if (obj == null) return "";
            var str = obj.ToString();
            if (string.IsNullOrEmpty(str)) return "";
            return ReplaceInvalidCharacters(str);
        }

        private static string ReplaceInvalidCharacters(string name)
        {
            return name
                .Replace(":", "_")
                .Replace("/", "_")
                .Replace("\\", "_");
        }

        private static string DetermineFolder(LogConfig config, DateTime start)
        {
            return Path.Join(
               config.LogRoot,
               $"{start.Year}-{Pad(start.Month)}",
               Pad(start.Day));
        }

        private static string GetFixtureName(string name, DateTime start)
        {
            var className = GetRawFixtureName();
            if (!string.IsNullOrEmpty(name)) className = name;
            return $"{Pad(start.Hour)}-{Pad(start.Minute)}-{Pad(start.Second)}Z_{className.Replace('.', '-')}";
        }

        private static string Pad(int n)
        {
            return n.ToString().PadLeft(2, '0');
        }
    }
}
