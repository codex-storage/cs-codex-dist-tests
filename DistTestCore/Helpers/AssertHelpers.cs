using NUnit.Framework.Constraints;
using NUnit.Framework;
using Utils;

namespace DistTestCore.Helpers
{
    public static class AssertHelpers
    {
        public static void RetryAssert<T>(IResolveConstraint constraint, Func<T> actual, string message)
        {
            try
            {
                var c = constraint.Resolve();
                Time.WaitUntil(() => c.ApplyTo(actual()).IsSuccess);
            }
            catch (TimeoutException)
            {
                Assert.That(actual(), constraint, message);
            }
        }
    }
}
