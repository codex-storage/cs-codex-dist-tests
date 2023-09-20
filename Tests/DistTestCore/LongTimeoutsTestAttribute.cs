using NUnit.Framework;

namespace DistTestCore
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class UseLongTimeoutsAttribute : PropertyAttribute
    {
    }
}
