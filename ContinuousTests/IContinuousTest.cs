using DistTestCore;
using DistTestCore.Codex;
using Logging;

namespace ContinuousTests
{
    public interface IContinuousTest
    {
        string Name { get; }
        int RequiredNumberOfNodes { get; }

        void Run();
    }

    public abstract class ContinuousTest : IContinuousTest
    {
        public CodexNode[] Nodes { get; set; } = null!;
        public BaseLog Log { get; set; } = null!;
        public FileManager FileManager { get; set; } = null!;

        public abstract int RequiredNumberOfNodes { get; }

        public string Name
        {
            get
            {
                return GetType().Name;
            }
        }

        public abstract void Run();
    }
}
