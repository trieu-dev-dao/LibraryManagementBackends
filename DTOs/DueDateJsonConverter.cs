using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LibraryManagement.DTOs;

// The due deadline is a calendar date, not an instant. Preserve the submitted
// YYYY-MM-DD portion even when an older client sends an ISO timestamp.
public sealed class DueDateJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
        if (value is null || value.Length < 10 ||
            (value.Length > 10 && value[10] != 'T') ||
            !DateOnly.TryParseExact(value.AsSpan(0, 10), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var date) ||
            (value.Length > 10 && !DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out _)))
            throw new JsonException("expiredDate must be an ISO calendar date or timestamp.");
        return date.ToDateTime(TimeOnly.MinValue);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
}
