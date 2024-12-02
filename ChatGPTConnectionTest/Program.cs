using ChatGPTConnectionTest;

var apiKey = "xxxx"; // Replace with your OpenAI API key
var chatService = new ChatGPTService(apiKey);

try
{
    var response = await chatService.SendMessageWithRetryAsync("Hello, ChatGPT!");
    Console.WriteLine("Response from ChatGPT: " + response);
}
catch (Exception ex)
{
    Console.WriteLine("Error: " + ex.Message);
}