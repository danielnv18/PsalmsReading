using System.Text.Json;
using System.Text.Json.Serialization;

namespace PsalmsReading.Api.Json;

internal sealed class DateOnlyJsonConverter : JsonConverter<DateOnly>
{
    private const string Format = "yyyy-MM-dd";

    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (value is null)
        {
            throw new JsonException("Date value is required.");
        }

        if (!DateOnly.TryParse(value, out var date))
        {
            throw new JsonException("Invalid date format. Use yyyy-MM-dd.");
        }

        return date;
    }

    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(Format));
    }
}
