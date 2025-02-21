using System.Globalization;

namespace CodexClient
{
    public class CodexLogLine
    {
        public static CodexLogLine? Parse(string line)
        {
            try
            {
                if (string.IsNullOrEmpty(line) ||
                    line.Length < 34 ||
                    line[3] != ' ' ||
                    line[33] != ' ') return null;

                line = line.Replace(Environment.NewLine, string.Empty);

                var level = line.Substring(0, 3);
                var dtLine = line.Substring(4, 23);

                var firstEqualSign = line.IndexOf('=');
                var msgStart = 34;
                var msgEnd = line.Substring(0, firstEqualSign).LastIndexOf(' ');
                var msg = line.Substring(msgStart, msgEnd - msgStart).Trim();
                var attrsLine = line.Substring(msgEnd);

                var attrs = SplitAttrs(attrsLine);

                var format = "yyyy-MM-dd HH:mm:ss.fff";
                var dt = DateTime.ParseExact(dtLine, format, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).ToUniversalTime();

                return new CodexLogLine()
                {
                    LogLevel = level,
                    TimestampUtc = dt,
                    Message = msg,
                    Attributes = attrs
                };
            }
            catch
            {
                return null;
            }
        }

        public string LogLevel { get; set; } = string.Empty;
        public DateTime TimestampUtc { get; set; }
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, string> Attributes { get; private set; } = new Dictionary<string, string>();

        /// <summary>
        /// After too much time spent cursing at regexes, here's what I got:
        /// Parses input string into 'key=value' pair, considerate of quoted (") values.
        /// </summary>
        private static Dictionary<string, string> SplitAttrs(string input)
        {
            input += " ";
            var result = new Dictionary<string, string>();

            var key = string.Empty;
            var value = string.Empty;
            var mode = 1;
            var inQuote = false;

            foreach (var c in input)
            {
                if (mode == 1)
                {
                    if (c == '=') mode = 2;
                    else if (c == ' ')
                    {
                        if (string.IsNullOrEmpty(key)) continue;
                        else
                        {
                            result.Add(key, string.Empty);
                            key = string.Empty;
                            value = string.Empty;
                        }
                    }
                    else key += c;
                }
                else if (mode == 2)
                {
                    if (c == ' ' && !inQuote)
                    {
                        result.Add(key, value);
                        key = string.Empty;
                        value = string.Empty;
                        mode = 1;
                    }
                    else if (c == '\"')
                    {
                        inQuote = !inQuote;
                    }
                    else
                    {
                        value += c;
                    }
                }
            }

            return result;
        }
    }
}
