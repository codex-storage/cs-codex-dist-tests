using DistTestCore;
using DistTestCore.Codex;
using NUnit.Framework;

namespace ContinuousTests.Tests
{
    public class ThresholdChecks : ContinuousTest
    {
        public override int RequiredNumberOfNodes => 1;
        public override TimeSpan RunTestEvery => TimeSpan.FromSeconds(30);
        public override TestFailMode TestFailMode => TestFailMode.StopAfterFirstFailure;

        [TestMoment(t: 0)]
        public void CheckAllThresholds()
        {
            var allNodes = CreateAccessToAllNodes();
            foreach (var n in allNodes) CheckThresholds(n);
        }

        private void CheckThresholds(CodexAccess n)
        {
            var breaches = n.GetDebugThresholdBreaches();
            if (breaches.breaches.Any())
            {
                Assert.Fail(string.Join(",", breaches.breaches.Select(b => FormatBreach(n, b))));
            }
        }

        private string FormatBreach(CodexAccess n, string breach)
        {
            return $"{n.Container.Name} = '{breach}'";
        }

        private CodexAccess[] CreateAccessToAllNodes()
        {
            // Normally, a continuous test accesses only a subset of the nodes in the deployment.
            // This time, we want to check all of them.
            var factory = new CodexAccessFactory();
            var allContainers = Configuration.CodexDeployment.CodexContainers;
            return factory.Create(Configuration, allContainers, Log, new DefaultTimeSet());
        }
    }
}
