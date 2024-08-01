using Newtonsoft.Json;

namespace OverwatchTranscript
{
    public class MomentReader
    {
        private readonly OverwatchTranscript model;
        private readonly string workingDir;
        private int referenceIndex = 0;
        private int momentsRead = 0;
        private OpenReference currentRef;

        public MomentReader(OverwatchTranscript model, string workingDir)
        {
            this.model = model;
            this.workingDir = workingDir;

            currentRef = CreateOpenReference();
        }

        public OverwatchMoment? Next()
        {
            if (referenceIndex >= model.MomentReferences.Length) return null;

            var moment = currentRef.ReadNext();
            if (moment == null)
            {
                currentRef.Close();
                currentRef = null!;

                // This reference file ran out.
                // The number of moments read should match exactly the number of moments
                // describe in the reference. If not, error:
                var expected = model.MomentReferences[referenceIndex].NumberOfMoments;
                if (momentsRead != expected)
                {
                    throw new Exception("Number of moments read from referenced file does not match number of moments value in model. " +
                        $"Reads: { momentsRead} - model.MomentReferences[{referenceIndex}].NumberOfMoment: {expected}"); 
                }

                referenceIndex++;
                if (referenceIndex < model.MomentReferences.Length)
                {
                    currentRef = CreateOpenReference();
                }
                momentsRead = 0;
                return Next();
            }
            else
            {
                momentsRead++;
                return moment;
            }
        }

        private OpenReference CreateOpenReference()
        {
            var filepath = Path.Combine(workingDir, model.MomentReferences[referenceIndex].MomentsFile);
            return new OpenReference(filepath);
        }

        private class OpenReference
        {
            private readonly FileStream file;
            private readonly StreamReader reader;

            public OpenReference(string filePath)
            {
                file = File.OpenRead(filePath);
                reader = new StreamReader(file);
            }

            public OverwatchMoment? ReadNext()
            {
                var line = reader.ReadLine();
                if (string.IsNullOrEmpty(line)) return null;
                return JsonConvert.DeserializeObject<OverwatchMoment>(line);
            }

            public void Close()
            {
                reader.Close();
                file.Close();

                reader.Dispose();
                file.Dispose();
            }
        }
    }
}
