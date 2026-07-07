from pathlib import Path


def render_pdf_pages_to_images(pdf_path: str, output_dir: str, dpi: int = 200, max_pages: int | None = None) -> list[str]:
    # Bai tap Milestone 9:
    # Convert PDF pages thanh PNG images de dua vao OCR.
    #
    # 1. Validate pdf_path ton tai.
    # 2. Tao output_dir neu chua co.
    # 3. Import fitz trong ham nay:
    #    import fitz
    # 4. Mo PDF:
    #    document = fitz.open(pdf_path)
    # 5. Lap tung page:
    #    page = document.load_page(page_index)
    #    pix = page.get_pixmap(matrix=fitz.Matrix(scale, scale))
    #    pix.save(image_path)
    # 6. Return list duong dan image da tao.
    #
    # DPI:
    # - 200 la muc kha on cho OCR co ban.
    # - DPI cao hon co the OCR tot hon nhung cham va ton RAM.
    #
    # scale:
    # - PyMuPDF mac dinh 72 DPI.
    # - scale = dpi / 72.
    pdf = Path(pdf_path)

    if not pdf.exists():
        raise ValueError("PDF path does not exist.")

    if not pdf.is_file():
        raise ValueError("PDF path is not a file.")

    import fitz

    output = Path(output_dir)
    output.mkdir(parents=True, exist_ok=True)

    image_paths: list[str] = []

    scale = dpi / 72
    matrix = fitz.Matrix(scale, scale)

    with fitz.open(str(pdf)) as document:
        page_count = len(document)

        pages_to_render = page_count

        if max_pages is not None:
            pages_to_render = min(page_count, max_pages)

        for page_index in range(pages_to_render):
            page = document.load_page(page_index)
            pixmap = page.get_pixmap(matrix=matrix)

            image_path = output / f"page_{page_index + 1}.png"
            pixmap.save(str(image_path))

            image_paths.append(str(image_path))

    return image_paths