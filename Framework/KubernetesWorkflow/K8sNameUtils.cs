namespace KubernetesWorkflow
{
    public static class K8sNameUtils
    {
        public static string Format(string s)
        {
            return Format(s, 62);
        }

        public static string FormatPortName(string s)
        {
            return Format(s, 15);
        }

        private static string Format(string s, int maxLength)
        {
            var result = s.ToLowerInvariant()
                .Replace("_", "-")
                .Replace(" ", "-")
                .Replace(":", "-")
                .Replace("/", "-")
                .Replace("\\", "-")
                .Replace("[", "-")
                .Replace("]", "-")
                .Replace(",", "-")
                .Replace("(", "-")
                .Replace(")", "-");

            if (result.Length > maxLength) result = result.Substring(0, maxLength);
            result = result.Trim('-');

            return result;
        }
    }
}
