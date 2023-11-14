namespace KubernetesWorkflow.Recipe
{
    public class SchedulingAffinity
    {
        public SchedulingAffinity(string? notIn = null)
        {
            NotIn = notIn;
        }

        public string? NotIn { get; }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(NotIn)) return "none";
            return "notIn:" + NotIn;
        }
    }
}
