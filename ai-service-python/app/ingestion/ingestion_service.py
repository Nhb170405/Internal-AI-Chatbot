from pathlib import Path

from app.ingestion.parser_registry import parse_by_extension
from app.models.ingest_request import IngestRequest
from app.models.ingest_response import IngestResponse

from app.ingestion.file_reference_resolver import (cleanup_resolved_file,resolve_file_reference)

def ingest_document(request: IngestRequest) -> IngestResponse:
    parser_name = get_parser_name(request.extension)

    try:
        resolved = resolve_file_reference(
            file_reference_type=request.file_reference_type,
            file_reference_value=request.file_reference_value,
            legacy_file_path=request.file_path,
            extension=request.extension,
        )
    except ValueError as error:
        return build_failed_response(
            request=request,
            parser_name=parser_name,
            error_message=str(error),
            metadata={"errorType": "file_reference_error"},
        )

    file_path = Path(resolved.file_path)

    try: 
        if not file_path.exists():
            return build_failed_response(
                request=request,
                parser_name=parser_name,
                error_message="File path does not exist.",
                metadata={"filePathExists": False},
            )

        if not file_path.is_file():
            return build_failed_response(
                request=request,
                parser_name=parser_name,
                error_message="File path is not a file.",
                metadata={"filePathIsFile": False},
            )
        
        actual_extension = file_path.suffix.strip().lower()
        request_extension = request.extension.strip().lower()

        if actual_extension and actual_extension != request_extension:
            return build_failed_response(
                request=request,
                parser_name=parser_name,
                error_message="File extension does not match request extension.",
                metadata={
                    "errorType": "extension_mismatch",
                    "actualExtension": actual_extension,
                    "requestExtension": request_extension,
                },
            )

        try:
            parsed = parse_by_extension(
                file_path=str(file_path),
                file_name=request.file_name,
                extension=request.extension,
            )

            if not parsed.text.strip():
                return build_failed_response(
                    request=request,
                    parser_name=parser_name,
                    error_message="Parser returned empty text.",
                    metadata={"extension": request.extension},
                )

            return IngestResponse(
                documentId=request.document_id,
                success=True,
                parserName=parsed.parser_name,
                extractedText=parsed.text,
                characterCount=len(parsed.text),
                pageCount=parsed.page_count,
                metadata=parsed.metadata,
                warnings=parsed.warnings,
                errorMessage=None,
            )

        except ValueError as error:
            return build_failed_response(
                request=request,
                parser_name=parser_name,
                error_message=str(error),
                metadata={
                    "extension": request.extension,
                    "errorType": "parser_error",
                },
            )

        except Exception:
            return build_failed_response(
                request=request,
                parser_name=parser_name,
                error_message="Unexpected ingestion error.",
                metadata={
                    "extension": request.extension,
                    "errorType": "unexpected_error",
                },
            )
            
    finally:
        cleanup_resolved_file(resolved)

def get_parser_name(extension: str) -> str:
    normalized = extension.strip().lower()

    if normalized == ".txt":
        return "txt_parser"
    if normalized == ".csv":
        return "csv_parser"
    if normalized == ".xlsx":
        return "xlsx_parser"
    if normalized == ".docx":
        return "docx_parser"
    if normalized == ".pdf":
        return "pdf_parser"

    return "unknown_parser"

def build_failed_response(request: IngestRequest, parser_name: str, error_message: str, metadata: dict | None = None, warnings: list[str] | None = None) -> IngestResponse:    
    return IngestResponse(
        documentId=request.document_id,
        success=False,
        parserName=parser_name,
        extractedText="",
        characterCount=0,
        pageCount=None,
        metadata=metadata or {},
        warnings=warnings or [],
        errorMessage=error_message,
    )