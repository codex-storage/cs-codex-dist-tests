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
            Timeliner3.PropertyChanged += Timeliner3_PropertyChanged;
        }

        private void Timeliner3_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Timeliner4.LeftEdge = Timeliner3.LeftEdge;
            Timeliner4.RightEdge = Timeliner3.RightEdge;
        }

        private void Timeliner1_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Timeliner2.LeftEdge = Timeliner1.LeftEdge;
            Timeliner2.RightEdge = Timeliner1.RightEdge;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //var path = "d:\\Projects\\cs-codex-dist-tests\\Tests\\CodexReleaseTests\\bin\\Debug\\net8.0\\CodexTestLogs\\2024-12\\20\\09-57-01Z_TwoClientTests\\";
            var path = "C:\\Projects\\cs-codex-dist-tests\\Tests\\CodexReleaseTests\\bin\\Debug\\net8.0\\CodexTestLogs\\2025-01\\06\\09-45-03Z_TwoClientTests\\";
            var file1 = Path.Combine(path, "TwoClientTest[thatbenbierens_nim-codex_blkex-cancelpresence-14]_000001_Downloader1.log");
            var file2 = Path.Combine(path, "TwoClientTest[thatbenbierens_nim-codex_blkex-cancelpresence-14]_000000_Uploader0.log");
            var file3 = Path.Combine(path, "TwoClientTest[thatbenbierens_nim-codex_blkex-cancelpresence-15]_000001_Downloader1.log");
            var file4 = Path.Combine(path, "TwoClientTest[thatbenbierens_nim-codex_blkex-cancelpresence-15]_000000_Uploader0.log");

            Line1Name.Text = file1;
            Line2Name.Text = file2;
            Line3Name.Text = file3;
            Line4Name.Text = file4;

            var set1 = new EventSet();
            set1.AddFile(file1);

            var addrs1 = set1.Addresses.Take(5).ToArray();
            set1.KeepOnly(addrs1);

            var set2 = new EventSet();
            set2.AddFile(file2);

            set2.KeepOnly(addrs1);

            var set3 = new EventSet();
            set3.AddFile(file3);

            var addrs2 = set3.Addresses.Take(5).ToArray();
            set3.KeepOnly(addrs2);

            var set4 = new EventSet();
            set4.AddFile(file4);

            set4.KeepOnly(addrs2);

            var factor = 3600.0 * 24.0;
            var now1 = set1.Earliest;
            set1.Scale(from: now1, factor);
            set2.Scale(from: now1, factor);

            var now2 = set3.Earliest;
            set3.Scale(from: now2, factor);
            set4.Scale(from: now2, factor);

            DisplaySet(set1, Timeliner1);
            DisplaySet(set2, Timeliner2);
            DisplaySet(set3, Timeliner3);
            DisplaySet(set4, Timeliner4);

            //var end1 = set2.Latest;
            Timeliner1.Now = now1;
            Timeliner2.Now = now1;

            Timeliner1.LeftEdge = now1;
            //Timeliner1.RightEdge = end1;
            Timeliner2.LeftEdge = now1;
            //Timeliner2.RightEdge = end1;

            //var end2 = set3.Latest;
            Timeliner3.Now = now2;
            Timeliner4.Now = now2;

            Timeliner3.LeftEdge = now2;
            //Timeliner3.RightEdge = end2;
            Timeliner4.LeftEdge = now2;
            //Timeliner4.RightEdge = end2;
        }

        private void DisplaySet(EventSet set, Timeliner timeliner)
        {
            timeliner.Data = new TimelinerData()
            {
                Items = CreateItems(set)
            };
        }

        private List<TimelinerItem> CreateItems(EventSet set)
        {
            var result = new List<TimelinerItem>();

            set.Iterate((addr, events) =>
            {
                //result.Add(CreateItem(addr, events));
                foreach (var e in events)
                {
                    result.Add(new TimelinerItem()
                    {
                        Name = addr + " " + e.Name,
                        Jobs = new List<TimelinerJob>
                        {
                            CreateJob(e)
                        }
                    });
                }
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
