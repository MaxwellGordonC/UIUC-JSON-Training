using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// A custom JSON converter for the DateOnly type.
/// Handles parsing from a JSON string into a DateOnly object,
/// and serializes a DateOnly object back to a string.
/// </summary>
public class DateOnlyConverter : JsonConverter<DateOnly>
{
    /// <summary>
    /// Reads the JSON string and converts it to a DateOnly object.
    /// </summary>
    /// <param name="reader">The Utf8JsonReader to read JSON data from.</param>
    /// <param name="typeToConvert">The target type to convert to, in this case DateOnly.</param>
    /// <param name="options">Serialization options for handling custom behaviors.</param>
    /// <returns>The DateOnly object parsed from the JSON string.</returns>
    /// <exception cref="JsonException">Thrown when the date format is invalid.</exception>
    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Attempt to parse the date string from JSON into DateOnly.
        if (DateOnly.TryParse(reader.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return date;
        }

        // Throw a JsonException if parsing fails.
        throw new JsonException($"Unable to parse date: {reader.GetString()}");
    }

    /// <summary>
    /// Writes the DateOnly object back to a JSON string.
    /// </summary>
    /// <param name="writer">The Utf8JsonWriter used to write JSON data.</param>
    /// <param name="value">The DateOnly value to serialize.</param>
    /// <param name="options">Serialization options for handling custom behaviors.</param>
    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
    {
        // Serialize the DateOnly object to a string in the default format.
        writer.WriteStringValue(value.ToString());
    }
}
