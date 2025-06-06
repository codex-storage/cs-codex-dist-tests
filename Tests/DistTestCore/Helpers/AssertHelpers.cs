﻿using NUnit.Framework.Constraints;
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
                Time.WaitUntil(() => {
                    var c = constraint.Resolve();
                    return c.ApplyTo(actual()).IsSuccess;
                }, "RetryAssert: " + message);
            }
            catch (TimeoutException)
            {
                Assert.That(actual(), constraint, message);
            }
        }
    }
}
