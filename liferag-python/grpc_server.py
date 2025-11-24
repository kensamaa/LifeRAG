import grpc
from concurrent import futures
import logging
import sys
import os

# Add protos directory to sys.path to fix import errors in generated code
sys.path.append(os.path.join(os.path.dirname(__file__), "protos"))

from protos import rag_pb2, rag_pb2_grpc
from rag_engine import rag_engine
from document_processor import DocumentProcessor

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

class RAGServicer(rag_pb2_grpc.RAGServiceServicer):
    async def Ingest(self, request, context):
        try:
            logger.info(f"gRPC Ingest: {request.filename}")
            
            text = DocumentProcessor.extract_text(
                request.file_content,
                request.content_type,
                request.filename
            )
            
            metadata = {
                "filename": request.filename,
                "content_type": request.content_type,
                "size": len(request.file_content)
            }
            
            chunks = await rag_engine.ingest_document(
                text=text,
                document_id=request.document_id,
                metadata=metadata
            )
            
            return rag_pb2.IngestResponse(
                document_id=request.document_id,
                filename=request.filename,
                chunks_created=chunks,
                status="success"
            )
        except Exception as e:
            logger.error(f"Ingest error: {e}")
            context.set_code(grpc.StatusCode.INTERNAL)
            context.set_details(str(e))
            return rag_pb2.IngestResponse(status="error")
    
    async def Query(self, request, context):
        try:
            logger.info(f"gRPC Query: {request.query[:100]}")
            
            chat_history = [
                {"role": msg.role, "content": msg.content}
                for msg in request.chat_history
            ]
            
            result = await rag_engine.query(
                query_text=request.query,
                chat_history=chat_history,
                top_k=request.top_k or 5
            )
            
            response = rag_pb2.QueryResponse(
                answer=result["answer"],
                sources=result["sources"]
            )
            
            for ctx in result["contexts"]:
                context_msg = rag_pb2.RetrievedContext(
                    text=ctx["text"],
                    score=ctx["score"]
                )
                for k, v in ctx["metadata"].items():
                    context_msg.metadata[k] = str(v)
                response.contexts.append(context_msg)
            
            yield response
            
        except Exception as e:
            logger.error(f"Query error: {e}")
            context.set_code(grpc.StatusCode.INTERNAL)
            context.set_details(str(e))

async def serve():
    server = grpc.aio.server(futures.ThreadPoolExecutor(max_workers=10))
    rag_pb2_grpc.add_RAGServiceServicer_to_server(RAGServicer(), server)
    server.add_insecure_port('[::]:50051')
    logger.info("gRPC server starting on port 50051")
    await server.start()
    await server.wait_for_termination()

if __name__ == '__main__':
    import asyncio
    asyncio.run(serve())
