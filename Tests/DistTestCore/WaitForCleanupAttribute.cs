using NUnit.Framework;

namespace DistTestCore
{
    /// <summary>
    /// By default, test system does not wait until all resources are destroyed before starting the
    /// next test. This saves a lot of time but it's not always what you want.
    /// If you want to be sure the resources of your test are destroyed before the next test starts,
    /// add this attribute to your test method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class WaitForCleanupAttribute : PropertyAttribute
    {
    }
}
