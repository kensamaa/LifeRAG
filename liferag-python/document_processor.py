from pypdf import PdfReader
from docx import Document as DocxDocument
from typing import Dict, Any
import io
import logging

logger = logging.getLogger(__name__)

class DocumentProcessor:
    @staticmethod
    def extract_text(file_bytes: bytes, content_type: str, filename: str) -> str:
        logger.info(f"Extracting text from {filename} ({content_type})")
        
        if content_type == "application/pdf" or filename.endswith(".pdf"):
            return DocumentProcessor._extract_pdf(file_bytes)
        elif content_type in ["application/vnd.openxmlformats-officedocument.wordprocessingml.document", "application/msword"] or filename.endswith((".docx", ".doc")):
            return DocumentProcessor._extract_docx(file_bytes)
        elif content_type.startswith("text/") or filename.endswith(".txt"):
            return file_bytes.decode("utf-8", errors="ignore")
        else:
            raise ValueError(f"Unsupported file type: {content_type}")
    
    @staticmethod
    def _extract_pdf(file_bytes: bytes) -> str:
        pdf_file = io.BytesIO(file_bytes)
        reader = PdfReader(pdf_file)
        
        text = ""
        for page in reader.pages:
            text += page.extract_text() + "\n\n"
        
        return text.strip()
    
    @staticmethod
    def _extract_docx(file_bytes: bytes) -> str:
        docx_file = io.BytesIO(file_bytes)
        doc = DocxDocument(docx_file)
        
        text = ""
        for paragraph in doc.paragraphs:
            text += paragraph.text + "\n"
        
        return text.strip()
