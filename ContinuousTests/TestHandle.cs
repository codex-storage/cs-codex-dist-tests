using System.Reflection;

namespace ContinuousTests
{
    public class TestHandle
    {
        private readonly List<MethodMoment> moments = new List<MethodMoment>();

        public TestHandle(ContinuousTest test)
        {
            Test = test;

            ReflectTestMoments();

            if (!moments.Any()) throw new Exception("Test has no moments.");
            if (moments.Count != moments.Select(m => m.Moment).Distinct().Count()) throw new Exception("Test has duplicate moments");
        }

        public ContinuousTest Test { get; }

        public int? GetNextMoment(int currentMoment)
        {
            var remainingMoments = moments.Where(m => m.Moment >= currentMoment).ToArray();
            if (!remainingMoments.Any()) return null;
            return remainingMoments.Min(m => m.Moment);
        }

        public int GetLastMoment()
        {
            return moments.Max(m => m.Moment);
        }

        public void InvokeMoment(int currentMoment)
        {
            var moment = moments.SingleOrDefault(m => m.Moment == currentMoment);
            if (moment == null) return;

            moment.Method.Invoke(Test, Array.Empty<object>());
        }

        private void ReflectTestMoments()
        {
            var methods = Test.GetType().GetMethods()
                      .Where(m => m.GetCustomAttributes(typeof(TestMomentAttribute), false).Length > 0)
                      .ToArray();

            foreach (var method in methods)
            {
                var moment = method.GetCustomAttribute<TestMomentAttribute>();
                if (moment != null && moment.T >= 0)
                {
                    moments.Add(new MethodMoment(method, moment.T));
                }
            }
        }
    }

    public class MethodMoment
    {
        public MethodMoment(MethodInfo method, int moment)
        {
            Method = method;
            Moment = moment;
        }

        public MethodInfo Method { get; }
        public int Moment { get; }
    }
}
