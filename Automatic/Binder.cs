using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

public static class CsvBinder
{
    // -----------------------------
    // SERIALIZATION
    // -----------------------------
    public static string[] ToFields<T>(T obj)
    {
        var props = GetOrderedProperties<T>();
        var fields = new string[props.Length];

        for (int i = 0; i < props.Length; i++)
        {
            var prop = props[i];
            var value = prop.GetValue(obj);

            if (prop.PropertyType == typeof(List<EventRecord>))
            {
                fields[i] = SerializeEventList((List<EventRecord>)value);
                continue;
            }

            fields[i] = value == null ? "" : value.ToString();
        }

        return fields;
    }

    private static string SerializeEventList(List<EventRecord> events)
    {
        if (events == null || events.Count == 0)
            return "";

        var sb = new StringBuilder();

        foreach (var e in events)
        {
            var fields = e.ToCsvFields();
            sb.Append(string.Join("|", fields)); // pipe-delimited event
            sb.Append(";");
        }

        return sb.ToString().TrimEnd(';');
    }

    // -----------------------------
    // DESERIALIZATION
    // -----------------------------
    public static T FromFields<T>(string[] fields) where T : new()
    {
        var obj = new T();
        var props = GetOrderedProperties<T>();

        if (fields.Length != props.Length)
            throw new InvalidOperationException("CSV field count mismatch.");

        for (int i = 0; i < props.Length; i++)
        {
            var prop = props[i];
            var attr = (CsvColumnAttribute)prop.GetCustomAttributes(typeof(CsvColumnAttribute), false)[0];
            var raw = fields[i];

            if (attr.Required && string.IsNullOrWhiteSpace(raw))
                throw new InvalidOperationException("Required CSV field missing: " + prop.Name);

            if (prop.PropertyType == typeof(List<EventRecord>))
            {
                var list = DeserializeEventList(raw);
                prop.SetValue(obj, list);
                continue;
            }

            object parsed = ParseValue(prop.PropertyType, raw);
            prop.SetValue(obj, parsed);
        }

        return obj;
    }

    private static List<EventRecord> DeserializeEventList(string raw)
    {
        var list = new List<EventRecord>();

        if (string.IsNullOrWhiteSpace(raw))
            return list;

        var eventStrings = raw.Split(';');

        foreach (var es in eventStrings)
        {
            var fields = es.Split('|');
            var ev = new EventRecord();
            ev.FromCsvFields(fields);
            list.Add(ev);
        }

        return list;
    }

    // -----------------------------
    // TYPE PARSING
    // -----------------------------
    private static object ParseValue(Type type, string raw)
    {
        if (type == typeof(string)) return raw;
        if (type == typeof(int)) return int.Parse(raw);
        if (type == typeof(bool)) return bool.Parse(raw);
        if (type == typeof(DateTime)) return DateTime.Parse(raw);
        if (type.IsEnum) return Enum.Parse(type, raw);

        throw new NotSupportedException("Unsupported CSV type: " + type.Name);
    }

    // -----------------------------
    // PROPERTY DISCOVERY
    // -----------------------------
    private static PropertyInfo[] GetOrderedProperties<T>()
    {
        return typeof(T)
            .GetProperties()
            .Where(p => Attribute.IsDefined(p, typeof(CsvColumnAttribute)))
            .OrderBy(p => ((CsvColumnAttribute)p.GetCustomAttributes(typeof(CsvColumnAttribute), false)[0]).Order)
            .ToArray();
    }
}
