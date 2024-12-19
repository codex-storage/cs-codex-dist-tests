using CodexPlugin;
using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TimelinerNet;

namespace SeeTimeline
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
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

        private TimelinerData CreateTimelineData(string logfile, DateTime now)
        {
            var lines = File.ReadAllLines(logfile);
            var handler = new LogLineHandler(now);
            foreach (var line in lines)
            {
                var cline = CodexLogLine.Parse(line);
                if (cline != null)
                {
                    handler.Handle(cline);
                    if (handler.Size > 20) break;
                }
            }
            return handler.GetTimeline();
        }

        public class LogLineHandler
        {
            public class BlockReq
            {
                public string Address { get; set; } = string.Empty;
                public DateTime Created { get; set; }
                public DateTime[] WantHaveSent { get; set; } = Array.Empty<DateTime>();
                public DateTime[] PresenceRecv{ get; set; } = Array.Empty<DateTime>();
                public DateTime[] WantBlkSent { get; set; } = Array.Empty<DateTime>();
                public DateTime[] BlkRecv { get; set; } = Array.Empty<DateTime>();
                public DateTime[] CancelSent { get; set; } = Array.Empty<DateTime>();
                public DateTime[] Resolve { get; set; } = Array.Empty<DateTime>();
            }

            private readonly List<BlockReq> requests = new List<BlockReq>();
            private readonly DateTime now;
            private long? zero;

            public int Size => requests.Count;

            public LogLineHandler(DateTime now)
            {
                this.now = now;
            }

            public void Handle(CodexLogLine line)
            {
                if (line.Message != "times for") return;

                var addr = line.Attributes["addrs"];
                if (!addr.Contains("index")) return;

                // addrs="treeCid: zDz*qNvVWp, index: 0"
                // reqCreatedTime=4784825696831
                // wantHaveSentTimes=@[4784825884225]
                // presenceRecvTimes=@[4784826770921]
                // wantBlkSentTimes=@[4784826954293]
                // blkRecvTimes=@[4784829724756]
                // cancelSentTimes=@[4784830399255]
                // resolveTimes=@[4784830322141]

                var req = new BlockReq
                {
                    Address = line.Attributes["addrs"],
                    Created = ToUtc(line.Attributes["reqCreatedTime"]),
                    WantHaveSent = ToUtcs(line.Attributes["wantHaveSentTimes"]),
                    PresenceRecv = ToUtcs(line.Attributes["presenceRecvTimes"]),
                    WantBlkSent = ToUtcs(line.Attributes["wantBlkSentTimes"]),
                    BlkRecv = ToUtcs(line.Attributes["blkRecvTimes"]),
                    CancelSent = ToUtcs(line.Attributes["cancelSentTimes"]),
                    Resolve = ToUtcs(line.Attributes["resolveTimes"])
                };

                requests.Add(req);
            }

            private DateTime[] ToUtcs(string str)
            {
                var tokens = str.Split(",");
                return tokens
                    .Select(t => t
                        .Replace(" ", "")
                        .Replace("@[", "")
                        .Replace("]", "")
                    ).Select(t => ToUtc(t)).ToArray();

            }

            private DateTime ToUtc(string str)
            {
                // is nanoseconds from arbitrary time point.
                var monotime = Convert.ToInt64(str);
                if (!zero.HasValue) zero = monotime;

                double deltaNanoseconds = (monotime - zero.Value);
                var delta = deltaNanoseconds / (1000 * 1000);

                return now + TimeSpan.FromSeconds(delta * 5.0);
            }

            public TimelinerData GetTimeline()
            {
                return new TimelinerData
                {
                    Items = requests.Select(ToTimelineItem).ToList()
                };
            }

            private TimelinerItem ToTimelineItem(BlockReq req)
            {
                return new TimelinerItem
                {
                    Name = req.Address,
                    Jobs = CreateTimelineJobs(req)
                };
            }

            private List<TimelinerJob> CreateTimelineJobs(BlockReq req)
            {
                var result = new List<TimelinerJob>();

                AddJobs(result, "Created", Colors.Red, req.Created);
                AddJobs(result, "WantHaveSent", Colors.Orange, req.WantHaveSent);
                AddJobs(result, "PresenceRecv", Colors.Yellow, req.PresenceRecv);
                AddJobs(result, "WantBlkSent", Colors.Green, req.WantBlkSent);
                AddJobs(result, "BlkRecv", Colors.Blue, req.BlkRecv);
                AddJobs(result, "CancelSent", Colors.Purple, req.CancelSent);
                AddJobs(result, "Resolve", Colors.Pink, req.Resolve);

                return result;
            }

            private void AddJobs(List<TimelinerJob> result, string name, Color color, params DateTime[] moments)
            {
                var i = 0;
                foreach (var dt in moments)
                {
                    result.Add(new TimelinerJob
                    {
                        Name = name + i.ToString(),
                        Begin = dt,
                        End = dt,
                        Color = new SolidColorBrush(color),
                    });
                    i++;
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var now = DateTime.Now;
            var dlg = new OpenFileDialog();
            if (dlg.ShowDialog() != true) return;
            var file1 = dlg.FileName;
            Line1Name.Text = file1;
            if (dlg.ShowDialog() != true) return;
            var file2 = dlg.FileName;
            Line2Name.Text = file2;

            var data1 = CreateTimelineData(file1, now);
            var data2 = CreateTimelineData(file2, now);

            Timeliner1.Data = data1;
            Timeliner2.Data = data2;
        }
    }
}
