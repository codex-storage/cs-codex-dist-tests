namespace ArgsUniform
{
    public class ExampleUser
    {
        public class Args
        {
            [Uniform("aaa", "a", "AAA", false, "Sets the AAA!")]
            public string Aaa { get; set; } = string.Empty;

            [Uniform("bbb", "b", "BBB", true, "Sets that BBB")]
            public string Bbb { get; set; } = string.Empty;
        }

        public class DefaultsProvider
        {
            public string Aaa { get { return "non-static operation"; } }
        }

        public void Example()
        {
            // env var: "AAA=BBB"
            var args = "--ccc=ddd";

            var uniform = new ArgsUniform<Args>(new DefaultsProvider(), args);

            var aaa = uniform.Parse();
        }
    }
}
