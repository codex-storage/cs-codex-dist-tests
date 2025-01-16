namespace AutoClient.Modes
{
    public interface IMode
    {
        void Start(CodexWrapper node, int index);
        void Stop();
    }
}
