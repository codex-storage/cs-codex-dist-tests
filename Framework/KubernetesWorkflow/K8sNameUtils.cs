namespace KubernetesWorkflow
{
    public static class K8sNameUtils
    {
        public static string Format(string s)
        {
            var result = s.ToLowerInvariant()
                .Replace(" ", "-")
                .Replace(":", "-")
                .Replace("/", "-")
                .Replace("\\", "-")
                .Replace("[", "-")
                .Replace("]", "-")
                .Replace(",", "-");

            result = result.Trim('-');
            if (result.Length > 62) result = result.Substring(0, 62);

            return result;
        }
    }
}
