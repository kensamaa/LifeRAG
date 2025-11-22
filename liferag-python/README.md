# LifeRAG Python Microservice

High-performance RAG service using LlamaIndex, Qdrant, and Ollama.

## Architecture

- **Embeddings**: nomic-embed-text-v1.5 (768-dim, open-source)
- **Vector Store**: Qdrant (cosine similarity)
- **LLM**: Llama-3.1-8B via Ollama (local)
- **Framework**: FastAPI + LlamaIndex

## Setup

### Windows
```powershell
.\install.ps1
```

### Linux/Mac
```bash
chmod +x install.sh
./install.sh
```

### Manual Setup
```bash
python -m venv venv
source venv/bin/activate  # Windows: .\venv\Scripts\Activate.ps1
pip install -r requirements.txt

# Start all services (PostgreSQL + Qdrant) from root
cd ..
docker-compose up -d
cd liferag-python

# Install Ollama and pull model
ollama pull llama3.1:8b
```

## Run

```bash
uvicorn main:app --reload --port 8000
```

API: http://localhost:8000
Docs: http://localhost:8000/docs

## Endpoints

### POST /ingest
Upload and index documents (PDF, DOCX, TXT)

### POST /query
Query with chat history, returns answer + sources

### DELETE /documents/{id}
Remove document from vector store
