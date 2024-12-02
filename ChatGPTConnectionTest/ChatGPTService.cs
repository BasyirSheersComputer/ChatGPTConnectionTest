using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ChatGPTConnectionTest
{
    public class ChatGPTService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public ChatGPTService(string apiKey)
        {
            _apiKey = apiKey;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }

        public async Task<string> SendMessageWithRetryAsync(string message, int maxRetries = 5, int initialDelayMilliseconds = 2000)
        {
            int retryCount = 0;
            int delayMilliseconds = initialDelayMilliseconds;

            while (retryCount < maxRetries)
            {
                try
                {
                    return await SendMessageAsync(message);
                }
                catch (Exception ex) when (ex.Message.Contains("Rate limit exceeded"))
                {
                    retryCount++;
                    Console.WriteLine($"Rate limit exceeded. Retrying in {delayMilliseconds / 1000} seconds... (Attempt {retryCount}/{maxRetries})");

                    await Task.Delay(delayMilliseconds);

                    // Exponential backoff with a cap
                    delayMilliseconds = Math.Min(delayMilliseconds * 2, 60000); // Max delay 60 seconds
                }
            }

            throw new Exception("Exceeded maximum retry attempts due to rate limiting.");
        }

        private async Task<string> SendMessageAsync(string message)
        {
            var endpoint = "https://api.openai.com/v1/chat/completions";

            var requestBody = new
            {
                model = "gpt-4o-mini",
                messages = new[]
                {
                    new { role = "user", content = message }
                },
                max_tokens = 50,
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(endpoint, jsonContent);

            if (response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                var responseJson = JsonDocument.Parse(responseData);
                var reply = responseJson.RootElement
                                        .GetProperty("choices")[0]
                                        .GetProperty("message")
                                        .GetProperty("content")
                                        .GetString();

                return reply ?? "No response content.";
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                throw new Exception("Rate limit exceeded. Please wait before making additional requests.");
            }
            else
            {
                throw new Exception($"Failed to communicate with ChatGPT API: {response.ReasonPhrase}");
            }
        }
    }
}
