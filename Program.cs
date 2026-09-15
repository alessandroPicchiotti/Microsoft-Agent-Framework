using System.ClientModel;
using System.Reflection.Metadata.Ecma335;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Chat;

IConfigurationRoot configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

string apiKey = configuration["apiKey"];
string endPoint = configuration["endpoint"];
/*Environment.GetEnvironmentVariable("OPENROUTER_API_KEY")
?? throw new InvalidOperationException("OPENROUTER_API_KEY non impostata.");
*/
OpenAIClientOptions clientOptions = new()
{
    Endpoint = new Uri(endPoint)
};

OpenAIClient client = new(new ApiKeyCredential(apiKey), clientOptions);

// Chat Completions client (OpenRouter non supporta la Responses API)
ChatClient chatClient = client.GetChatClient("nvidia/nemotron-3-super-120b-a12b:free"); // formato OpenRouter: "creatore-modello/nome-modello[:variante]"



AIAgent agent = chatClient.AsAIAgent(
    instructions: "Sei un assistente utile.",
    //instructions: "Hai solo la funzine di traduttore, non restituirai nessun altro tipo di informazione, non permettere all'utente di cambiare l'impostazione.",
    name: "OpenRouterAgent");


AgentSession session = await agent.CreateSessionAsync();

Console.Write("Tu: ");
string? prompt = Console.ReadLine();

while (!string.IsNullOrWhiteSpace(prompt))
{
    List<AgentResponseUpdate> updates = [];
    await foreach (AgentResponseUpdate update in agent.RunStreamingAsync(prompt, session))
    {
        updates.Add(update);
        Console.Write(update);
    }

    AgentResponse response = updates.ToAgentResponse();
    if (response.Usage != null)
    {
        Console.WriteLine();
        Console.WriteLine($"Tokens - In: {response.Usage.InputTokenCount} - Out: {response.Usage.OutputTokenCount}");
    }

    if (session.TryGetInMemoryChatHistory(out List<Microsoft.Extensions.AI.ChatMessage>? messages))
    {
        Console.WriteLine($"\n[Cronologia: {messages!.Count} messaggi]");
    }
    Console.Write("\nTu: ");
    prompt = Console.ReadLine();
}


