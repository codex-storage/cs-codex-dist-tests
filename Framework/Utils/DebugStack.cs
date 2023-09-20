using System.Diagnostics;

namespace Utils
{
    public class DebugStack
    {
        public static string GetCallerName(int skipFrames = 0)
        {
            return new StackFrame(2 + skipFrames, true).GetMethod()!.Name;
        }
    }
}
