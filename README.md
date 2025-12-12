# 🤖 RAG Sandbox

> **R**etrieval **A**ugmented **G**eneration playground - Chat with AI about any webpage!

<div align="center">

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp)
![Ollama](https://img.shields.io/badge/Ollama-000000?style=for-the-badge&logo=ollama)

**A powerful API that lets you chat with AI about web content using local LLMs**

[Features](#-features) • [Quick Start](#-quick-start) • [API Reference](#-api-reference) • [Examples](#-examples)

</div>

---

## ✨ Features

| Feature | Description |
|---------|-------------|
| 🌐 **Web Scraping** | Automatically fetch and parse content from any URL |
| 🧠 **Multiple LLMs** | Support for various Ollama models (llama3, qwen, deepseek, etc.) |
| ⚡ **Streaming Responses** | Get real-time token-by-token responses |
| 💬 **Session Management** | Maintain conversation context across requests |
| 🎯 **Smart URL Parsing** | Automatically detect and extract URLs from messages |
| 🔄 **RAG Pipeline** | Retrieve webpage content and augment LLM responses |

---

## 🚀 Quick Start

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Ollama](https://ollama.ai/) installed and running
- At least one Ollama model pulled (e.g., `ollama pull llama3.2`)

### Installation

```bash
# Clone the repository
git clone <your-repo-url>
cd rag-sandbox

# Restore dependencies
dotnet restore rag-sandbox.sln

# Run the service
dotnet run --project src/RagSandbox/rag-sandbox.csproj
```

The service will start on **http://localhost:5247** 🎉

---

## 📡 API Reference

### 🔍 Get Available Models

```http
GET /api/chat/models
```

**Response:**
```json
[
  {
    "name": "llama3.2:latest",
    "id": "a80c4f17...",
    "size": "1.9 GB",
    "modified": "12/04/2025 23:56"
  }
]
```

---

### 💬 Chat (Standard)

```http
POST /api/chat
Content-Type: application/json
```

**Request Body:**
```json
{
  "message": "Based on this article https://example.com, tell me about...",
  "model": "qwen2.5:32b",
  "sessionId": "optional-session-id",
  "webUrl": "optional-direct-url"
}
```

**Response:**
```json
{
  "message": "AI response here...",
  "sessionId": "generated-session-id",
  "webUrl": "https://example.com",
  "model": "qwen2.5:32b"
}
```

---

### ⚡ Chat (Streaming)

```http
POST /api/chat/stream
Content-Type: application/json
```

**Request Body:**
```json
{
  "message": "Tell me a story about coding",
  "model": "llama3.2:latest"
}
```

**Response (Server-Sent Events):**
```
data: Once
data:  upon
data:  a
data:  time
data: ...
data: [DONE]
```

---

### 🗂️ Get Session

```http
GET /api/chat/session/{sessionId}
```

**Response:**
```json
{
  "id": "session-id",
  "messages": [...],
  "webContentUrl": "https://example.com"
}
```

---

## 💡 Examples

### Example 1: Simple Question

```bash
curl -X POST http://localhost:5247/api/chat \
  -H "Content-Type: application/json" \
  -d '{
    "message": "What is artificial intelligence?"
  }'
```

---

### Example 2: Chat About a Webpage

```bash
curl -X POST http://localhost:5247/api/chat \
  -H "Content-Type: application/json" \
  -d '{
    "message": "Summarize this article: https://en.wikipedia.org/wiki/Machine_learning"
  }'
```

---

### Example 3: Streaming Response

```bash
curl -N -X POST http://localhost:5247/api/chat/stream \
  -H "Content-Type: application/json" \
  -d '{
    "message": "Write a haiku about programming",
    "model": "qwen2.5:32b"
  }'
```

---

### Example 4: Continue Conversation

```bash
curl -X POST http://localhost:5247/api/chat \
  -H "Content-Type: application/json" \
  -d '{
    "message": "Tell me more about that",
    "sessionId": "previous-session-id"
  }'
```

---

## ⚙️ Configuration

### appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "DefaultModel": "llama3.2:latest"
  }
}
```

### Available Models

The service supports any model available in your local Ollama installation:

| Model | Size | Speed | Quality |
|-------|------|-------|---------|
| 🦙 llama3.2:latest | 1.9 GB | ⚡⚡⚡ | ⭐⭐⭐ |
| 🦙 llama3.1:latest | 4.6 GB | ⚡⚡ | ⭐⭐⭐⭐ |
| 🧠 deepseek-r1:14b | 8.4 GB | ⚡ | ⭐⭐⭐⭐ |
| 🎯 qwen2.5:32b | 18.5 GB | ⚡ | ⭐⭐⭐⭐⭐ |

---

## 🏗️ Architecture

```
┌─────────────────┐
│   HTTP Client   │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  ChatController │
└────────┬────────┘
         │
         ▼
┌─────────────────┐      ┌──────────────────┐
│   ChatService   │─────▶│ MessageParser    │
└────────┬────────┘      └──────────────────┘
         │
         ├──────────────────┬──────────────────┐
         ▼                  ▼                  ▼
┌─────────────────┐ ┌──────────────┐ ┌──────────────┐
│ WebScrapingService│ │ OllamaClient │ │SessionManager│
└─────────────────┘ └──────────────┘ └──────────────┘
```

---

## 🎨 Features in Detail

### 🌐 Automatic URL Detection

The service automatically detects URLs in your messages:

```json
{
  "message": "Tell me about https://example.com, what does it say?"
}
```

The URL is extracted, scraped, and provided as context to the LLM.

---

### 💾 Session Persistence

Conversations are maintained across requests using session IDs:

1. First request creates a new session
2. Response includes `sessionId`
3. Include `sessionId` in subsequent requests to continue the conversation

---

### ⚡ Streaming for Better UX

Streaming responses provide a better user experience, especially with large models:

- See responses as they're generated
- No timeout waiting for complete responses
- Perfect for chatbot interfaces

---

## 🔧 Development

### Project Structure

**This project now follows a canonical folder layout for better organization:**

```
rag-sandbox/
├── README.md                 # Project documentation
├── .gitignore                # Git ignore rules
├── rag-sandbox.sln           # Visual Studio solution file
├── src/                      # Source code
│   └── RagSandbox/          # Main application project
│       ├── Api/
│       │   └── Controllers/  # API endpoints
│       ├── Application/
│       │   ├── Chat/        # Chat service logic
│       │   └── WebContent/  # Web scraping
│       ├── Domain/
│       │   ├── Chat/        # Domain models
│       │   └── WebContent/
│       ├── Infrastructure/
│       │   ├── LLM/         # Ollama integration
│       │   └── WebScraping/ # HTML parsing
│       ├── Program.cs       # App entry point
│       ├── rag-sandbox.csproj
│       ├── appsettings.json
│       └── appsettings.Development.json
├── tests/                    # Test projects
│   └── SmokeTests/          # Integration smoke tests
│       ├── IntegrationSmokeTests.cs
│       └── SmokeTests.csproj
├── postman/                  # API testing artifacts
│   ├── rag-sandbox.http     # HTTP request examples
│   └── test-request.ps1     # PowerShell test script
└── ops/                      # Operational scripts (future use)
```

### Building and Running After Restructure

The restructure preserves all functionality. To build and run:

```bash
# Restore dependencies
dotnet restore rag-sandbox.sln

# Build the solution
dotnet build rag-sandbox.sln --configuration Release

# Run the application
dotnet run --project src/RagSandbox/rag-sandbox.csproj

# Run tests (requires service running on localhost:8080)
dotnet test rag-sandbox.sln
```

### Run Tests

```bash
# Run all tests (requires service running on localhost:8080)
dotnet test rag-sandbox.sln

# Run with coverage
dotnet test rag-sandbox.sln --collect:"XPlat Code Coverage"
```

---

## 🐛 Troubleshooting

### Service won't start

**Problem:** Port 5247 already in use

**Solution:**
```bash
# Kill existing process
pkill -f "dotnet run"

# Or change port in launchSettings.json
```

---

### Ollama connection refused

**Problem:** Cannot connect to Ollama

**Solution:**
```bash
# Make sure Ollama is running
ollama serve

# Or check your Ollama BaseUrl in appsettings.json
```

---

### Slow responses with large models

**Problem:** qwen2.5:32b takes too long

**Solution:**
- Use streaming endpoint for better UX
- Or switch to a smaller model like llama3.2
- Timeout is set to 10 minutes (configurable in Program.cs)

---

## 🤝 Contributing

Contributions are welcome! Feel free to:

- 🐛 Report bugs
- 💡 Suggest features
- 🔧 Submit pull requests

---

## 📝 License

This project is open source and available under the MIT License.

---

## 🎯 Roadmap

- [ ] Add support for multiple URLs in a single message
- [ ] Implement document upload (PDF, DOCX)
- [ ] Add vector database for long-term memory
- [ ] Web UI for easier testing
- [ ] Docker support
- [ ] Add authentication/API keys

---

## 🙏 Acknowledgments

- **Ollama** - For making local LLMs accessible
- **HtmlAgilityPack** - For robust HTML parsing
- **.NET Team** - For an awesome framework

---

<div align="center">

**Made with ❤️ and ☕**

⭐ Star this repo if you find it useful!

</div>
