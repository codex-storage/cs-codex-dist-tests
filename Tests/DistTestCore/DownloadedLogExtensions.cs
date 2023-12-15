using Core;
using NUnit.Framework;

namespace DistTestCore
{
    public static class DownloadedLogExtensions
    {
        public static void AssertLogContains(this IDownloadedLog log, string expectedString)
        {
            Assert.That(log.DoesLogContain(expectedString), $"Did not find '{expectedString}' in log.");
        }

        public static void AssertLogDoesNotContain(this IDownloadedLog log, params string[] unexpectedStrings)
        {
            var errors = new List<string>();
            foreach (var str in unexpectedStrings)
            {
                if (log.DoesLogContain(str))
                {
                    errors.Add($"Did find '{str}' in log.");
                }
            }
            CollectionAssert.IsEmpty(errors);
        }
    }
}
