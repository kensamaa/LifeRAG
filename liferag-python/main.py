from fastapi import FastAPI, UploadFile, File, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from models import IngestRequest, IngestResponse, QueryRequest, QueryResponse
from rag_engine import rag_engine
from document_processor import DocumentProcessor
import logging

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

app = FastAPI(
    title="LifeRAG Python Microservice",
    description="RAG service for document ingestion and querying",
    version="1.0.0"
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

@app.get("/")
async def root():
    return {
        "service": "LifeRAG Python Microservice",
        "status": "running",
        "version": "1.0.0"
    }

@app.get("/health")
async def health():
    return {"status": "healthy"}

@app.post("/ingest", response_model=IngestResponse)
async def ingest_document(
    file: UploadFile = File(...),
    document_id: str = None,
):
    try:
        logger.info(f"Ingesting file: {file.filename}")
        
        file_bytes = await file.read()
        
        text = DocumentProcessor.extract_text(
            file_bytes,
            file.content_type,
            file.filename
        )
        
        if not text.strip():
            raise HTTPException(status_code=400, detail="No text could be extracted from the document")
        
        doc_id = document_id or file.filename
        
        metadata = {
            "filename": file.filename,
            "content_type": file.content_type,
            "size": len(file_bytes)
        }
        
        chunks_created = await rag_engine.ingest_document(
            text=text,
            document_id=doc_id,
            metadata=metadata
        )
        
        return IngestResponse(
            document_id=doc_id,
            filename=file.filename,
            chunks_created=chunks_created,
            status="success"
        )
    
    except Exception as e:
        logger.error(f"Error ingesting document: {str(e)}")
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/query", response_model=QueryResponse)
async def query_documents(request: QueryRequest):
    try:
        logger.info(f"Query received: {request.query[:100]}")
        
        chat_history = [
            {"role": msg.role, "content": msg.content}
            for msg in request.chat_history
        ] if request.chat_history else []
        
        result = await rag_engine.query(
            query_text=request.query,
            chat_history=chat_history,
            top_k=request.top_k
        )
        
        return QueryResponse(**result)
    
    except Exception as e:
        logger.error(f"Error processing query: {str(e)}")
        raise HTTPException(status_code=500, detail=str(e))

@app.delete("/documents/{document_id}")
async def delete_document(document_id: str):
    try:
        await rag_engine.delete_document(document_id)
        return {"status": "success", "message": f"Document {document_id} deleted"}
    except Exception as e:
        logger.error(f"Error deleting document: {str(e)}")
        raise HTTPException(status_code=500, detail=str(e))

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)
