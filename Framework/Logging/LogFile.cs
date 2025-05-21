namespace Logging
{
    public class LogFile
    {
        private readonly object fileLock = new object();

        public LogFile(string filename)
        {
            Filename = filename;

            EnsurePathExists(filename);
        }

        public string Filename { get; private set; }

        public void Write(string message)
        {
            try
            {
                lock (fileLock)
                { 
                    File.AppendAllLines(Filename, new[] { message });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Writing to log has failed: " + ex);
            }
        }

        public void WriteRawMany(IEnumerable<string> lines)
        {
            try
            {
                lock (fileLock)
                {
                    File.AppendAllLines(Filename, lines);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Writing to log has failed: " + ex);
            }
        }

        private void EnsurePathExists(string filename)
        {
            var path = new FileInfo(filename).Directory!.FullName;
            Directory.CreateDirectory(path);
        }
    }
}
