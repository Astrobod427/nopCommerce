using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Nop.Plugin.Misc.RestApi.Api;

/// <summary>
/// JSON conventions of the API: camelCase properties, enums as strings, UTC ISO dates
/// </summary>
public static class ApiJson
{
    public static JsonSerializerSettings Settings { get; } = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        Converters = { new StringEnumConverter() },
        DateTimeZoneHandling = DateTimeZoneHandling.Utc,
        NullValueHandling = NullValueHandling.Include
    };
}
