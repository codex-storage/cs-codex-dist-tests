using Newtonsoft.Json;
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
        private readonly DateTime t0 = DateTime.UtcNow;
        private readonly DateTime t1 = DateTime.UtcNow.AddMinutes(1);
        private readonly DateTime t2 = DateTime.UtcNow.AddMinutes(2);

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
            var writer = new TranscriptWriter(workdir);

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
                EventData = EventData2
            });
            writer.Add(t1, new MyEvent
            {
                EventData = EventData1
            });

            writer.Write(TranscriptFilename);
        }

        private void ReadTranscript(string workdir)
        {
            var reader = new TranscriptReader(workdir, TranscriptFilename);

            var header = reader.GetHeader<TestHeader>(HeaderKey);
            Assert.That(header.HeaderData, Is.EqualTo(HeaderData));

            var events = new List<MyEvent>();
            reader.AddHandler<MyEvent>((utc, e) =>
            {
                e.CheckUtc = utc;
                events.Add(e);
            });

            Assert.That(events.Count, Is.EqualTo(0));
            reader.Next();
            Assert.That(events.Count, Is.EqualTo(1));
            reader.Next();
            Assert.That(events.Count, Is.EqualTo(2));
            reader.Next();
            Assert.That(events.Count, Is.EqualTo(3));
            reader.Next();
            Assert.That(events.Count, Is.EqualTo(3));

            Assert.That(events[0].CheckUtc, Is.EqualTo(t0));
            Assert.That(events[0].EventData, Is.EqualTo(EventData0));
            Assert.That(events[1].CheckUtc, Is.EqualTo(t1));
            Assert.That(events[1].EventData, Is.EqualTo(EventData1));
            Assert.That(events[2].CheckUtc, Is.EqualTo(t2));
            Assert.That(events[2].EventData, Is.EqualTo(EventData2));

            reader.Close();
        }
    }

    public class TestHeader
    {
        public string HeaderData { get; set; } = string.Empty;
    }

    public class MyEvent
    {
        public string EventData { get; set; } = string.Empty;

        [JsonIgnore]
        public DateTime CheckUtc { get; set; }   
    }
}
