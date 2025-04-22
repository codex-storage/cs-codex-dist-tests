namespace Logging
{
    public class FileLog : BaseLog
    {
        public FileLog(string fullFilename)
        {
            FullFilename = fullFilename;
        }

        public string FullFilename { get; }

        public override string GetFullName()
        {
            return FullFilename;
        }
    }
}
