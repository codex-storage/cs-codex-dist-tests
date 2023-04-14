using CodexDistTestCore.Config;
using NUnit.Framework;
using System.Numerics;
using System.Text;

namespace CodexDistTestCore.Marketplace
{
    public class MarketplaceController
    {
        private readonly TestLog log;
        private readonly K8sManager k8sManager;
        private readonly NumberSource companionGroupNumberSource = new NumberSource(0);
        private List<GethCompanionGroup> companionGroups = new List<GethCompanionGroup>();
        private GethBootstrapInfo? bootstrapInfo;

        public MarketplaceController(TestLog log, K8sManager k8sManager)
        {
            this.log = log;
            this.k8sManager = k8sManager;
        }


    }
}
