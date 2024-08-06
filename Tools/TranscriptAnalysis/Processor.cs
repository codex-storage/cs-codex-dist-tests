using Logging;
using OverwatchTranscript;

namespace TranscriptAnalysis
{
    public class Processor
    {
        private readonly ILog log;
        private readonly ITranscriptReader reader;

        public Processor(ILog log, ITranscriptReader reader)
        {
            this.log = new LogPrefixer(log, "(Processor) ");
            this.reader = reader;
        }

        public void RunAll()
        {
            log.Log("Events: " + reader.Header.NumberOfEvents);
            log.Log("Moments: " + reader.Header.NumberOfMoments);

            log.Log("Processing...");
            var count = 0;
            var tenth = reader.Header.NumberOfMoments / 10;
            var miss = 0;
            while (true)
            {
                if (!reader.Next())
                {
                    miss++;
                    if (miss > 1000)
                    {
                        log.Log("Done");
                        return;
                    }
                    Thread.Sleep(1);
                }
                else
                {
                    miss = 0;
                    count++;
                    if (count % tenth == 0)
                    {
                        log.Log($"{count} / {reader.Header.NumberOfMoments}...");
                    }
                }
            }
        }
    }
}
