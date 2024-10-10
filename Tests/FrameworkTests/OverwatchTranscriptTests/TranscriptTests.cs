using Logging;
using Newtonsoft.Json;
using NUnit.Framework;
using OverwatchTranscript;
using System.IO.Compression;

namespace FrameworkTests.OverwatchTranscriptTests
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
        [Combinatorial]
        public void WriteAndRun()
        {
            WriteTranscript();
            AssertFileContent();
            ReadTranscript();

            File.Delete(TranscriptFilename);
        }

        private void WriteTranscript()
        {
            var log = new ConsoleLog();
            var writer = Transcript.NewWriter(log);

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

        private void ReadTranscript()
        {
            var reader = Transcript.NewReader(TranscriptFilename);

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

            var timeout = 10;
            while (moments.Count < 3 && events.Count < 4)
            {
                Thread.Sleep(10);
                reader.Next();

                timeout--;
                if (timeout == 0) Assert.Fail("Events not received.");
            }

            reader.Next();
            Assert.That(moments.Count, Is.EqualTo(3));
            Assert.That(events.Count, Is.EqualTo(4));

            AssertMoment(moments[0], utc: t0, duration: t1 - t0, index: 0);
            AssertMoment(moments[1], utc: t1, duration: t2 - t1, index: 1);
            AssertMoment(moments[2], utc: t2, duration: null, index: 2);

            AssertEvent(events, utc: t0, duration: t1 - t0, index: 0, data: EventData0);
            AssertEvent(events, utc: t1, duration: t2 - t1, index: 1, data: EventData2);
            AssertEvent(events, utc: t1, duration: t2 - t1, index: 1, data: EventData1);
            AssertEvent(events, utc: t2, duration: null, index: 2, data: EventData3);

            reader.Close();
        }

        private void AssertMoment(ActivateMoment m, DateTime utc, TimeSpan? duration, int index)
        {
            Assert.That(m.Utc, Is.EqualTo(utc));
            Assert.That(m.Duration, Is.EqualTo(duration));
            Assert.That(m.Index, Is.EqualTo(index));
        }

        private void AssertEvent(List<ActivateEvent<MyEvent>> events, DateTime utc, TimeSpan? duration, int index, string data)
        {
            var e = events.SingleOrDefault(e => e.Moment.Utc == utc && e.Payload.EventData == data);
            if (e == null) Assert.Fail("Event not found");

            Assert.That(e.Moment.Utc, Is.EqualTo(utc));
            Assert.That(e.Moment.Duration, Is.EqualTo(duration));
            Assert.That(e.Moment.Index, Is.EqualTo(index));
            Assert.That(e.Payload.EventData, Is.EqualTo(data));
        }

        private void AssertFileContent()
        {
            using var zip = ZipFile.OpenRead(TranscriptFilename);
            Assert.That(zip.Entries.Count, Is.EqualTo(2));
            foreach (var entry in zip.Entries)
            {
                if (entry.Name == "transcript.json")
                {
                    var transcript = ZipEntryJson<OverwatchTranscript.OverwatchTranscript>(entry);
                    AssertTranscript(transcript);
                }
                else
                {
                    var moments = ZipEntryToMoments(entry);
                    AssertMoments(moments);
                }
            }
        }

        private void AssertTranscript(OverwatchTranscript.OverwatchTranscript transcript)
        {
            Assert.That(transcript.Header.Common.NumberOfMoments, Is.EqualTo(3));
            Assert.That(transcript.Header.Common.NumberOfEvents, Is.EqualTo(4));
            Assert.That(transcript.Header.Common.EarliestUtc, Is.EqualTo(t0));
            Assert.That(transcript.Header.Common.LatestUtc, Is.EqualTo(t2));
            Assert.That(transcript.Header.Entries.Length, Is.EqualTo(1));
            Assert.That(transcript.Header.Entries[0].Key, Is.EqualTo(HeaderKey));
            Assert.That(transcript.Header.Entries[0].Value, Is.EqualTo("{\"HeaderData\":\"abcdef\"}"));

            Assert.That(transcript.MomentReferences.Length, Is.EqualTo(1));
            Assert.That(transcript.MomentReferences[0].NumberOfMoments, Is.EqualTo(3));
            Assert.That(transcript.MomentReferences[0].NumberOfEvents, Is.EqualTo(4));
            Assert.That(transcript.MomentReferences[0].EarliestUtc, Is.EqualTo(t0));
            Assert.That(transcript.MomentReferences[0].LatestUtc, Is.EqualTo(t2));
        }

        private void AssertMoments(OverwatchMoment[] moments)
        {
            Assert.That(moments.Length, Is.EqualTo(3));

            Assert.That(moments[0].Utc, Is.EqualTo(t0));
            Assert.That(moments[0].Events.Length, Is.EqualTo(1));
            Assert.That(moments[0].Events[0].Type, Is.EqualTo("FrameworkTests.OverwatchTranscriptTests.MyEvent"));
            Assert.That(moments[0].Events[0].Payload, Is.EqualTo("{\"EventData\":\"12345\"}"));

            Assert.That(moments[1].Utc, Is.EqualTo(t1));
            Assert.That(moments[1].Events.Length, Is.EqualTo(2));
            Assert.That(moments[1].Events[0].Type, Is.EqualTo("FrameworkTests.OverwatchTranscriptTests.MyEvent"));
            Assert.That(moments[1].Events[1].Type, Is.EqualTo("FrameworkTests.OverwatchTranscriptTests.MyEvent"));

            // output order is not guaranteed:
            var payloads = moments[1].Events.Select(e => e.Payload).ToArray();
            CollectionAssert.AreEquivalent(new[]
            {
                "{\"EventData\":\"90\"}",
                "{\"EventData\":\"678\"}"
            }, payloads);

            Assert.That(moments[2].Utc, Is.EqualTo(t2));
            Assert.That(moments[2].Events.Length, Is.EqualTo(1));
            Assert.That(moments[2].Events[0].Type, Is.EqualTo("FrameworkTests.OverwatchTranscriptTests.MyEvent"));
            Assert.That(moments[2].Events[0].Payload, Is.EqualTo("{\"EventData\":\"-=\"}"));
        }

        private T ZipEntryJson<T>(ZipArchiveEntry? entry)
        {
            if (entry == null) Assert.Fail("entry is null");
            using var stream = entry!.Open();
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            var result = JsonConvert.DeserializeObject<T>(json);
            if (result == null) Assert.Fail("didn't deserialize");
            return result!;
        }

        private OverwatchMoment[] ZipEntryToMoments(ZipArchiveEntry? entry)
        {
            var result = new List<OverwatchMoment>();
            if (entry == null) Assert.Fail("entry is null");
            using var stream = entry!.Open();
            using var reader = new StreamReader(stream);

            var line = reader.ReadLine();
            while (!string.IsNullOrEmpty(line))
            {
                var moment = JsonConvert.DeserializeObject<OverwatchMoment>(line);
                if (moment != null) result.Add(moment);
                line = reader.ReadLine();
            }

            return result.ToArray();
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
