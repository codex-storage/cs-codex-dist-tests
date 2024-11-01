using AutoClient;
using CodexOpenApi;
using Logging;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace DevconBoothImages
{
    public partial class MainWindow : Window
    {
        private readonly Configuration config = new Configuration();
        private readonly CodexWrapper codexWrapper = new CodexWrapper();
        private readonly ImageGenerator imageGenerator = new ImageGenerator(new NullLog());
        private string currentLocalCid = string.Empty;
        private string currentPublicCid = string.Empty;

        public MainWindow()
        {
            InitializeComponent();

            Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
        }

        private void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show("Unhandled exception: " + e.Exception);
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            // image
            Log("Getting image...");
            var file = await imageGenerator.Generate();
            var filename = Path.Combine(config.WorkingDir, file);
            File.Copy(file, filename);

            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(filename);
            bmp.EndInit();
            Img.Source = bmp;

            Log("Uploading...");
            // upload
            await UploadToCodexes(filename, file);

            // clipboard info
            InfoToClipboard();
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            InfoToClipboard();
        }

        private async void Button_Click_2(object sender, RoutedEventArgs e)
        {
            // check codexes
            Log("Checking Codex connections...");
            await codexWrapper.GetCodexes();
            Log("Connections OK");
        }

        private async Task UploadToCodexes(string filename, string shortName)
        {
            var codexes = await codexWrapper.GetCodexes();
            try
            {
                currentLocalCid = await UploadFile(filename, shortName, codexes.Local);
                currentPublicCid = await UploadFile(filename, shortName, codexes.Testnet);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Upload failed: " + ex);
            }
            Log($"Generated CIDs");
        }

        private async Task<string> UploadFile(string filename, string shortName, CodexApi codex)
        {
            using (var fileStream = File.OpenRead(filename))
            {
                var response = await codex.UploadAsync(
                    "image/jpeg",
                    $"attachment; filename=\"{shortName}\"",
                    fileStream);

                if (string.IsNullOrEmpty(response) ||
                    response.ToLowerInvariant().Contains("unable to store block"))
                {
                    throw new Exception("Unable to upload image. Response empty or error message.");
                }
                return response;
            }
        }

        private void InfoToClipboard()
        {
            Clipboard.Clear();
            if (string.IsNullOrEmpty(currentLocalCid) || string.IsNullOrEmpty(currentPublicCid))
            {
                Log("No CIDs were generated! Clipboard cleared.");
                return;
            }

            var nl = Environment.NewLine;
            var msg = 
                $"** Codex@Devcon 💻 Raspberry Pi Challenge **{nl}" +
                $"📢 A new image is available. Download it and bring it to the booth!{nl}" +
                $"Public Testnet CID: `{currentPublicCid}`{nl}" +
                $"Local Devcon network CID: `{currentLocalCid}`{nl}" +
                $"Setup instructions: [Here](https://docs.codex.storage){nl}" +
                $"Local Devcon network information: [Here](todo)";

            Clipboard.SetText(msg);
            Log("CID info copied to clipboard. Paste it in Discord plz!");
        }

        private void Log(string v)
        {
            Txt.Text = v;
        }
    }
}
