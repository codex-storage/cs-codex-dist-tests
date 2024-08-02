using Logging;
using NUnit.Framework;
using OverwatchTranscript;

namespace FrameworkTests.OverwatchTranscript
{
    [TestFixture]
    public class TranscriptTests
    {
        private const string TranscriptFilename = "testtranscript.owts";
        private const string HeaderKey = "testHeader";
        private const string HeaderData = "abcdef";
        private const string EventData0 = "12345";
        private const string EventData1 = "678";
        private const string EventData2 = "90";
        private const string EventData3 = "-=";
        private readonly DateTime t0 = DateTime.UtcNow;
        private readonly DateTime t1 = DateTime.UtcNow.AddMinutes(1);
        private readonly DateTime t2 = DateTime.UtcNow.AddMinutes(3);

        [Test]
        public void WriteAndRun()
        {
            var workdir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            WriteTranscript(workdir);
            ReadTranscript(workdir);

            File.Delete(TranscriptFilename);
        }

        private void WriteTranscript(string workdir)
        {
            var log = new ConsoleLog();
            var writer = new TranscriptWriter(log, workdir);

            writer.AddHeader(HeaderKey, new TestHeader
            {
                HeaderData = HeaderData
            });

            writer.Add(t0, new MyEvent
            {
                EventData = EventData0
            });
            writer.Add(t2, new MyEvent
            {
                EventData = EventData3
            });
            writer.Add(t1, new MyEvent
            {
                EventData = EventData1
            });
            writer.Add(t1, new MyEvent
            {
                EventData = EventData2
            });

            if (File.Exists(TranscriptFilename)) File.Delete(TranscriptFilename);

            writer.Write(TranscriptFilename);
        }

        private void ReadTranscript(string workdir)
        {
            var reader = new TranscriptReader(workdir, TranscriptFilename);

            var header = reader.GetHeader<TestHeader>(HeaderKey);
            Assert.That(header.HeaderData, Is.EqualTo(HeaderData));
            Assert.That(reader.Header.NumberOfMoments, Is.EqualTo(3));
            Assert.That(reader.Header.NumberOfEvents, Is.EqualTo(4));
            Assert.That(reader.Header.EarliestUtc, Is.EqualTo(t0));
            Assert.That(reader.Header.LatestUtc, Is.EqualTo(t2));

            var moments = new List<ActivateMoment>();
            var events = new List<ActivateEvent<MyEvent>>();
            reader.AddMomentHandler(moments.Add);
            reader.AddEventHandler<MyEvent>(events.Add);


            Assert.That(moments.Count, Is.EqualTo(0));
            Assert.That(events.Count, Is.EqualTo(0));

            reader.Next();
            Assert.That(moments.Count, Is.EqualTo(1));
            Assert.That(events.Count, Is.EqualTo(1));

            reader.Next();
            Assert.That(moments.Count, Is.EqualTo(2));
            Assert.That(events.Count, Is.EqualTo(3));

            reader.Next();
            Assert.That(moments.Count, Is.EqualTo(3));
            Assert.That(events.Count, Is.EqualTo(4));

            reader.Next();
            Assert.That(moments.Count, Is.EqualTo(3));
            Assert.That(events.Count, Is.EqualTo(4));

            AssertMoment(moments[0], utc: t0, duration: t1 - t0, index: 0);
            AssertMoment(moments[1], utc: t1, duration: t2 - t1, index: 1);
            AssertMoment(moments[2], utc: t2, duration: null, index: 2);

            AssertEvent(events[0], utc: t0, duration: t1 - t0, index: 0, data: EventData0);
            AssertEvent(events[1], utc: t1, duration: t2 - t1, index: 1, data: EventData1);
            AssertEvent(events[2], utc: t1, duration: t2 - t1, index: 1, data: EventData2);
            AssertEvent(events[3], utc: t2, duration: null, index: 2, data: EventData3);

            reader.Close();
        }

        private void AssertMoment(ActivateMoment m, DateTime utc, TimeSpan? duration, int index)
        {
            Assert.That(m.Utc, Is.EqualTo(utc));
            Assert.That(m.Duration, Is.EqualTo(duration));
            Assert.That(m.Index, Is.EqualTo(index));
        }

        private void AssertEvent(ActivateEvent<MyEvent> e, DateTime utc, TimeSpan? duration, int index, string data)
        {
            Assert.That(e.Moment.Utc, Is.EqualTo(utc));
            Assert.That(e.Moment.Duration, Is.EqualTo(duration));
            Assert.That(e.Moment.Index, Is.EqualTo(index));
            Assert.That(e.Payload.EventData, Is.EqualTo(data));
        }
    }

    public class TestHeader
    {
        public string HeaderData { get; set; } = string.Empty;
    }

    public class MyEvent
    {
        public string EventData { get; set; } = string.Empty;
    }
}
