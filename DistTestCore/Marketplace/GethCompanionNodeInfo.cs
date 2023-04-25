﻿using KubernetesWorkflow;
using NethereumWorkflow;

namespace DistTestCore.Marketplace
{
    public class GethCompanionNodeInfo
    {
        public GethCompanionNodeInfo(RunningContainer runningContainer, string account, string privateKey)
        {
            RunningContainer = runningContainer;
            Account = account;
            PrivateKey = privateKey;
        }

        public RunningContainer RunningContainer { get; }
        public string Account { get; }
        public string PrivateKey { get; }

        public NethereumInteraction StartInteraction()
        {
            var ip = RunningContainer.Pod.Cluster.IP;
            var port = RunningContainer.ServicePorts[0].Number;
            var account = Account;
            var privateKey = PrivateKey;

            var creator = new NethereumInteractionCreator(ip, port, account, privateKey);
            return creator.CreateWorkflow();
        }
    }
}