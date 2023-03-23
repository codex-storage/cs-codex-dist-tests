namespace CodexDistTestCore
{
    public interface IPodLogsHandler
    {
        void Log(int id, string podDescription, Stream log);
    }
}
