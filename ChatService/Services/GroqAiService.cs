using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Workflow.ChatService.Services
{
    public class GroqAiService : IAiService
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;

        public GroqAiService(HttpClient http, IConfiguration config)
        {
            _http = http;
            _config = config;
            _http.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<string> ReplyAsync(string userText, CancellationToken ct)
        {
            var apiKey = _config["Groq:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
                return "AI: (Groq ApiKey tanımlı değil)";

            var model = _config["Groq:Model"] ?? "llama-3.1-8b-instant";

            var payload = new
            {
                model,
                messages = new object[]
                {
                    new { role = "system", content = "Kısa, net ve Türkçe cevap ver." },
                    new { role = "user", content = userText }
                },
                temperature = 0.3
            };

            var json = JsonSerializer.Serialize(payload);

            using var req = new HttpRequestMessage(
                HttpMethod.Post,
                "https://api.groq.com/openai/v1/chat/completions" // OpenAI-compatible
            );

            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            req.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var res = await _http.SendAsync(req, ct);
            var body = await res.Content.ReadAsStringAsync(ct);

            if (!res.IsSuccessStatusCode)
            {
                var shortBody = body.Length > 400 ? body[..400] + "..." : body;
                return $"AI: (Groq hata {(int)res.StatusCode}) {shortBody}";
            }

            // OpenAI-compatible response: choices[0].message.content
            using var doc = JsonDocument.Parse(body);
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return content ?? "";
        }
    }
}
