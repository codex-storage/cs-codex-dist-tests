using System.IO;
using System.Windows;
using System.Windows.Media;
using TimelinerNet;

namespace SeeTimeline
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Timeliner1.PropertyChanged += Timeliner1_PropertyChanged;
        }

        private void Timeliner1_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Timeliner2.LeftEdge = Timeliner1.LeftEdge;
            Timeliner2.RightEdge = Timeliner1.RightEdge;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Post-holiday todo:
            // TODO: way to line up upload and download events for same blockaddress
            // currently: group by address => mixed together = not very handy.
            // also: check resolution for log lines is correctly preserved, some events appear at exact same moment.

            //var dlg = new OpenFileDialog();
            var path = "d:\\Projects\\cs-codex-dist-tests\\Tests\\CodexReleaseTests\\bin\\Debug\\net8.0\\CodexTestLogs\\2024-12\\20\\09-57-01Z_TwoClientTests\\";
            var file1 = Path.Combine(path, "TwoClientTest[thatbenbierens_nim-codex_blkex-cancelpresence-14]_FAST_Downloader1.log");
            var file2 = Path.Combine(path, "TwoClientTest[thatbenbierens_nim-codex_blkex-cancelpresence-14]_FAST_Uploader0.log");
            var file3 = Path.Combine(path, "TwoClientTest[thatbenbierens_nim-codex_blkex-cancelpresence-15]_000000_Uploader0.log");
            var file4 = Path.Combine(path, "TwoClientTest[thatbenbierens_nim-codex_blkex-cancelpresence-15]_000001_Downloader1.log");

            //MessageBox.Show("Select fast-run upload and download logs.");
            //if (dlg.ShowDialog() != true) return;
            //var file1 = dlg.FileName;
            //if (dlg.ShowDialog() != true) return;
            //var file2 = dlg.FileName;
            Line1Name.Text = file1 + " / " + file2;

            //MessageBox.Show("Select slow-run upload and download logs.");
            //if (dlg.ShowDialog() != true) return;
            //var file3 = dlg.FileName;
            //if (dlg.ShowDialog() != true) return;
            //var file4 = dlg.FileName;
            Line2Name.Text = file3 + " / " + file4;

            var set1 = new EventSet();
            set1.AddFile(file1);
            set1.AddFile(file2);

            var set2 = new EventSet();
            set2.AddFile(file3);
            set2.AddFile(file4);

            set2.Move(-(set2.Earliest - set1.Earliest));

            var now = set1.Earliest;
            set1.Scale(from: now, factor: 5000.0);
            set2.Scale(from: now, factor: 5000.0);

            DisplaySet(set1, Timeliner1, max: 5);
            DisplaySet(set2, Timeliner2, max: 5);
        
            var end = set2.Latest;
            Timeliner1.Now = now;
            Timeliner2.Now = now;

            Timeliner1.LeftEdge = now;
            Timeliner1.RightEdge = end;
            Timeliner2.LeftEdge = now;
            Timeliner2.RightEdge = end;
        }

        private void DisplaySet(EventSet set, Timeliner timeliner, int max)
        {
            timeliner.Data = new TimelinerData()
            {
                Items = CreateItems(set, max)
            };
        }

        private List<TimelinerItem> CreateItems(EventSet set, int max)
        {
            var result = new List<TimelinerItem>();

            set.Iterate(max, (addr, events) =>
            {
                result.Add(CreateItem(addr, events));
            });

            return result;
        }

        private TimelinerItem CreateItem(string addr, CodexEvent[] events)
        {
            return new TimelinerItem
            {
                Name = addr,
                Jobs = CreateJobs(events)
            };
        }

        private List<TimelinerJob> CreateJobs(CodexEvent[] events)
        {
            return events.Select(CreateJob).ToList();
        }

        private TimelinerJob CreateJob(CodexEvent e)
        {
            return new TimelinerJob
            {
                Name = e.Name,
                Color = new SolidColorBrush(e.Color),
                Begin = e.Dt,
                End = e.Dt
            };
        }
    }
}
