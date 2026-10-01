public class EventRecord : ICsvSerializable
{
    public EventType Type { get; set; }
    public DateTime Timestamp { get; set; }

    public string[] ToCsvFields()
    {
        return new[]
        {
            Type.ToString(),
            Timestamp.ToString("o")
        };
    }

    public void FromCsvFields(string[] fields)
    {
        Type = Enum.Parse<EventType>(fields[0]);
        Timestamp = DateTime.Parse(fields[1]);
    }
}
