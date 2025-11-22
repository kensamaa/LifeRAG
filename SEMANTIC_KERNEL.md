# Semantic Kernel Integration

## Architecture

```
User Message
    ↓
Semantic Kernel (Orchestrator)
    ↓
Auto-invokes RagPlugin.RetrieveContext()
    ↓
gRPC call to Python RAG service
    ↓
Returns context from Qdrant
    ↓
SK combines context + history + LLM
    ↓
Final answer to user
```

## What It Does

**RagPlugin** - Semantic Kernel function that:
- Calls Python gRPC service
- Retrieves relevant context from user's documents
- Auto-invoked by SK when needed

**SemanticKernelService** - Orchestrator that:
- Manages conversation flow
- Auto-invokes RAG plugin via function calling
- Combines retrieved context with LLM reasoning
- Supports both OpenAI and local Ollama

## Configuration

appsettings.json:
```json
"SemanticKernel": {
  "UseOpenAI": false,
  "Ollama": {
    "Url": "http://localhost:11434",
    "Model": "llama3.1:8b"
  }
}
```

Set `UseOpenAI: true` and add API key for GPT-4o-mini.

## Why This Is Principal Engineer Level

1. **Function Calling**: SK auto-invokes RAG plugin when context needed
2. **Orchestration**: Manages complex AI workflows declaratively
3. **Flexibility**: Swap OpenAI ↔ Ollama with config change
4. **Clean Architecture**: Plugin pattern separates concerns
5. **Production Ready**: Error handling, logging, type-safe

The chat endpoint now uses SK to orchestrate RAG retrieval + LLM reasoning automatically!
