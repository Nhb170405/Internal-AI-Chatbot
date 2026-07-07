from fastapi import APIRouter

from app.chunking.text_chunker import chunk_text
from app.models.chunk_request import ChunkRequest
from app.models.chunk_response import ChunkItemResponse, ChunkResponse


router = APIRouter(tags=["Chunking"])


@router.post("/chunk", response_model=ChunkResponse)
def chunk_document(request: ChunkRequest) -> ChunkResponse:
    # Bai tap Milestone 6:
    # 1. Goi chunk_text(request.text, request.chunk_size, request.chunk_overlap).
    # 2. Neu chunk_text raise ValueError:
    #    - return success=false.
    #    - errorMessage la message an toan.
    # 3. Neu thanh cong:
    #    - map moi TextChunk sang ChunkItemResponse.
    #    - chunkCount = len(chunks).
    #    - metadata nen co strategy, chunkSize, chunkOverlap.
    # 4. Khong log full text vi co the chua du lieu noi bo.
    try:
        chunks = chunk_text(request.text, request.chunk_size, request.chunk_overlap)
    except ValueError as error:
        return ChunkResponse(
            documentId=request.document_id,
            success=False,
            chunkCount=0,
            chunks=[],
            metadata={
                "errorType": "validation_error"
            },
            warnings=[],
            errorMessage=str(error)
        )

    return ChunkResponse(
        documentId=request.document_id,
        success=True,
        chunkCount=len(chunks),
        chunks=[
            ChunkItemResponse(
                chunkIndex=chunk.chunk_index,
                content=chunk.content,
                characterCount=len(chunk.content),
                startOffset=chunk.start_offset,
                endOffset=chunk.end_offset,
                metadata=chunk.metadata,
            )
            for chunk in chunks
        ],
        metadata={
            "strategy": "paragraph_character",
            "chunkSize": request.chunk_size,
            "chunkOverlap": request.chunk_overlap,
        },
        warnings=[],
        errorMessage=None,
    )