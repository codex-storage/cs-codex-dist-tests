using Discord.WebSocket;

namespace BiblioTech.Options
{
    public class CommandContext
    {
        private const string AttachmentFolder = "attachments";

        public CommandContext(SocketSlashCommand command, IReadOnlyCollection<SocketSlashCommandDataOption> options)
        {
            Command = command;
            Options = options;

            var attachmentPath = Path.Combine(Program.Config.DataPath, AttachmentFolder);
            if (Directory.Exists(attachmentPath))
            {
                Directory.CreateDirectory(attachmentPath);
            }
        }

        public SocketSlashCommand Command { get; }
        public IReadOnlyCollection<SocketSlashCommandDataOption> Options { get; }

        public async Task Followup(string line)
        {
            var array = line.Split(Environment.NewLine);
            await Followup(array);
        }

        public async Task Followup(string[] lines)
        {
            var chunker = new LineChunker(lines);
            var chunks = chunker.GetChunks();
            if (!chunks.Any()) return;

            // First chunk is a modification of the original message.
            // Everything after that, we must create a new message.
            var first = chunks.First();
            chunks.RemoveAt(0);

            await Command.ModifyOriginalResponseAsync(m =>
            {
                m.Content = FormatChunk(first);
            });

            foreach (var remaining in chunks)
            {
                await Command.FollowupAsync(FormatChunk(remaining));
            }
        }

        private string FormatChunk(string[] chunk)
        {
            return string.Join(Environment.NewLine, chunk);
        }
    }

    public class LineChunker
    {
        private readonly List<string> input;
        private readonly int maxCharacters;

        public LineChunker(string[] input, int maxCharacters = 1950)
        {
            this.input = input.ToList();
            this.maxCharacters = maxCharacters;
        }

        public List<string[]> GetChunks()
        {
            var result = new List<string[]>();
            while (input.Any())
            {
                result.Add(GetChunk());
            }

            return result;
        }

        private string[] GetChunk()
        {
            var totalLength = 0;
            var result = new List<string>();

            while (input.Any())
            {
                var nextLine = input[0];
                var nextLength = totalLength + nextLine.Length;
                if (nextLength > maxCharacters) 
                {
                    return result.ToArray();
                }

                input.RemoveAt(0);
                result.Add(nextLine);
                totalLength += nextLine.Length;
            }

            return result.ToArray();
        }
    }
}
