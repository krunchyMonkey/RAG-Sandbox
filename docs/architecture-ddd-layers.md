# Domain-Driven Design Layer Architecture

This document explains how the RAG-Sandbox project is organized using Domain-Driven Design (DDD) principles and the responsibilities of each layer.

## Layer Overview

The RAG-Sandbox project follows a clean architecture with three main layers:

```
src/
├── RagSandbox.Domain/          # Core business logic and entities
├── RagSandbox.Application/     # Use cases and interfaces
└── RagSandbox.Infrastructure/  # External service implementations
```

## Layer Responsibilities

### Domain Layer
**Purpose**: Contains the core business logic and domain entities

**What belongs here:**
- Domain entities (e.g., `WebPage`, `ChatSession`, `Document`, `Chunk`)
- Value objects (e.g., `ChunkMetadata`, `DocumentId`)
- Domain events
- Business rules and invariants
- Enums representing domain concepts (e.g., `ChunkingStrategy`)

**What does NOT belong here:**
- Framework dependencies (ASP.NET, Entity Framework, etc.)
- External service calls (HTTP, databases, file systems)
- Application-specific logic

**Example:**
```csharp
// Domain/DocumentProcessing/Chunk.cs
namespace RagSandbox.Domain.DocumentProcessing;

public class Chunk
{
    public string Id { get; init; }
    public string Content { get; init; }
    public int StartPosition { get; init; }
    public int EndPosition { get; init; }
    public ChunkMetadata Metadata { get; init; }
}
```

### Application Layer
**Purpose**: Defines use cases and orchestrates domain logic

**What belongs here:**
- **Interfaces** that define what operations are needed (contracts)
- **Service orchestration** that coordinates multiple operations
- Application-specific business logic
- DTOs (Data Transfer Objects) for API contracts
- Configuration objects (e.g., `ChunkingOptions`)

**What does NOT belong here:**
- Technical implementation details
- Framework-specific code
- External service calls (the actual HTTP requests, database queries, etc.)

**Example:**
```csharp
// Application/DocumentProcessing/IChunkingService.cs
namespace RagSandbox.Application.DocumentProcessing;

public interface IChunkingService
{
    Task<List<Domain.DocumentProcessing.Chunk>> ChunkTextAsync(
        string text,
        ChunkingOptions options,
        CancellationToken cancellationToken = default);
}

// Application/Chat/ChatService.cs
public class ChatService : IChatService
{
    private readonly ILlmClient _llmClient;
    private readonly IMessageParser _messageParser;

    // Orchestrates domain logic and calls infrastructure services
    public async Task<ChatResponse> SendMessageAsync(string message)
    {
        var parsedMessage = _messageParser.Parse(message);
        var response = await _llmClient.GenerateResponseAsync(parsedMessage);
        return new ChatResponse { Message = response };
    }
}
```

### Infrastructure Layer
**Purpose**: Provides concrete implementations of interfaces using specific technologies

**What belongs here:**
- **Concrete implementations** of Application layer interfaces
- External service adapters (HTTP clients, database contexts)
- Framework-specific code
- Third-party library integrations (e.g., HtmlAgilityPack)
- File system operations
- Caching implementations

**What does NOT belong here:**
- Business logic
- Domain entities
- Interface definitions (those belong in Application)

**Example:**
```csharp
// Infrastructure/DocumentProcessing/TextChunkingService.cs
namespace RagSandbox.Infrastructure.DocumentProcessing;

public class TextChunkingService : IChunkingService
{
    private readonly ILogger<TextChunkingService> _logger;

    public async Task<List<Chunk>> ChunkTextAsync(
        string text,
        ChunkingOptions options,
        CancellationToken cancellationToken = default)
    {
        // Technical implementation using specific algorithm
        // Maybe uses a specific library or custom logic
        _logger.LogInformation("Chunking text with {Strategy}", options.Strategy);

        // Implementation details...
    }
}
```

## Interface/Implementation Pattern

The project consistently follows the **Dependency Inversion Principle**:

| Interface (Application) | Implementation (Infrastructure) | Purpose |
|------------------------|----------------------------------|---------|
| `ILlmClient` | `OllamaClient` | LLM communication |
| `IWebScrapingService` | `WebScrapingService` | Web scraping |
| `IChunkingService` | `TextChunkingService` | Document chunking |
| `IEmbeddingService` | `OpenAIEmbeddingService` | Text embeddings |
| `IVectorStore` | `ChromaDbVectorStore` | Vector database |

## Why This Separation Matters

### 1. **Testability**
You can easily mock interfaces in Application layer for unit testing without needing real HTTP calls, databases, or external services.

### 2. **Flexibility**
You can swap implementations without changing business logic:
- Switch from Ollama to OpenAI by implementing a new `ILlmClient`
- Change from ChromaDB to Pinecone by implementing a new `IVectorStore`
- Replace web scraping with a different library without touching the interface

### 3. **Dependency Direction**
Dependencies flow inward:
```
Infrastructure → Application → Domain
```
- Infrastructure depends on Application (implements interfaces)
- Application depends on Domain (uses entities)
- Domain depends on nothing (pure business logic)

### 4. **Clear Boundaries**
Each layer has a clear responsibility, making it easier to:
- Understand where to add new code
- Find existing functionality
- Maintain and refactor code

## Real Examples from RAG-Sandbox

### Example 1: Web Scraping

**Domain Layer:**
```csharp
// Domain/WebContent/WebPage.cs
public class WebPage
{
    public string Url { get; init; }
    public string Title { get; init; }
    public string Content { get; init; }
}
```

**Application Layer:**
```csharp
// Application/WebContent/IWebScrapingService.cs
public interface IWebScrapingService
{
    Task<Domain.WebContent.WebPage> ScrapeUrlAsync(
        string url,
        CancellationToken cancellationToken = default);
}
```

**Infrastructure Layer:**
```csharp
// Infrastructure/WebScraping/WebScrapingService.cs
public class WebScrapingService : IWebScrapingService
{
    private readonly HttpClient _httpClient;

    // Uses HtmlAgilityPack to implement scraping
    public async Task<WebPage> ScrapeUrlAsync(string url, CancellationToken ct)
    {
        var html = await _httpClient.GetStringAsync(url, ct);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        // ... parsing logic
    }
}
```

### Example 2: LLM Client

**Application Layer:**
```csharp
// Application/Chat/ILlmClient.cs
public interface ILlmClient
{
    Task<string> GenerateResponseAsync(
        List<LlmMessage> messages,
        string? model = null,
        CancellationToken cancellationToken = default);
}
```

**Infrastructure Layer:**
```csharp
// Infrastructure/LLM/OllamaClient.cs
public class OllamaClient : ILlmClient
{
    private readonly HttpClient _httpClient;

    // Ollama-specific implementation
    public async Task<string> GenerateResponseAsync(
        List<LlmMessage> messages,
        string? model = null,
        CancellationToken ct = default)
    {
        var request = new { model, prompt = BuildPrompt(messages) };
        var response = await _httpClient.PostAsJsonAsync("/api/generate", request, ct);
        // ... Ollama-specific response parsing
    }
}
```

## Decision Guide: Where Does My Code Belong?

### Ask yourself these questions:

1. **Is it a core business concept?** → Domain Layer
   - Examples: Chunk, Document, ChatSession, WebPage

2. **Is it defining what operations are needed?** → Application Layer (Interface)
   - Examples: IChunkingService, ILlmClient, IVectorStore

3. **Is it orchestrating multiple operations?** → Application Layer (Service)
   - Examples: ChatService, DocumentProcessingService

4. **Is it implementing how something is done with a specific technology?** → Infrastructure Layer
   - Examples: OllamaClient, WebScrapingService, ChromaDbVectorStore

5. **Does it depend on external libraries or frameworks?** → Infrastructure Layer
   - Examples: HtmlAgilityPack, HttpClient, database contexts

## Common Patterns for RAG Features

### Document Chunking
```
Domain/DocumentProcessing/
├── Chunk.cs                    // What a chunk is
├── ChunkMetadata.cs            // Chunk properties
└── ChunkingStrategy.cs         // Enum/value object

Application/DocumentProcessing/
├── IChunkingService.cs         // What chunking does
├── ChunkingOptions.cs          // Configuration
└── DocumentProcessingService.cs // Orchestration

Infrastructure/DocumentProcessing/
├── TextChunkingService.cs      // How: fixed-size chunking
├── SemanticChunkingService.cs  // How: LLM-based chunking
└── SentenceChunkingService.cs  // How: sentence-aware chunking
```

### Vector Embeddings
```
Domain/Embeddings/
└── Embedding.cs                // What an embedding is

Application/Embeddings/
├── IEmbeddingService.cs        // What embedding does
└── IVectorStore.cs             // What vector storage does

Infrastructure/Embeddings/
├── OpenAIEmbeddingService.cs   // How: OpenAI API
├── OllamaEmbeddingService.cs   // How: Ollama local
├── ChromaDbVectorStore.cs      // How: ChromaDB
└── PineconeVectorStore.cs      // How: Pinecone
```

## Summary

The RAG-Sandbox project uses DDD layers to maintain clean separation of concerns:

- **Domain**: Pure business concepts (Chunk, Document, WebPage)
- **Application**: Use cases and contracts (interfaces, orchestration services)
- **Infrastructure**: Technical implementations (HTTP clients, specific algorithms, databases)

This architecture makes the codebase:
- Easy to understand and navigate
- Simple to test (mock interfaces)
- Flexible to change (swap implementations)
- Maintainable over time (clear boundaries)

When adding new features, follow the existing patterns and ask: "Is this a business concept, a use case definition, or a technical implementation?" The answer tells you which layer it belongs in.
