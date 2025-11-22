from llama_index.core import VectorStoreIndex, Settings, Document
from llama_index.core.node_parser import SentenceSplitter
from llama_index.embeddings.huggingface import HuggingFaceEmbedding
from llama_index.llms.ollama import Ollama
from llama_index.vector_stores.qdrant import QdrantVectorStore
from qdrant_client import QdrantClient
from qdrant_client.models import Distance, VectorParams
from typing import List, Dict, Any
import logging
from config import settings

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

class RAGEngine:
    def __init__(self):
        logger.info("Initializing RAG Engine...")
        
        self.embed_model = HuggingFaceEmbedding(
            model_name="nomic-ai/nomic-embed-text-v1.5",
            trust_remote_code=True
        )
        
        self.llm = Ollama(
            model=settings.llm_model,
            base_url=settings.ollama_base_url,
            request_timeout=120.0,
            temperature=0.7,
        )
        
        Settings.embed_model = self.embed_model
        Settings.llm = self.llm
        Settings.chunk_size = settings.chunk_size
        Settings.chunk_overlap = settings.chunk_overlap
        
        self.client = QdrantClient(
            host=settings.qdrant_host,
            port=settings.qdrant_port
        )
        
        self._ensure_collection()
        
        self.vector_store = QdrantVectorStore(
            client=self.client,
            collection_name=settings.collection_name
        )
        
        self.index = VectorStoreIndex.from_vector_store(
            vector_store=self.vector_store
        )
        
        logger.info("RAG Engine initialized successfully")
    
    def _ensure_collection(self):
        collections = self.client.get_collections().collections
        collection_names = [c.name for c in collections]
        
        if settings.collection_name not in collection_names:
            logger.info(f"Creating collection: {settings.collection_name}")
            self.client.create_collection(
                collection_name=settings.collection_name,
                vectors_config=VectorParams(
                    size=768,
                    distance=Distance.COSINE
                )
            )
    
    async def ingest_document(
        self,
        text: str,
        document_id: str,
        metadata: Dict[str, Any]
    ) -> int:
        logger.info(f"Ingesting document: {document_id}")
        
        doc = Document(
            text=text,
            metadata={
                **metadata,
                "document_id": document_id
            }
        )
        
        splitter = SentenceSplitter(
            chunk_size=settings.chunk_size,
            chunk_overlap=settings.chunk_overlap
        )
        nodes = splitter.get_nodes_from_documents([doc])
        
        self.index.insert_nodes(nodes)
        
        logger.info(f"Created {len(nodes)} chunks for document {document_id}")
        return len(nodes)
    
    async def query(
        self,
        query_text: str,
        chat_history: List[Dict[str, str]] = None,
        top_k: int = 5
    ) -> Dict[str, Any]:
        logger.info(f"Processing query: {query_text[:100]}...")
        
        retriever = self.index.as_retriever(similarity_top_k=top_k)
        nodes = retriever.retrieve(query_text)
        
        contexts = []
        sources = set()
        context_text = ""
        
        for node in nodes:
            contexts.append({
                "text": node.text,
                "score": node.score,
                "metadata": node.metadata
            })
            sources.add(node.metadata.get("filename", "Unknown"))
            context_text += f"\n\n{node.text}"
        
        history_context = ""
        if chat_history:
            history_context = "\n".join([
                f"{msg['role'].upper()}: {msg['content']}"
                for msg in chat_history[-3:]
            ])
        
        prompt = f"""You are a helpful AI assistant. Use the following context to answer the user's question.
If the context doesn't contain relevant information, say so honestly.

CONTEXT:
{context_text}

{f'CHAT HISTORY:{history_context}' if history_context else ''}

USER QUESTION: {query_text}

ANSWER:"""
        
        response = self.llm.complete(prompt)
        
        return {
            "answer": response.text,
            "contexts": contexts,
            "sources": list(sources)
        }
    
    async def delete_document(self, document_id: str):
        logger.info(f"Deleting document: {document_id}")
        
        self.client.delete(
            collection_name=settings.collection_name,
            points_selector={
                "filter": {
                    "must": [
                        {
                            "key": "document_id",
                            "match": {"value": document_id}
                        }
                    ]
                }
            }
        )

rag_engine = RAGEngine()
