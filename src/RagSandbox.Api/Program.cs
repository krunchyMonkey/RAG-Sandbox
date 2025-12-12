using RagSandbox.Application.Chat;
using RagSandbox.Application.WebContent;
using RagSandbox.Infrastructure.LLM;
using RagSandbox.Infrastructure.WebScraping;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
// builder.Services.AddOpenApi();

// Configure HttpClients
builder.Services.AddHttpClient<IWebScrapingService, WebScrapingService>(client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
});
builder.Services.AddHttpClient<ILlmClient, OllamaClient>(client =>
{
    var ollamaUrl = builder.Configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";
    client.BaseAddress = new Uri(ollamaUrl);
    client.Timeout = TimeSpan.FromMinutes(10); // Allow up to 10 minutes for large models
});

// Register application services
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddSingleton<IMessageParser, MessageParser>();

var app = builder.Build();

// Configure the HTTP request pipeline
// if (app.Environment.IsDevelopment())
// {
//     app.MapOpenApi();
// }

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
