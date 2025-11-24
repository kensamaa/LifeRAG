from pydantic_settings import BaseSettings

class Settings(BaseSettings):
    qdrant_host: str = "localhost"
    qdrant_port: int = 6333
    ollama_base_url: str = "http://localhost:11434"
    embedding_model: str = "nomic-embed-text-v1.5"
    llm_model: str = "llama3.1:8b"
    collection_name: str = "liferag_documents"
    chunk_size: int = 512
    chunk_overlap: int = 50
    
    class Config:
        env_file = ".env"
        case_sensitive = False
        extra = 'ignore'

settings = Settings()
