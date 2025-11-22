from pydantic import BaseModel
from typing import List, Optional

class IngestRequest(BaseModel):
    document_id: str
    filename: str
    content_type: str

class IngestResponse(BaseModel):
    document_id: str
    filename: str
    chunks_created: int
    status: str

class ChatMessage(BaseModel):
    role: str
    content: str

class QueryRequest(BaseModel):
    query: str
    chat_history: Optional[List[ChatMessage]] = []
    top_k: int = 5

class RetrievedContext(BaseModel):
    text: str
    score: float
    metadata: dict

class QueryResponse(BaseModel):
    answer: str
    contexts: List[RetrievedContext]
    sources: List[str]
