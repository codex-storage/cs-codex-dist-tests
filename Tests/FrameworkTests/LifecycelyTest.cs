//using NUnit.Framework;

//namespace FrameworkTests
//{
//    [Parallelizable(ParallelScope.All)]
//    [TestFixture(10)]
//    [TestFixture(20)]
//    [TestFixture(30)]
//    public class LifecycelyTest
//    {
//        public LifecycelyTest(int num)
//        {
//            Log("ctor", GetCurrentTestName(), num);
//            this.num = num;
//        }

//        [SetUp]
//        public void Setup()
//        {
//            Log(nameof(Setup), GetCurrentTestName());
//        }

//        [TearDown]
//        public void TearDown()
//        {
//            Log(nameof(TearDown), GetCurrentTestName());
//        }

//        [Test]
//        public void A()
//        {
//            Log(nameof(A), "Run");
//            SleepRandom();
//            Log(nameof(A), "Finish");
//        }

//        [Test]
//        public void B()
//        {
//            Log(nameof(B), "Run");
//            SleepRandom();
//            Log(nameof(B), "Finish");
//        }

//        [Test]
//        public void C()
//        {
//            Log(nameof(C), "Run");
//            SleepRandom();
//            Log(nameof(C), "Finish");
//        }

//        [Test]
//        [Combinatorial]
//        public void Multi(
//            [Values(1, 2, 3)] int num)
//        {
//            Log(nameof(Multi), "Run", num);
//            SleepRandom();
//            Log(nameof(Multi), "Finish", num);
//        }












//        private static readonly Random r = new Random();
//        private readonly int num;

//        private void SleepRandom()
//        {
//            Thread.Sleep(TimeSpan.FromSeconds(5.0));
//            Thread.Sleep(TimeSpan.FromMilliseconds(r.Next(100, 1000)));
//        }

//        private void Log(string scope, string msg)
//        {
//            ALog.Log($"{num} {scope} {msg}");
//        }

//        private void Log(string scope, string msg, int num)
//        {
//            ALog.Log($"{this.num} {scope} {msg} {num}");
//        }

//        private string GetCurrentTestName()
//        {
//            return $"[{TestContext.CurrentContext.Test.Name}]";
//        }
//    }




//    public class ALog
//    {
//        private static readonly object _lock = new object();

//        public static void Log(string msg)
//        {
//            lock (_lock)
//            {
//                File.AppendAllLines("C:\\Users\\vexor\\Desktop\\Alog.txt", [msg]);
//            }
//        }
//    }












//    public class Base
//    {
//        private readonly Dictionary<int, Dictionary<Type, ITestLifecycleComponent>> anyFields = new();

//        public void Setup()
//        {
//            var testId = 23;

//            var fields = new Dictionary<Type, ITestLifecycleComponent>();
//            anyFields.Add(testId, fields);
//            YieldFields(field =>
//            {
//                fields.Add(field.GetType(), field);
//            });

//            foreach (var field in fields.Values)
//            {
//                field.Start();
//            }
//        }

//        public void TearDown()
//        {
//            var testId = 23;

//            // foreach stop

//            anyFields.Remove(testId);
//        }

//        public T Get<T>()
//        {
//            int testId = 123;
//            var fields = anyFields[testId];
//            var type = typeof(T);
//            var result = fields[type];
//            return (T)result;
//        }

//        public BaseFields GetBaseField()
//        {
//            return Get<BaseFields>();
//        }

//        protected virtual void YieldFields(Action<ITestLifecycleComponent> giveField)
//        {
//            giveField(new BaseFields());
//        }
//    }

//    public class Mid : Base
//    {
//        protected override void YieldFields(Action<ITestLifecycleComponent> giveField)
//        {
//            base.YieldFields(giveField);
//            giveField(new MidFields());
//        }

//        public MidFields GetMid()
//        {
//            return Get<MidFields>();
//        }
//    }

//    public class Top : Mid
//    {
//        protected override void YieldFields(Action<ITestLifecycleComponent> giveField)
//        {
//            base.YieldFields(giveField);
//            giveField(new TopFields());
//        }

//        public TopFields GetTop()
//        {
//            return Get<TopFields>();
//        }
//    }

//    public class BaseFields : ITestLifecycleComponent
//    {
//        public string EntryPoint { get; set; } = string.Empty;
//        public string Log { get; set; } = string.Empty;
//    }

//    public class MidFields : ITestLifecycleComponent
//    {
//        public string Nodes { get; set; } = string.Empty;
//    }

//    public class TopFields : ITestLifecycleComponent
//    {
//        public string Geth { get; set; } = string.Empty;
//        public string Contracts { get; set; } = string.Empty;
//    }
//}
