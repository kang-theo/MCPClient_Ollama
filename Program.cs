using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;
//using Microsoft.Extensions.AI.Abstractions;
using ModelContextProtocol.Client;
//using ModelContextProtocol.FunctionCalling;
using OllamaSharp;
using Microsoft.Extensions.Logging;

/** https://learn.microsoft.com/en-us/dotnet/ai/quickstarts/chat-local-model **/
//IChatClient chatClient =
//    new OllamaApiClient(new Uri("http://10.0.4.3:11434/"), "qwen2.5:32b");

//// Start the conversation with context for the AI model
//List<ChatMessage> chatHistory = new();

//while (true)
//{
//  // Get user prompt and add to chat history
//  Console.WriteLine("Your prompt:");
//  var userPrompt = Console.ReadLine();
//  chatHistory.Add(new ChatMessage(ChatRole.User, userPrompt));

//  // Stream the AI response and add to chat history
//  Console.WriteLine("AI Response:");
//  var response = "";
//  await foreach (ChatResponseUpdate item in
//      chatClient.GetStreamingResponseAsync(chatHistory))
//  {
//    Console.Write(item.Text);
//    response += item.Text;
//  }
//  chatHistory.Add(new ChatMessage(ChatRole.Assistant, response));
//  Console.WriteLine();
//}

/**https://learn.microsoft.com/en-us/dotnet/ai/quickstarts/build-mcp-client**/
// v1: Ollama local without UseFunctionInvocation
//IChatClient client =
//    new OllamaApiClient(new Uri("http://10.0.4.3:11434/"), "qwen2.5:32b");

// v2: Ollama local with UseFunctionInvocation
//IChatClient client = new ChatClientBuilder(new OllamaChatClient(new Uri("http://10.0.4.3:11434"), "qwen2.5:32b"))
//                .UseFunctionInvocation()
//                .Build();

// v3: Ollama local with UseFunctionInvocation parameters
IChatClient client = new ChatClientBuilder(new OllamaApiClient(new Uri("http://10.0.4.3:11434"), "qwen2.5:32b"))
    .UseFunctionInvocation(
        configure: cfg =>
        {
          cfg.MaximumIterationsPerRequest = 10;
          cfg.AllowConcurrentInvocation = false;
        })
    .Build();

/* Create and Configure the MCP client*/
// git
//IMcpClient mcpClient = await McpClientFactory.CreateAsync(
//    new StdioClientTransport(new()
//    {
//      Command = "uvx",
//      Arguments = [
//          "mcp-server-git",
//          "--repository",
//          @"E:\Amplink\DevExpressRefactor\Gitee\Demo\MCPClient\mcp-server-git"
//        ],
//      Name = "Git MCP Server",
//    }));

// filesystem
//IMcpClient mcpClient = await McpClientFactory.CreateAsync(
//    new StdioClientTransport(new()
//    {
//      Command = "npx",
//      Arguments = [
//          "-y",
//        "@modelcontextprotocol/server-filesystem",
//        "D:\\SharedFolder",
//        "E:\\Amplink\\DevExpressRefactor\\Gitee\\Demo\\MCPClient\\mcp-server-git\\src",
//        @"e:\Amplink\DevExpressRefactor\Gitee\Amplink\"
//        ],
//      Name = "MCP Server FileSystem",
//    }));

// everything
//IMcpClient mcpClient = await McpClientFactory.CreateAsync(
//    new StdioClientTransport(new StdioClientTransportOptions()
//    {
//      Command = "npx",
//      Arguments = [
//          "-y",
//        "@modelcontextprotocol/server-everything"
//        ],
//      Name = "MCP Server everything",
//    }));

// brave search
bool isWindows = OperatingSystem.IsWindows();
string command = isWindows ? "cmd.exe" : "/bin/sh";
string[] arguments = isWindows
    ? new[] { "/C", "set BRAVE_API_KEY=xxxxxx && npx -y @modelcontextprotocol/server-brave-search" }
    : new[] { "-c", "BRAVE_API_KEY=xxxxxx npx -y @modelcontextprotocol/server-brave-search" };

IMcpClient mcpClient = await McpClientFactory.CreateAsync(
    new StdioClientTransport(new StdioClientTransportOptions()
    {
      Command = command,
      Arguments = arguments,
      Name = "MCP Server Brave Search",
    }));

// List all available tools from the MCP server.
Console.WriteLine("Available tools:");
IList<McpClientTool> tools = await mcpClient.ListToolsAsync();
foreach (McpClientTool tool in tools)
{
  Console.WriteLine($"{tool}");
}
Console.WriteLine();

// Conversational loop that can utilize the tools via prompts.
List<ChatMessage> messages = [];
while (true)
{
  Console.Write("Prompt: ");
  messages.Add(new(ChatRole.User, Console.ReadLine()));

  List<ChatResponseUpdate> updates = [];
  await foreach (ChatResponseUpdate update in client
      .GetStreamingResponseAsync(messages, new() { Tools = [.. tools] }))
  {
    Console.Write(update);
    updates.Add(update);
  }
  Console.WriteLine();

  messages.AddMessages(updates);
}