using System.Text;
using System.Text.Json;
using TR.Connector.Interfaces;

namespace TR.Connector.Services
{
    public class JsonSerializerService : ISerializer
    {
        private readonly JsonSerializerOptions _options;

        public JsonSerializerService()
        {
            _options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };
        }

        public T Deserialize<T>(string json) where T : class
        {
            return JsonSerializer.Deserialize<T>(json, _options);
        }

        public StringContent CreateContent(object body)
        {
            return new StringContent(JsonSerializer.Serialize(body), UnicodeEncoding.UTF8, "application/json");
        }
    }
}
