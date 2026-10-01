public class CsvOptions
{
    public char Delimiter { get; set; } = ',';
    public bool IncludeHeader { get; set; } = true;
    public bool TrimWhitespace { get; set; } = true;
}


public interface ICsvSerializable
{
    string[] ToCsvFields();
    void FromCsvFields(string[] fields);
}


public class CsvService
{
    private readonly CsvOptions _options;

    public CsvService(CsvOptions options = null)
    {
        _options = options ?? new CsvOptions();
    }

    public string Serialize<T>(IEnumerable<T> records) where T : ICsvSerializable
    {
        var sb = new StringBuilder();

        if (_options.IncludeHeader)
        {
            var header = typeof(T)
                .GetProperties()
                .Select(p => p.Name);

            sb.AppendLine(string.Join(_options.Delimiter, header));
        }

        foreach (var r in records)
        {
            var fields = r.ToCsvFields()
                          .Select(EscapeField);

            sb.AppendLine(string.Join(_options.Delimiter, fields));
        }

        return sb.ToString();
    }

    public List<T> Deserialize<T>(string csv) where T : ICsvSerializable, new()
    {
        var lines = csv.Split('\n')
                       .Where(l => !string.IsNullOrWhiteSpace(l))
                       .ToList();

        int start = _options.IncludeHeader ? 1 : 0;

        var list = new List<T>();

        for (int i = start; i < lines.Count; i++)
        {
            var fields = ParseLine(lines[i]);
            var obj = new T();
            obj.FromCsvFields(fields);
            list.Add(obj);
        }

        return list;
    }

    private string EscapeField(string f)
    {
        if (f.Contains(_options.Delimiter) || f.Contains('"'))
            return $"\"{f.Replace("\"", "\"\"")}\"";

        return f;
    }

    private string[] ParseLine(string line)
    {
        // Deterministic CSV parser (handles quotes)
        var result = new List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;

        foreach (var c in line)
        {
            if (c == '"' && !inQuotes)
            {
                inQuotes = true;
                continue;
            }
            if (c == '"' && inQuotes)
            {
                inQuotes = false;
                continue;
            }
            if (c == _options.Delimiter && !inQuotes)
            {
                result.Add(sb.ToString());
                sb.Clear();
                continue;
            }

            sb.Append(c);
        }

        result.Add(sb.ToString());
        return result.ToArray();
    }
}
