using FileUtils;
using Logging;
using Utils;

namespace AutoClient
{
    public interface IFileGenerator
    {
        Task<string> Generate();
    }

    public class ImageGenerator : IFileGenerator
    {
        private readonly ILog log;

        public ImageGenerator(ILog log)
        {
            this.log = log;
        }

        public async Task<string> Generate()
        {
            log.Debug("Fetching random image from picsum.photos...");
            var httpClient = new HttpClient();
            var thing = await httpClient.GetStreamAsync("https://picsum.photos/3840/2160");

            var filename = $"{Guid.NewGuid().ToString().ToLowerInvariant()}.jpg";
            using var file = File.OpenWrite(filename);
            await thing.CopyToAsync(file);

            return filename;
        }
    }

    public class RandomFileGenerator : IFileGenerator
    {
        private readonly ByteSize size;
        private readonly FileManager fileManager;

        public RandomFileGenerator(Configuration config, ILog log)
        {
            size = config.FileSizeMb.MB();
            fileManager = new FileManager(log, config.DataPath);
        }

        public Task<string> Generate()
        {
            return Task.Run(() =>
            {
                var file = fileManager.GenerateFile(size);
                return file.Filename;
            });
        }
    }
}
