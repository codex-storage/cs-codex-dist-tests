using CodexContractsPlugin;
using GethPlugin;

namespace BiblioTech
{
    public class GethLink
    {
        private GethLink(IGethNode node, ICodexContracts contracts)
        {
            Node = node;
            Contracts = contracts;
        }

        public IGethNode Node { get; }
        public ICodexContracts Contracts { get; }

        public static GethLink? Create()
        {
            var gethConnector = GetGeth();
            if (gethConnector == null) return null;

            var gethNode = gethConnector.GethNode;
            var contracts = gethConnector.CodexContracts;
            return new GethLink(gethNode, contracts);
        }

        private static GethConnector.GethConnector? GetGeth()
        {
            try
            {
                return GethConnector.GethConnector.Initialize(Program.Log);
            }
            catch (Exception ex)
            {
                Program.Log.Error("Failed to initialize geth connector: " + ex);
                return null;
            }
        }
    }
}
