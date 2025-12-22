using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Workflow.ChatService.Services
{
    public class OpenAiService : IAiService
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;

        public OpenAiService(HttpClient http, IConfiguration config)
        {
            _http = http;
            _config = config;

            // İsteğe bağlı: timeout
            _http.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<string> ReplyAsync(string userText, CancellationToken ct)
        {
            var apiKey = _config["OpenAI:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
                return "AI: (OpenAI ApiKey tanımlı değil)";

            var model = _config["OpenAI:Model"] ?? "gpt-4.1-mini";

            var payload = new
            {
                model,
                input = new object[]
                {
                    new {
                        role = "system",
                        content = new object[]
                        {
                            new { type = "input_text", text = "Kısa, net ve Türkçe cevap ver." }
                        }
                    },
                    new {
                        role = "user",
                        content = new object[]
                        {
                            new { type = "input_text", text = userText }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload);

            using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            req.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var res = await _http.SendAsync(req, ct);
            var body = await res.Content.ReadAsStringAsync(ct);

            if (!res.IsSuccessStatusCode)
            {
                try
                {
                    using var errDoc = JsonDocument.Parse(body);
                    var msg = errDoc.RootElement
                        .GetProperty("error")
                        .GetProperty("message")
                        .GetString();

                    var code = errDoc.RootElement
                        .GetProperty("error")
                        .GetProperty("code")
                        .GetString();

                    if (code == "insufficient_quota")
                        return "AI: OpenAI kotası/bütçesi yok. Platform > Billing’den ödeme yöntemi ekleyip limit açmalısın.";

                    return $"AI: OpenAI hata ({(int)res.StatusCode}) {msg}";
                }
                catch
                {
                    return $"AI: OpenAI hata ({(int)res.StatusCode})";
                }
            }

            // Responses API genelde output_text döner
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("output_text", out var outText) &&
                outText.ValueKind == JsonValueKind.String)
            {
                return outText.GetString() ?? "";
            }

            return "AI: (OpenAI cevabı parse edilemedi)";
        }
    }
}
