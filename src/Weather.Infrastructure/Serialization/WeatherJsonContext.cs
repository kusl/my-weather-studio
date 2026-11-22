using System.Text.Json;
using System.Text.Json.Serialization;
using Weather.Core.Models;
using Weather.Infrastructure.Nws;

namespace Weather.Infrastructure.Serialization;

[JsonSourceGenerationOptions(
    JsonSerializerDefaults.Web,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    ReadCommentHandling = JsonCommentHandling.Skip)]
[JsonSerializable(typeof(Forecast))]
[JsonSerializable(typeof(NwsPointResponse))]
[JsonSerializable(typeof(NwsForecastResponse))]
internal sealed partial class WeatherJsonContext : JsonSerializerContext;
