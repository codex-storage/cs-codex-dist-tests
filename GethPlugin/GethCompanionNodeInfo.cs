//using KubernetesWorkflow;
//using NethereumWorkflow;

//namespace GethPlugin
//{
//    public class GethCompanionNodeInfo
//    {
//        public GethCompanionNodeInfo(RunningContainer runningContainer, GethAccount[] accounts)
//        {
//            RunningContainer = runningContainer;
//            Accounts = accounts;
//        }

//        public RunningContainer RunningContainer { get; }
//        public GethAccount[] Accounts { get; }

//        public NethereumInteraction StartInteraction(TestLifecycle lifecycle, GethAccount account)
//        {
//            var address = lifecycle.Configuration.GetAddress(RunningContainer);
//            var privateKey = account.PrivateKey;

//            var creator = new NethereumInteractionCreator(lifecycle.Log, address.Host, address.Port, privateKey);
//            return creator.CreateWorkflow();
//        }
//    }
//}
