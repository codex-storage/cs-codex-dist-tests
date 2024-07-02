namespace AutoClient
{
    public class ImageGenerator
    {
        public async Task<string> GenerateImage()
        {
            var httpClient = new HttpClient();
            var thing = await httpClient.GetStreamAsync("https://picsum.photos/3840/2160");

            var filename = $"{Guid.NewGuid().ToString().ToLowerInvariant()}.jpg";
            using var file = File.OpenWrite(filename);
            await thing.CopyToAsync(file);

            return filename;
        }
    }
}
