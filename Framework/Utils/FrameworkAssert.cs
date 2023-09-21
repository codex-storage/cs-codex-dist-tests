namespace Utils
{
    public static class FrameworkAssert
    {
        public static void That(bool condition, string message)
        {
            if (!condition) Fail(message);
        }

        public static void Fail(string message)
        {
            throw new Exception(message);
        }
    }
}
