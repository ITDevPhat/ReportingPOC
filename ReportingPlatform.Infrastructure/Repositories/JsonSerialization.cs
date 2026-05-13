using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReportingPlatform.Infrastructure.Repositories;

internal static class JsonSerialization
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    static JsonSerialization()
    {
        Options.Converters.Add(new JsonStringEnumConverter());
    }
}
