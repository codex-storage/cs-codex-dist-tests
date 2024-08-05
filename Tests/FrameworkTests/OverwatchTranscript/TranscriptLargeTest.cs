using Logging;
using NUnit.Framework;
using OverwatchTranscript;

namespace FrameworkTests.OverwatchTranscript
{
    [TestFixture]
    public class TranscriptLargeTests
    {
        private const int NumberOfThreads = 10;
        private const int NumberOfEventsPerThread = 1000000;
        private const string TranscriptFilename = "testtranscriptlarge.owts";
        private TranscriptWriter writer = null!;

        [Test]
        [Ignore("Takes about 25 minutes to run.")]
        public void MillionsOfEvents()
        {
            var workdir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            var log = new FileLog(nameof(MillionsOfEvents));
            writer = new TranscriptWriter(log, workdir);

            Stopwatch.Measure(log, "Generate", () =>
            {
                var tasks = new List<Task>();
                for (var i = 0; i < NumberOfThreads; i++)
                {
                    tasks.Add(RunGeneratorThread());
                }

                Task.WaitAll(tasks.ToArray());
            });

            Stopwatch.Measure(log, "Write", () =>
            {
                writer.Write(TranscriptFilename);
            });

            Stopwatch.Measure(log, "Read", () =>
            {
                ReadTranscript(workdir);
            });

            File.Delete(TranscriptFilename);
        }

        private Task RunGeneratorThread()
        {
            return Task.Run(() =>
            {
                try
                {
                    var remaining = NumberOfEventsPerThread;
                    while (remaining > 0)
                    {
                        writer.Add(DateTime.UtcNow, new MyEvent
                        {
                            EventData = Guid.NewGuid().ToString()
                        });
                        remaining--;
                    }
                }
                catch (Exception ex)
                {
                    Assert.Fail("exception in thread: " + ex);
                }
            });
        }

        private void ReadTranscript(string workdir)
        {
            var reader = new TranscriptReader(workdir, TranscriptFilename);

            var expectedNumberOfEvents = NumberOfThreads * NumberOfEventsPerThread;
            Assert.That(reader.Header.NumberOfEvents, Is.EqualTo(expectedNumberOfEvents));

            var counter = 0;
            var current = DateTime.MinValue;
            reader.AddEventHandler<MyEvent>(e =>
            {
                counter++;
                if (e.Moment.Utc < current)
                {
                    Assert.Fail("Event has moment BEFORE previous one.");
                }
                current = e.Moment.Utc;
            });

            var run = true;
            while (run)
            {
                run = reader.Next();
            }

            reader.Close();
        }
    }
}
