namespace Logging
{
    public class LogFile
    {
        private readonly string extension;
        private readonly object fileLock = new object();
        private string filename;

        public LogFile(string filename, string extension)
        {
            this.filename = filename;
            this.extension = extension;
            FullFilename = filename + "." + extension;

            EnsurePathExists(filename);
        }

        public string FullFilename { get; private set; }

        public void Write(string message)
        {
            WriteRaw($"{GetTimestamp()} {message}");
        }

        public void WriteRaw(string message)
        {
            try
            {
                lock (fileLock)
                { 
                    File.AppendAllLines(FullFilename, new[] { message });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Writing to log has failed: " + ex);
            }
        }

        public void ConcatToFilename(string toAdd)
        {
            var oldFullName = FullFilename;

            filename += toAdd;
            FullFilename = filename + "." + extension;

            File.Move(oldFullName, FullFilename);
        }

        private static string GetTimestamp()
        {
            return $"[{DateTime.UtcNow.ToString("o")}]";
        }

        private void EnsurePathExists(string filename)
        {
            var path = new FileInfo(filename).Directory!.FullName;
            Directory.CreateDirectory(path);
        }
    }
}
