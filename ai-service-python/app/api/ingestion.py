from fastapi import APIRouter, status
from fastapi.responses import JSONResponse

from app.ingestion.ingestion_service import ingest_document
from app.models.ingest_request import IngestRequest
from app.models.ingest_response import IngestResponse

router = APIRouter(tags=["Ingestion"])


@router.post("/ingest", response_model=IngestResponse)
def ingest(request: IngestRequest) -> IngestResponse | JSONResponse:
    # Muc tieu:
    # 1. Nhan request tu ASP.NET Core gom documentId, filePath, fileName, extension.
    # 2. Goi ingestion_service de chon parser phu hop.
    # 3. Neu parser thanh cong thi tra success=true va extractedText.
    # 4. Neu parser loi thi tra success=false, khong de service crash.
    #
    # Goi y flow:
    # - validate request.file_path ton tai.
    # - goi ingest_document(request).
    # - return IngestResponse.
    #
    # Hien tai de skeleton tra 501 de ban tu implement tung buoc.
    try:
        return ingest_document(request)
    except NotImplementedError as error:
        return JSONResponse(
            status_code=status.HTTP_501_NOT_IMPLEMENTED,
            content={
                "documentId": request.document_id,
                "success": False,
                "parserName": "not_implemented",
                "extractedText": "",
                "characterCount": 0,
                "pageCount": None,
                "metadata": {},
                "warnings": [],
                "errorMessage": str(error),
            },
        )
