namespace CodexContractsPlugin
{
    public class SelfUpdater
    {
        public void Update(string abi, string bytecode)
        {
            var deserializer = new Nethereum.Generators.Net.GeneratorModelABIDeserialiser();
            var abiModel = deserializer.DeserialiseABI(abi);
            var abiCtor = abiModel.Constructor;
            //var b = new Nethereum.Generators.Console.ConsoleGenerator(abiModel, "Marketplace", bytecode, "namespace", "cqsNamespace", "fucntionoutputnamespace", Nethereum.Generators.Core.CodeGenLanguage.CSharp);
            var c = new Nethereum.Generators.CQS.ContractDeploymentCQSMessageGenerator(abiCtor, "namespace", bytecode, "Marketplace", Nethereum.Generators.Core.CodeGenLanguage.CSharp);
            var lines = c.GenerateClass();
            var a = 0;
        }
    }
}
