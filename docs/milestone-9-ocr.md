# Milestone 9: OCR

Trang thai: Completed

Ngay cap nhat: 2026-06-27

Muc tieu:
- Doc duoc PDF scan hoac anh.
- Dua text OCR vao pipeline ingestion/chunking/RAG.
- Neu OCR loi thi document status Failed voi reason ro rang.

Ly do lam milestone nay:
- Nhieu tai lieu nha may/co quan la scan PDF, khong co text layer.
- Neu chi dung PDF text extraction, file scan se bi empty.
- OCR mo rong kha nang search tai lieu noi bo.

Kien thuc can hoc:
- OCR la gi.
- Tesseract hoac cloud OCR.
- PDF page to image.
- DPI va image preprocessing.
- Vietnamese/English language model.
- Text cleanup sau OCR.

Pham vi nen lam:
- Detect file PDF co text hay khong.
- Convert PDF page sang image neu can.
- OCR image/page.
- Combine OCR text theo page.
- Luu extracted text va page metadata.

Pham vi chua nen lam:
- OCR async scale lon.
- Layout reconstruction nang cao.
- Table OCR chuan.
- Handwriting OCR.

Python module du kien:
- python-service/app/ocr/ocr_service.py
- python-service/app/ocr/pdf_to_image.py
- python-service/app/ocr/image_preprocess.py
- python-service/app/ocr/text_cleanup.py

Python module da tao thuc te:
- `ai-service-python/app/ocr/__init__.py`
- `ai-service-python/app/ocr/ocr_models.py`
- `ai-service-python/app/ocr/ocr_service.py`
- `ai-service-python/app/ocr/pdf_to_images.py`
- `ai-service-python/app/ocr/pdf_ocr_parser.py`
- `ai-service-python/app/ocr/image_preprocess.py`
- `ai-service-python/app/ocr/text_cleanup.py`
- `ai-service-python/app/api/ocr.py`

Python module da cap nhat:
- `ai-service-python/app/ingestion/pdf_parser.py`
- `ai-service-python/app/ingestion/parser_registry.py`
- `ai-service-python/main.py`

Config du kien:
- OCR enabled true/false.
- OCR language: eng, vie, eng+vie.
- DPI.
- Max pages OCR.
- Timeout per page.

Config/tool thuc te:
- OCR engine: Tesseract `v5.5.0.20241111`.
- OCR languages da co tren may dev: `vie`, `eng` va nhieu ngon ngu khac.
- Language mac dinh trong code: `vie+eng`.
- PDF render library: PyMuPDF, import bang `fitz`.
- Image library: Pillow.
- Python wrapper cho Tesseract: pytesseract.
- DPI render PDF mac dinh: `200`.

Flow OCR:
Document ingestion
 -> try normal text extraction
 -> neu text rong/qua it va file la PDF/image
 -> convert pages to images
 -> OCR each page
 -> cleanup text
 -> return extracted text with page info
 -> chunking/indexing tiep tuc

Flow thuc te:

```text
POST /ingest
 -> ingestion_service.ingest_document
 -> parser_registry.parse_by_extension
 -> .pdf => parse_pdf_auto
 -> parse_pdf_auto thu parse_pdf text layer truoc
 -> neu text layer co text:
      return pdf_parser
 -> neu parse_pdf bao "OCR is required":
      parse_pdf_with_ocr
 -> render_pdf_pages_to_images
 -> ocr_image_file tung page
 -> cleanup_ocr_text
 -> return ParsedDocument parser_name="pdf_ocr_parser"
 -> ingestion_service map sang IngestResponse
```

## Quyet dinh kien truc

### 1. Text layer first, OCR fallback sau

Thu tu xu ly PDF:

```text
parse_pdf text layer
 -> fallback parse_pdf_with_ocr neu khong co text layer
```

Ly do:
- Text layer nhanh hon OCR.
- Text layer chinh xac hon OCR voi PDF binh thuong.
- OCR cham hon va co the sai dau tieng Viet/ky hieu/bang.
- Khong nen OCR moi PDF neu PDF da co text that.

### 2. Fallback dat trong `pdf_parser.py`

Da tao `parse_pdf_auto`.

Khong dat OCR fallback trong `ingestion_service.py` vi:
- `ingestion_service.py` nen generic: validate file, goi parser, map response.
- OCR fallback la logic rieng cua PDF.
- Sau nay sua heuristic OCR/config chi can sua quanh PDF parser.

`parser_registry.py` goi:

```text
.pdf -> parse_pdf_auto
```

### 3. PyMuPDF thay vi pdf2image/Poppler

Chon PyMuPDF de render PDF pages thanh image.

Ly do:
- De dung tren Windows hon.
- Khong can cai Poppler rieng.
- Du tot cho OCR baseline.

## Ham chinh

### `ocr_image_file`

Nhiem vu:
- Nhan image path.
- Mo image bang Pillow.
- Goi `preprocess_for_ocr`.
- Goi `pytesseract.image_to_string`.
- Cleanup text.
- Return `OcrPageResult`.

Dung de test OCR anh don va OCR tung page cua PDF.

### `preprocess_for_ocr`

Nhiem vu:
- Xu ly anh nhe truoc OCR.

Ban dau chi lam grayscale:

```text
image.convert("L")
```

Khong lam threshold/denoise manh trong Milestone 9 de tranh lam mat dau tieng Viet hoac lam hong anh tot.

### `render_pdf_pages_to_images`

Nhiem vu:
- Convert PDF pages thanh PNG images.
- Dung PyMuPDF `fitz`.
- DPI mac dinh `200`.
- Return list image paths.

### `parse_pdf_with_ocr`

Nhiem vu:
- Tao temporary directory.
- Render PDF thanh images.
- OCR tung image.
- Ghep text theo page marker.
- Return `ParsedDocument` voi:
  - `parser_name = pdf_ocr_parser`
  - `metadata.strategy = pdf_ocr_fallback`

### `parse_pdf_auto`

Nhiem vu:
- Dieu phoi PDF parsing.

Flow:

```text
try parse_pdf
catch ValueError "OCR is required"
 -> parse_pdf_with_ocr
```

Neu ValueError khac thi raise lai, khong nuot loi.

## API test OCR anh don

Endpoint:

```http
POST /ocr/image
```

Request:

```json
{
  "imagePath": "E:\\test-ocr.png",
  "language": "vie+eng"
}
```

Response:

```json
{
  "success": true,
  "text": "...",
  "characterCount": 123,
  "warnings": [],
  "errorMessage": null
}
```

Cach test:
- Upload PDF scan.
- OCR ra text khong rong.
- Search duoc noi dung trong PDF scan sau khi index.
- File scan mo tra Failed hoac warning ro rang.
- OCR language tieng Viet co dau duoc chap nhan o muc co ban.

Test da pass:
- `tesseract --version` tra ve Tesseract v5.5.
- `tesseract --list-langs` co `vie` va `eng`.
- `/ocr/image` doc duoc anh don va tra text.
- `render_pdf_pages_to_images` render PDF thanh PNG thanh cong.
- `parse_pdf_with_ocr` doc duoc file scan `E:\test.pdf`, 21 pages, hon 31k characters.
- `parse_by_extension(..., ".pdf")` tu fallback sang `pdf_ocr_parser`.
- `/ingest` Python pass voi PDF scan.
- PDF text layer cu van dung `pdf_parser`.

## Gioi han da phat hien

### OCR khong hoan hao

OCR output co the:
- mat dau tieng Viet o mot so tu.
- doc sai ky tu dac biet.
- vo layout bang.
- doc sai cong thuc/ky hieu toan.
- bi nhieu neu scan mo/lẹch/thap DPI.

Ket luan:
- OCR baseline du cho search/RAG noi dung co ban.
- Khong dung OCR text de tinh toan bang tai chinh/chinh xac.
- Bang/CSV/XLSX tinh toan se theo ADR-002 dataset metadata/profiling/analysis.

### OCR cham hon text layer

OCR can render tung page va chay Tesseract.

Sau nay nen dua OCR vao background job Milestone 15 neu file dai.

### Chua OCR image upload trong C# backend

Milestone 9 tap trung PDF scan fallback.

Endpoint `/ocr/image` dung de test Python OCR anh don.
Neu sau nay cho upload anh truc tiep, can mo rong Document extension va parser registry.

Dau hieu hoan thanh:
- PDF scan khong con empty text.
- OCR loi thi status Failed voi reason.
- OCR output di tiep vao chunking/RAG duoc.

Trang thai thuc te:
- Hoan thanh core OCR pipeline.
- PDF scan da OCR duoc va tra `pdf_ocr_parser`.
- OCR fallback da tich hop vao `/ingest`.
- San sang chunk/index/RAG tren text OCR.

## Viec de danh cho milestone sau

- Milestone 15:
  - Dua OCR vao background job.
  - Retry khi OCR loi.
  - Gioi han max pages/file size.
- Milestone 16:
  - Rate limit OCR.
  - Audit OCR request.
  - Security scan file upload.
- Dataset/table milestone sau:
  - Khong dung OCR text de tinh toan bang chinh xac.
  - Can table extraction/profiling rieng.
