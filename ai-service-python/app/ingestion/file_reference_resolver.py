from dataclasses import dataclass
from pathlib import Path
from tempfile import NamedTemporaryFile

import httpx


@dataclass
class ResolvedFileReference:
    file_path: str
    should_cleanup: bool


def resolve_file_reference(file_reference_type: str,file_reference_value: str,legacy_file_path: str,extension: str) -> ResolvedFileReference:
    # Muc tieu:
    # 1. Neu file_reference_type rong hoac local_path:
    #    - dung file_reference_value neu co
    #    - neu khong co thi fallback legacy_file_path
    # 2. Neu file_reference_type la sas_url:
    #    - download URL ve file tam
    #    - return path file tam
    # 3. Neu type khong ho tro thi raise ValueError.

    normalized_type = (file_reference_type or "local_path").strip().lower()

    if normalized_type == "local_path":
        path = file_reference_value.strip() or legacy_file_path
        return ResolvedFileReference(
            file_path=path,
            should_cleanup=False,
        )

    if normalized_type == "sas_url":
        return download_sas_url_to_temp_file(
            sas_url=file_reference_value,
            extension=extension,
        )

    raise ValueError(f"Unsupported file reference type: {normalized_type}")


def download_sas_url_to_temp_file(sas_url: str, extension: str) -> ResolvedFileReference:
    # Muc tieu:
    # 1. Check sas_url khong rong.
    # 2. Tao extension an toan, vi du ".pdf".
    # 3. Tao temp file bang NamedTemporaryFile(delete=False, suffix=extension).
    # 4. Dung httpx stream/download noi dung tu sas_url.
    # 5. Ghi bytes vao temp file.
    # 6. Return ResolvedFileReference(file_path=temp_path, should_cleanup=True).

    if not sas_url or not sas_url.strip():
        raise ValueError("SAS URL is missing.")

    normalized_extension = extension.strip().lower()
    if not normalized_extension.startswith("."):
        normalized_extension = "." + normalized_extension

    with NamedTemporaryFile(delete=False, suffix=normalized_extension) as temp_file:
        temp_path = temp_file.name

        with httpx.stream("GET", sas_url, timeout=60.0) as response:
            response.raise_for_status()

            for chunk in response.iter_bytes():
                temp_file.write(chunk)

    return ResolvedFileReference(
        file_path=temp_path,
        should_cleanup=True,
    )


def cleanup_resolved_file(resolved: ResolvedFileReference) -> None:
    # Muc tieu:
    # Neu file la temp file thi xoa sau khi parse xong.
    # Neu la local path that thi khong xoa.

    if not resolved.should_cleanup:
        return

    path = Path(resolved.file_path)

    try:
        if path.exists() and path.is_file():
            path.unlink()
    except OSError:
        # Cleanup temp file la best-effort. Khong de loi xoa file tam lam hong response chinh.
        return
