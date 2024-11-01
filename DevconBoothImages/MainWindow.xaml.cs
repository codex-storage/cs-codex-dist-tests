using AutoClient;
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
        private string[] currentCids = Array.Empty<string>();

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
            var result = new List<string>();
            var codexes = await codexWrapper.GetCodexes();
            try
            {
                foreach (var codex in codexes)
                {
                    using (var fileStream = File.OpenRead(filename))
                    {
                        var response = await codex.UploadAsync(
                            "application/image??",
                            "attachement filanem???",
                            fileStream);

                        if (string.IsNullOrEmpty(response) ||
                            response.ToLowerInvariant().Contains("unable to store block"))
                        {
                            MessageBox.Show("Unable to upload image. Response empty or error message.");
                        }
                        else
                        {
                            result.Add(response);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Upload failed: " + ex);
            }
            Log($"Generated {result.Count} CIDs");
            currentCids = result.ToArray();
        }

        private void InfoToClipboard()
        {
            Clipboard.Clear();
            if (!currentCids.Any())
            {
                Log("No CIDs were generated! Clipboard cleared.");
                return;
            }

            var msg = 
                "";



            Clipboard.SetText(msg);
            Log("CID info copied to clipboard. Paste it in Discord plz!");
        }

        private void Log(string v)
        {
            Txt.Text = v;
        }
    }
}