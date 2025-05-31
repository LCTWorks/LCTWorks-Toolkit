using System.Text.Json;
using System.Text.Json.Serialization;

namespace LCTWorks.Common.Helpers
{
    public static class Json
    {
        private static readonly Lock _lock = new();
        private static JsonSerializerOptions? _defaultJsonSerializerOptions;

        public static JsonSerializerOptions DefaultJsonSerializerOptions
        {
            get
            {
                lock (_lock)
                {
                    return _defaultJsonSerializerOptions ??= CreateSerializerOptions();
                }
            }
        }

        public static string Stringify(object? value)
            => TrySerialize(value);

        public static async Task<string> StringifyAsync(object value)
        {
            return await Task.Run(() =>
            {
                return TrySerialize(value);
            });
        }

        public static T? ToObject<T>(string value)
            => TryDeserialize<T>(value);

        public static async Task<T?> ToObjectAsync<T>(string value)
        {
            try
            {
                return await Task.Run(() =>
                {
                    return TryDeserialize<T>(value);
                });
            }
            catch (Exception)
            {
                return default;
            }
        }

        private static JsonSerializerOptions CreateSerializerOptions(bool writeIndented = false)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = new LowerCaseNamingPolicy(),
                WriteIndented = writeIndented,
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            };
            options.Converters.Add(new ExtendedDateTimeOffsetConverter());
            return options;
        }

        private static T? TryDeserialize<T>(string value)
        {
            try
            {
                return JsonSerializer.Deserialize<T>(value, DefaultJsonSerializerOptions);
            }
            catch
            {
            }
            return default;
        }

        private static string TrySerialize(object? value)
        {
            try
            {
                return JsonSerializer.Serialize(value, DefaultJsonSerializerOptions);
            }
            catch
            {
            }
            return string.Empty;
        }
    }
}