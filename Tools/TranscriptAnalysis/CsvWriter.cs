namespace TranscriptAnalysis
{
    public class CsvWriter
    {
        public ICsv CreateNew()
        {
            return new Csv();
        }

        public void Write(ICsv csv, string filename)
        {
            var c = (Csv)csv;

            using var file = File.OpenWrite(filename);
            using var writer = new StreamWriter(file);
            c.CreateLines(writer.WriteLine);
        }
    }

    public interface ICsv
    {
        ICsvColumn GetColumn(string title, float defaultValue);
        ICsvColumn GetColumn(string title, string defaultValue);
        void AddRow(params CsvCell[] cells);
    }

    public class Csv : ICsv
    {
        private readonly string Sep = ",";
        private readonly List<CsvColumn> columns = new List<CsvColumn>();
        private readonly List<CsvRow> rows = new List<CsvRow>();

        public ICsvColumn GetColumn(string title, float defaultValue)
        {
            return GetColumn(title, defaultValue.ToString());
        }

        public ICsvColumn GetColumn(string title, string defaultValue)
        {
            var column = columns.SingleOrDefault(c => c.Title == title);
            if (column == null)
            {
                column = new CsvColumn(title, defaultValue);
                columns.Add(column);
            }
            return column;
        }

        public void AddRow(params CsvCell[] cells)
        {
            rows.Add(new CsvRow(cells));
        }

        public void CreateLines(Action<string> onLine)
        {
            CreateHeaderLine(onLine);
            foreach (var row in rows)
            {
                CreateRowLine(row, onLine);
            }
        }

        private void CreateHeaderLine(Action<string> onLine)
        {
            onLine(string.Join(Sep, columns.Select(c => c.Title).ToArray()));
        }

        private void CreateRowLine(CsvRow row, Action<string> onLine)
        {
            onLine(string.Join(Sep, columns.Select(c => GetRowCellValue(row, c)).ToArray()));
        }

        private string GetRowCellValue(CsvRow row, CsvColumn column)
        {
            var cell = row.Cells.SingleOrDefault(c => c.Column == column);
            if (cell == null) return column.DefaultValue;
            return cell.Value;
        }
    }

    public class CsvCell
    {
        public CsvCell(ICsvColumn column, float value)
            : this(column, value.ToString())
        {
        }

        public CsvCell(ICsvColumn column, string value)
        {
            Column = column;
            Value = value;
        }

        public ICsvColumn Column { get; }
        public string Value { get; }
    }

    public interface ICsvColumn
    {
        string Title { get; }
        string DefaultValue { get; }
    }

    public class CsvColumn : ICsvColumn
    {
        public CsvColumn(string title, string defaultValue)
        {
            Title = title;
            DefaultValue = defaultValue;
        }

        public string Title { get; }
        public string DefaultValue { get; }
    }

    public class CsvRow
    {
        public CsvRow(CsvCell[] cells)
        {
            Cells = cells;
        }

        public CsvCell[] Cells { get; }
    }
}
