using System.Text;
using Utils;

namespace FileUtils
{
    public class GeneratorCollection : IGenerateOption
    {
        private readonly List<IGenerator> generators = new List<IGenerator>();

        public IGenerateOption ByteRepeat(byte[] bytes, ByteSize size)
        {
            var times = size.SizeInBytes / bytes.Length;
            generators.Add(new ByteRepeater(bytes, times));
            return this;
        }

        public IGenerateOption ByteRepeat(byte[] bytes, int times)
        {
            generators.Add(new ByteRepeater(bytes, times));
            return this;
        }

        public IGenerateOption Random(ByteSize size)
        {
            generators.Add(new RandomGenerator(size));
            return this;
        }

        public IGenerateOption StringRepeat(string str, ByteSize size)
        {
            var times = size.SizeInBytes / str.Length;
            generators.Add(new StringRepeater(str, times));
            return this;
        }

        public IGenerateOption StringRepeat(string str, int times)
        {
            generators.Add(new StringRepeater(str, times));
            return this;
        }

        public void Run(FileStream file)
        {
            foreach (var generator in generators)
            {
                generator.Generate(file);
            }
        }

        public long GetRequiredSpace()
        {
            return generators.Sum(g => g.GetRequiredSpace());
        }
    }

    public interface IGenerator
    {
        void Generate(FileStream file);
        long GetRequiredSpace();
    }

    public class ByteRepeater : IGenerator
    {
        private readonly byte[] bytes;
        private readonly long times;

        public ByteRepeater(byte[] bytes, long times)
        {
            this.bytes = bytes;
            this.times = times;
        }

        public void Generate(FileStream file)
        {
            for (var i = 0; i < times; i++)
            {
                file.Write(bytes, 0, bytes.Length);
            }
        }

        public long GetRequiredSpace()
        {
            return bytes.Length * times;
        }
    }

    public class StringRepeater : IGenerator
    {
        private readonly string str;
        private readonly long times;

        public StringRepeater(string str, long times)
        {
            this.str = str;
            this.times = times;
        }

        public void Generate(FileStream file)
        {
            using var writer = new StreamWriter(file);
            for (var i = 0; i < times; i++)
            {
                writer.Write(str);
            }
        }

        public long GetRequiredSpace()
        {
            return Encoding.ASCII.GetBytes(str).Length * times;
        }
    }

    public class RandomGenerator : IGenerator
    {
        private readonly Random random = new Random();
        private readonly ByteSize size;

        public RandomGenerator(ByteSize size)
        {
            this.size = size;
        }

        public void Generate(FileStream file)
        {
            var bytesLeft = size.SizeInBytes;
            while (bytesLeft > 0)
            {
                var size = Math.Min(bytesLeft, FileManager.ChunkSize);
                var bytes = new byte[size];
                random.NextBytes(bytes);
                file.Write(bytes, 0, bytes.Length);
                bytesLeft -= size;
            }
        }

        public long GetRequiredSpace()
        {
            return size.SizeInBytes;
        }
    }
}
