using Core;
using NUnit.Framework;

namespace DistTestCore
{
    public static class DownloadedLogExtensions
    {
        public static void AssertLogContains(this IDownloadedLog log, string expectedString)
        {
            Assert.That(log.GetLinesContaining(expectedString).Any(), $"Did not find '{expectedString}' in log.");
        }

        public static void AssertLogDoesNotContain(this IDownloadedLog log, params string[] unexpectedStrings)
        {
            var errors = new List<string>();
            foreach (var str in unexpectedStrings)
            {
                var lines = log.GetLinesContaining(str);
                foreach (var line in lines)
                {
                    errors.Add($"Found '{str}' in line '{line}'.");
                }
            }
            CollectionAssert.IsEmpty(errors);
        }
    }
}
