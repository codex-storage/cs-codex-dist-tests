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

            return result.Trim('-');
        }
    }
}
