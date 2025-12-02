using System.Text.Json;
using System.Text.Json.Serialization;

namespace LCTWorks.Core.Helpers;

public class ExtendedDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var str = reader.GetString();
        if (DateTimeOffset.TryParse(str, out var dto))
        {
            return dto;
        }

        if (!string.IsNullOrEmpty(str) && str.Contains(' '))
        {
            var iso = str.Replace(" ", "T");
            if (DateTimeOffset.TryParse(iso, out dto))
            {
                return dto;
            }
        }

        throw new JsonException($"Invalid DateTimeOffset format: {str}");
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("o"));
    }
}