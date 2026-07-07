from fastapi import APIRouter

from app.config.settings import get_settings
from app.embedding.embedding_service import EmbeddingService
from app.models.vector_index_request import VectorIndexRequest
from app.models.vector_index_response import VectorIndexResponse
from app.models.vector_search_request import VectorSearchRequest
from app.models.vector_search_response import VectorSearchHit, VectorSearchResponse
from app.vector.qdrant_service import QdrantService
from app.vector.vector_models import VectorPointInput


router = APIRouter(tags=["Vector"])


@router.post("/index-document", response_model=VectorIndexResponse)
def index_document(request: VectorIndexRequest) -> VectorIndexResponse:
    # Bai tap Milestone 7:
    # 1. Doc settings = get_settings().
    # 2. Tao EmbeddingService va QdrantService.
    # 3. Validate request.chunks khong rong.
    # 4. Lay danh sach content tu chunks.
    # 5. Goi embedding_service.embed_texts(contents).
    # 6. Lay vector_size = len(embeddings[0]).
    # 7. Goi qdrant_service.ensure_collection(vector_size).
    # 8. Tao points:
    #    - point_id = chunk.chunk_id.
    #    - vector = embedding tuong ung.
    #    - payload gom documentId, chunkId, chunkIndex, content, accessLevel, originalFileName, documentStatus.
    # 9. Goi qdrant_service.upsert_points(points).
    # 10. Return success=true.
    # 11. Neu loi co kiem soat thi return success=false, khong raise stack trace ra client.
    
    collection_name = "internal_documents"

    try:
        settings = get_settings()
        embedding_service = EmbeddingService(settings)
        qdrant_service = QdrantService(settings)
        
        if not request.chunks : 
            return VectorIndexResponse(
                documentId=request.document_id,
                success=False,
                collectionName=settings.qdrant_collection,
                indexedCount=0,
                errorMessage="Chunks are empty.",
            )
        
        contents = []
        
        for chunk in request.chunks :
            content = chunk.content.strip()
            if not content:
                return VectorIndexResponse(
                    documentId=request.document_id,
                    success=False,
                    collectionName=settings.qdrant_collection,
                    indexedCount=0,
                    errorMessage="Chunk content is empty.",
                )

            contents.append(content)
        
        embeddings = embedding_service.embed_texts(contents)
        if not embeddings:
            return VectorIndexResponse(
                documentId=request.document_id,
                success=False,
                collectionName=settings.qdrant_collection,
                indexedCount=0,
                errorMessage="Embedding result is empty.",
            )
            
        vector_size = len(embeddings[0])
        qdrant_service.ensure_collection(vector_size)
        
        points = []
        for chunk,vector in zip(request.chunks, embeddings):
            points.append(VectorPointInput(
                point_id=chunk.chunk_id,
                vector=vector,
                payload={
                    "documentId": request.document_id,
                    "chunkId": chunk.chunk_id,
                    "chunkIndex": chunk.chunk_index,
                    "content": chunk.content,
                    "accessLevel": request.access_level,
                    "originalFileName": request.original_file_name,
                    "documentStatus": request.document_status,
                    "metadata": chunk.metadata,
                },
            ))
        qdrant_service.upsert_points(points)
        
        return VectorIndexResponse(
            documentId=request.document_id,
            success=True,
            collectionName=settings.qdrant_collection,
            indexedCount=len(points),
            errorMessage=None,
        )
        
        
    except ValueError as error:
        return VectorIndexResponse(
            documentId=request.document_id,
            success=False,
            collectionName=collection_name,
            indexedCount=0,
            errorMessage=str(error),
        )

    except Exception:
        return VectorIndexResponse(
            documentId=request.document_id,
            success=False,
            collectionName=collection_name,
            indexedCount=0,
            errorMessage="Vector indexing failed.",
        )
        
    

@router.post("/search", response_model=VectorSearchResponse)
def search_documents(request: VectorSearchRequest) -> VectorSearchResponse:
    # Bai tap Milestone 7:
    # 1. Validate request.query khong rong.
    # 2. Validate allowed_access_levels khong rong.
    # 3. Doc settings, tao EmbeddingService va QdrantService.
    # 4. Embed query bang embed_text.
    # 5. Goi qdrant_service.search(query_vector, top_k, allowed_access_levels).
    # 6. Map hits thanh VectorSearchHit.
    # 7. Return success=true.

    try:
        query = request.query.strip()
        if not query:
            return VectorSearchResponse(
                success=False,
                hits=[],
                errorMessage="Query is empty.",
            )

        if not request.allowed_access_levels:
            return VectorSearchResponse(
                success=False,
                hits=[],
                errorMessage="Allowed access levels are empty.",
            )

        settings = get_settings()
        embedding_service = EmbeddingService(settings)
        qdrant_service = QdrantService(settings)

        query_vector = embedding_service.embed_text(query)

        results = qdrant_service.search(
            query_vector=query_vector,
            top_k=request.top_k,
            allowed_access_levels=request.allowed_access_levels,
        )

        hits = []

        for result in results:
            payload = result.payload

            hits.append(VectorSearchHit(
                score=result.score,
                documentId=payload.get("documentId", ""),
                chunkId=payload.get("chunkId", ""),
                chunkIndex=payload.get("chunkIndex", 0),
                content=payload.get("content", ""),
                payload=payload,
            ))

        return VectorSearchResponse(
            success=True,
            hits=hits,
            errorMessage=None,
        )

    except ValueError as error:
        return VectorSearchResponse(
            success=False,
            hits=[],
            errorMessage=str(error),
        )

    except Exception:
        return VectorSearchResponse(
            success=False,
            hits=[],
            errorMessage="Vector search failed.",
        )