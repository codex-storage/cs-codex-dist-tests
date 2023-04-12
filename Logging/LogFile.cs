namespace Logging
{
    public class LogFile
    {
        private readonly DateTime now;
        private string name;
        private readonly string ext;
        private readonly string filepath;

        public LogFile(LogConfig config, DateTime now, string name, string ext = "log")
        {
            this.now = now;
            this.name = name;
            this.ext = ext;

            filepath = Path.Join(
                config.LogRoot,
                $"{now.Year}-{Pad(now.Month)}",
                Pad(now.Day));

            Directory.CreateDirectory(filepath);

            GenerateFilename();
        }

        public string FullFilename { get; private set; } = string.Empty;
        public string FilenameWithoutPath { get; private set; } = string.Empty;

        public void Write(string message)
        {
            WriteRaw($"{GetTimestamp()} {message}");
        }

        public void WriteRaw(string message)
        {
            try
            {
                File.AppendAllLines(FullFilename, new[] { message });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Writing to log has failed: " + ex);
            }
        }

        public void ConcatToFilename(string toAdd)
        {
            var oldFullName = FullFilename;

            name += toAdd;

            GenerateFilename();

            File.Move(oldFullName, FullFilename);
        }

        private static string Pad(int n)
        {
            return n.ToString().PadLeft(2, '0');
        }

        private static string GetTimestamp()
        {
            return $"[{DateTime.UtcNow.ToString("u")}]";
        }

        private void GenerateFilename()
        {
            FilenameWithoutPath = $"{Pad(now.Hour)}-{Pad(now.Minute)}-{Pad(now.Second)}Z_{name.Replace('.', '-')}.{ext}";
            FullFilename = Path.Combine(filepath, FilenameWithoutPath);
        }
    }
}
