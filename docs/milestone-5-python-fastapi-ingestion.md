# Milestone 5: Python FastAPI Ingestion Service

Trang thai: Completed

Ngay cap nhat: 2026-06-25

## Ket qua da hoan thanh

- Tao service rieng `ai-service-python` bang FastAPI.
- Python service co Swagger docs va endpoint:
  - `GET /health`
  - `POST /ingest`
- Tao Python venv rieng trong `ai-service-python/.venv`.
- Parse thanh cong cac file:
  - `.txt`
  - `.csv`
  - `.xlsx`
  - `.docx`
  - `.pdf` co text layer
- CSV/XLSX dung chien luoc an toan:
  - khong doan header
  - khong tach table regions
  - giu du lieu theo row/cell
- DOCX parser giu dung thu tu block:
  - paragraph
  - table
  - paragraph
- PDF parser chi xu ly text layer.
- PDF scan/no text layer tra failed voi message can OCR.
- Python ingestion service xu ly cac loi:
  - file path khong ton tai
  - file path la folder, khong phai file
  - unsupported extension
  - extension mismatch giua file path va request extension
  - parser error
  - unexpected error
- ASP.NET Core goi duoc Python service qua `PythonIngestionClient`.
- Tao entity/table `DocumentExtractions`.
- Tao endpoint C#:
  - `POST /api/documents/{documentId}/ingest`
- `DocumentIngestionService` da dieu phoi ingestion workflow:
  - check auth
  - check role admin/employee
  - check document chua deleted
  - employee ingest document accessLevel `employee` hoac `guest`
  - set status `processing`
  - goi Python
  - success thi upsert `DocumentExtraction`, set status `extracted`
  - failed thi set status `failed`, luu `ErrorMessage`
  - ghi audit log
- Test end-to-end pass:
  - upload document
  - ingest document
  - extracted text luu vao SQL
  - document status cap nhat dung
  - Python service tat thi document failed
  - PDF scan/no text layer failed ro rang

## Implementation thuc te

### Python files

```text
ai-service-python/
  requirements.txt
  main.py
  app/
    api/
      health.py
      ingestion.py
    models/
      ingest_request.py
      ingest_response.py
    ingestion/
      ingestion_service.py
      parser_registry.py
      parsed_document.py
      text_cleanup.py
      txt_parser.py
      csv_parser.py
      xlsx_parser.py
      docx_parser.py
      pdf_parser.py
```

### ASP.NET Core files

```text
backend-dotnet/
  Infrastructure/
    Python/
      PythonServiceOptions.cs
      PythonIngestionClient.cs

  Contracts/
    Python/
      PythonIngestRequest.cs
      PythonIngestResponse.cs

  Contracts/
    Documents/
      DocumentIngestResponse.cs

  Modules/
    Documents/
      DocumentExtraction.cs
      DocumentIngestionService.cs
      DocumentStatus.cs
      DocumentsController.cs

  Infrastructure/Persistence/
    AppDbContext.cs
```

### Database thuc te

Bang moi:

```text
DocumentExtractions
- Id
- DocumentId
- ExtractedText
- ParserName
- CharacterCount
- PageCount
- MetadataJson
- CreatedAt
- UpdatedAt
```

Migration:

```text
AddDocumentExtractions
```

Quan he:

```text
Documents 1 -> 0..1 DocumentExtractions
```

`Documents` tiep tuc luu metadata file. `DocumentExtractions` luu text da parse.

### Status thuc te

```text
uploaded -> processing -> extracted
uploaded -> processing -> failed
deleted -> khong duoc ingest
```

`indexed` van de Milestone 7 sau khi embedding/Qdrant.

## Flow thuc te

### Flow Python truc tiep

```text
Swagger Python
 -> POST /ingest
 -> app/api/ingestion.py
 -> ingestion_service.ingest_document
 -> check file path
 -> check file la file that
 -> check extension mismatch
 -> parser_registry.parse_by_extension
 -> parser cu the doc file
 -> ParsedDocument
 -> IngestResponse
```

### Flow C# end-to-end

```text
Swagger C#
 -> POST /api/documents/{documentId}/ingest
 -> DocumentsController.Ingest
 -> DocumentIngestionService.IngestAsync
 -> check authenticated
 -> check role admin/employee
 -> query document theo id va status != deleted
 -> employee filter theo AccessLevel employee/guest
 -> set Document.Status = processing
 -> audit document_ingest_started
 -> PythonIngestionClient.IngestAsync
 -> POST ai-service-python /ingest
 -> Python parse file
 -> Python return success/failure
 -> neu success=false:
      Document.Status = failed
      Document.ErrorMessage = python error
      audit document_ingest_failed
      return DocumentIngestResponse success=false
 -> neu success=true:
      upsert DocumentExtraction
      Document.Status = extracted
      Document.ErrorMessage = null
      audit document_ingest_completed
      return DocumentIngestResponse success=true
```

## Parser da implement

### TXT

- Thu encoding:
  - `utf-8`
  - `utf-8-sig`
  - `cp1258`
  - `latin-1`
- Giu tieng Viet/tieng Anh.
- Them header:
  - `Document`
  - `File type`
  - `Encoding`
- Text rong thi failed.

### CSV

- Dung `csv.reader`.
- Dung raw row/cell matrix.
- Khong doan header.
- Bo row rong.
- Output dang:

```text
Row N:
Column 1: ...
Column 2: ...
```

- Metadata:
  - `strategy`
  - `rowCount`
  - `maxColumnCount`
  - `encoding`

### XLSX

- Dung `openpyxl`.
- `load_workbook(data_only=True, read_only=True)`.
- Lap qua workbook worksheets.
- Bo sheet/row rong.
- Output:

```text
Sheet: ...
Row N:
Column N: ...
```

- Metadata:
  - `sheetCount`
  - `sheets`
  - `totalRows`
  - `maxColumnCount`

### DOCX

- Dung `python-docx`.
- Doc body theo block order.
- Khong doc all paragraphs roi all tables.
- `iter_block_items` yield `Paragraph` hoac `Table` theo thu tu xuat hien.
- Table output theo row/cell.
- Metadata:
  - `paragraphCount`
  - `tableCount`
  - `nonEmptyBlockCount`

### PDF

- Dung `pypdf`.
- Chi xu ly PDF text layer.
- Lap qua pages.
- Giu marker `Page N`.
- Neu khong page nao co text:
  - failed
  - message: `No text layer detected. OCR is required.`
- PDF scan de Milestone 9 OCR.
- Metadata:
  - `pageCount`
  - `pagesWithText`
  - `emptyPages`
  - `likelyScanned`

## Error handling thuc te

### Python

`ingestion_service.py` tra `success=false` cho:

- `filePathExists=false`
- `filePathIsFile=false`
- `extension_mismatch`
- `parser_error`
- `unexpected_error`
- unsupported extension
- PDF no text layer/OCR required

Khong de parser error lam crash API neu do la loi co the du doan.

### C#

`DocumentIngestionService` xu ly:

- Python service HTTP/config loi -> document `failed`
- Python response `success=false` -> document `failed`
- Python response `success=true` -> upsert extraction va document `extracted`
- Deleted document -> 404
- Guest/anonymous -> unauthorized/forbidden theo controller mapping

## Noi luu noi dung

- File goc: local storage, duong dan nam trong `Documents.StoragePath`.
- Metadata file: bang `Documents`.
- Text da extract: bang `DocumentExtractions`, field `ExtractedText`.
- Parser metadata: `DocumentExtractions.MetadataJson`.
- Audit khong luu full extracted text.

## OpenAI/RAG sau nay lay text o dau

Khong gui file goc len OpenAI moi lan chat.

Flow sau nay:

```text
DocumentExtractions.ExtractedText
 -> Milestone 6: DocumentChunks
 -> Milestone 7: embeddings + Qdrant
 -> Milestone 8: search chunks lien quan
 -> chi gui chunks lien quan vao OpenAI prompt
```

Ly do:

- Giam token cost.
- Khong gui toan bo document moi lan hoi.
- Giu duoc citation.
- De filter permission theo document/chunk.
- De tai su dung extracted text cho chunking/indexing.

## Test cases da pass

Python service:

- `/health` tra ok.
- `.txt` parse thanh cong.
- `.csv` parse thanh cong.
- `.xlsx` parse thanh cong.
- `.docx` paragraph + table parse thanh cong, giu block order.
- `.pdf` text layer parse thanh cong.
- file path sai -> `success=false`.
- file path la folder -> `success=false`.
- unsupported extension -> `success=false`.
- extension mismatch -> `success=false`.
- PDF scan/no text layer -> `success=false`, OCR required.

C# integration:

- Login admin.
- Upload document.
- Ingest document thanh cong.
- `Documents.Status = extracted`.
- `DocumentExtractions` co `ExtractedText`.
- Python service tat -> document `failed`.
- PDF scan/no text layer -> document `failed`.
- Guest/anonymous khong ingest.
- Employee ingest document accessLevel `employee`/`guest`, khong can la nguoi upload.
- Audit log co started/completed/failed.

## Loi da gap va bai hoc

- Python Swagger `/ingest` khong co nut upload file vi endpoint nhan JSON `filePath`, khong nhan multipart file.
- Path phai tro toi file that, khong phai folder.
- Windows JSON path can `\\`.
- File khong co duoi van co the parse neu request extension dung, nhung file co duoi ma extension request sai thi fail `extension_mismatch`.
- PDF scan khong co text layer, nen `pypdf.extract_text()` tra rong; day la co che phat hien can OCR.
- Neu Python `success=false`, C# phai return failed ngay, khong duoc tiep tuc luu extraction.
- Neu Python service chet, C# phai set document failed de khong ket o `processing`.

## Muc tieu

- Tao `ai-service-python` bang FastAPI de xu ly file da upload.
- ASP.NET Core goi Python service qua HTTP.
- Python nhan `documentId`, `filePath`, `fileName`, `extension`, `contentType`.
- Python extract text co ban tu file:
  - `.txt`
  - `.csv`
  - `.xlsx`
  - `.docx`
  - `.pdf` co text layer
- C# nhan ket qua parse va luu vao SQL Server.
- Document status chuyen dung:
  - `uploaded -> processing -> extracted`
  - `uploaded -> processing -> failed`
- PDF scan/anh chua xu ly trong milestone nay, de Milestone 9 OCR.

## Ly do lam milestone nay

- Milestone 4 moi chi upload file va luu metadata, chua doc duoc noi dung file.
- RAG can text truoc khi chunk, embedding, Qdrant.
- Python phu hop hon C# cho file parsing, pandas, PDF extraction, OCR ve sau.
- Tach ingestion ra service rieng giup ASP.NET Core backend khong bi phinh to.

## Vai tro trong pipeline tong the

```text
Milestone 4:
Upload file -> luu file vat ly + metadata Document

Milestone 5:
Document file -> extracted text -> DocumentExtraction

Milestone 6:
DocumentExtraction.ExtractedText -> DocumentChunks

Milestone 7:
DocumentChunks -> embeddings -> Qdrant

Milestone 8:
Qdrant search -> context -> OpenAI -> answer + citation
```

## Quyet dinh kien truc

- Python service chi parse file va tra ket qua, khong tu ghi SQL Server.
- ASP.NET Core van la backend chinh:
  - check auth/permission
  - cap nhat document status
  - luu extracted text
  - ghi audit log
- Milestone 5 xu ly sync qua HTTP de hoc va debug de hon.
- Background jobs/retry chuan dua sang Milestone 15.
- Chua chunk trong Milestone 5, nhung output phai du sach de Milestone 6 chunk an toan.
- Chua OCR PDF scan trong Milestone 5.
- Chua embedding, Qdrant, RAG trong Milestone 5.

## Kien thuc can hoc

- FastAPI co ban.
- Python virtual environment.
- Pydantic request/response models.
- HTTP service-to-service.
- Timeout va error handling giua .NET va Python.
- File parser co ban.
- Unicode/encoding cho tieng Viet va tieng Anh.
- Cach bien table thanh text de phuc vu RAG.
- Cach giu page/paragraph markers de chunking sau nay khong cat vo nghia.

## Folder structure du kien

```text
ai-service-python/
  requirements.txt
  main.py
  app/
    api/
      health.py
      ingestion.py
    models/
      ingest_request.py
      ingest_response.py
    ingestion/
      ingestion_service.py
      parser_registry.py
      parsed_document.py
      text_cleanup.py
      txt_parser.py
      csv_parser.py
      xlsx_parser.py
      docx_parser.py
      pdf_parser.py
```

Phia ASP.NET Core:

```text
backend-dotnet/
  Infrastructure/
    Python/
      PythonServiceOptions.cs
      PythonIngestionClient.cs

  Contracts/
    Python/
      PythonIngestRequest.cs
      PythonIngestResponse.cs

  Contracts/
    Documents/
      DocumentIngestResponse.cs

  Modules/
    Documents/
      DocumentExtraction.cs
      DocumentIngestionService.cs
      DocumentStatus.cs update them Extracted
      DocumentsController.cs them endpoint ingest

  Infrastructure/Persistence/
    AppDbContext.cs update DbSet<DocumentExtraction>
```

## Database du kien

Them bang `DocumentExtractions`.

```text
DocumentExtractions
- Id
- DocumentId
- ExtractedText
- ParserName
- CharacterCount
- PageCount nullable
- MetadataJson nullable
- CreatedAt
- UpdatedAt
```

Quan he:

```text
Documents 1 -> 0..1 DocumentExtractions
```

Ly do tach bang:

- `Documents` chi nen luu metadata file.
- `DocumentExtractions` luu noi dung text da parse.
- Text co the rat dai, khong nen lam bang `Documents` bi nang.
- Milestone 6 se doc `DocumentExtractions.ExtractedText` de chunk.
- Sau nay co the re-ingest ma khong lam roi metadata goc.

## Document status

Can them status:

```text
extracted
```

Status lifecycle giai doan nay:

```text
uploaded -> processing -> extracted
uploaded -> processing -> failed
deleted: khong duoc ingest
```

`indexed` chi dung sau Milestone 7 khi da embedding va luu Qdrant.

## Python API contract

### GET /health

Response:

```json
{
  "status": "ok",
  "service": "ai-service-python"
}
```

### POST /ingest

Request:

```json
{
  "documentId": "7e7b4fd7-2c64-4f7c-a90d-3fb45f6fdacc",
  "filePath": "E:\\Project\\internal-ai-chatbot\\backend-dotnet\\storage\\uploads\\7e7b4fd7.pdf",
  "fileName": "policy.pdf",
  "contentType": "application/pdf",
  "extension": ".pdf"
}
```

Success response:

```json
{
  "documentId": "7e7b4fd7-2c64-4f7c-a90d-3fb45f6fdacc",
  "success": true,
  "parserName": "pdf_parser",
  "extractedText": "Document: policy.pdf\n\nPage 1:\n...",
  "characterCount": 12000,
  "pageCount": 5,
  "metadata": {
    "strategy": "pdf_text_layer",
    "pagesWithText": 5,
    "emptyPages": []
  },
  "warnings": [],
  "errorMessage": null
}
```

Failed response:

```json
{
  "documentId": "7e7b4fd7-2c64-4f7c-a90d-3fb45f6fdacc",
  "success": false,
  "parserName": "pdf_parser",
  "extractedText": "",
  "characterCount": 0,
  "pageCount": 5,
  "metadata": {
    "strategy": "pdf_text_layer",
    "likelyScanned": true,
    "pagesWithText": 0
  },
  "warnings": [
    "No text layer detected"
  ],
  "errorMessage": "No text layer detected. OCR is required."
}
```

## .NET API du kien

Them endpoint:

```text
POST /api/documents/{documentId}/ingest
```

Quyen:

- `admin`: ingest moi document chua deleted.
- `employee`: ingest document accessLevel `employee` hoac `guest`, bao gom document do employee khac upload.
- `guest`: khong ingest.
- `anonymous`: khong ingest.

Neu document `Status = deleted` thi khong ingest.

## Flow ingestion sync ban dau

```text
Swagger/Frontend
 -> POST /api/documents/{documentId}/ingest
 -> DocumentsController.Ingest
 -> DocumentIngestionService.IngestAsync
 -> check authenticated
 -> query document theo id
 -> check document chua deleted
 -> check quyen ingest
 -> set Document.Status = processing
 -> SaveChangesAsync
 -> PythonIngestionClient.IngestAsync
 -> POST ai-service-python /ingest
 -> Python chon parser theo extension
 -> Python extract text
 -> Python return response
 -> neu success:
      upsert DocumentExtraction
      set Document.Status = extracted
      clear Document.ErrorMessage
   neu failed:
      set Document.Status = failed
      set Document.ErrorMessage = errorMessage
 -> SaveChangesAsync
 -> AuditLogService.LogAsync
 -> return DocumentIngestResponse
```

## Parser output chung trong Python

Moi parser nen tra ve object noi bo co dang:

```text
ParsedDocument
- text
- parserName
- characterCount
- pageCount nullable
- metadata dictionary
- warnings list
```

API layer map `ParsedDocument` sang `IngestResponse`.

Nguyen tac:

- Parser khong duoc crash service neu file loi.
- Loi co the duoc bat va tra `success=false`.
- Parser phai giu du lieu quan trong hon la format dep.
- Parser phai giu markers nhu `Page`, `Sheet`, `Row`, `Paragraph`, `Table`.
- Parser khong duoc xoa dau tieng Viet.
- Parser khong ep lowercase toan bo text.

## Text cleanup chung

Ap dung nhe, khong cleanup qua tay:

- Normalize `\r\n` thanh `\n`.
- Trim dau/cuoi text.
- Trim tung dong.
- Giam nhieu dong trong lien tiep thanh toi da 2 dong trong.
- Giu paragraph/page/sheet/table markers.
- Khong noi tat ca dong thanh 1 dong.
- Khong xoa dau tieng Viet.
- Khong xoa punctuation.

Ly do:

- Cleanup qua manh co the lam mat ranh gioi doan.
- Neu mat ranh gioi doan, Milestone 6 chunking de cat giua y.
- Neu gop qua nhieu doan thanh mot block dai, chunking co the cat vo nghia.

## Parser strategy cho tung extension

### TXT parser

Muc tieu:

- Doc text thuong.
- Ho tro tieng Viet va tieng Anh.

Cach lam:

- Thu encoding theo thu tu:
  - `utf-8`
  - `utf-8-sig`
  - `cp1258`
  - `latin-1`
- Neu doc thanh cong thi cleanup nhe.
- Neu file rong thi tra failed hoac warning ro rang.

Output mau:

```text
Document: policy.txt
File type: TXT

Noi dung file...
```

Metadata:

```json
{
  "strategy": "plain_text",
  "encoding": "utf-8"
}
```

### CSV parser

Quyet dinh:

- Tam thoi dung huong an toan: Table-Like Simple Parser.
- Giu theo row/cell, khong can tach bang.
- Khong tin rang row dau tien luon la header.
- Khong ep file ve mot dinh dang table co dinh.

Ly do:

- CSV noi bo co the co ten bang o dong dau.
- Co file co 1, 2, 3 dong tieu de.
- Co file co nhieu bang trong cung mot file.
- Neu doan sai header thi se mat ngu canh hoac gan sai cot.

Cach lam:

- Doc raw rows bang parser co kha nang fallback encoding.
- Giu thu tu row.
- Dong trong van co tac dung ngan cach block, nhung Milestone 5 chua can tach block.
- Moi row output thanh:

```text
Row N:
Column 1: ...
Column 2: ...
Column 3: ...
```

Output mau:

```text
Document: inventory.csv
File type: CSV
Strategy: raw row/cell matrix

Row 1:
Column 1: Bao cao ton kho thang 6

Row 3:
Column 1: Ma hang
Column 2: Ten hang
Column 3: So luong

Row 4:
Column 1: A01
Column 2: Bolt
Column 3: 20
```

Metadata:

```json
{
  "strategy": "raw_row_cell_matrix",
  "rowCount": 120,
  "maxColumnCount": 8,
  "encoding": "utf-8-sig"
}
```

Future improvement:

- Detect table regions.
- Guess header rows.
- Tach nhieu table trong cung CSV.

### XLSX parser

Quyet dinh:

- Lam luon `.xlsx` trong Milestone 5.
- Tam thoi dung Table-Like Simple Parser.
- Giu theo sheet/row/cell, khong can tach bang.
- Khong doan header phuc tap trong version dau.

Ly do:

- Excel noi bo thuong khong co format co dinh.
- Co file co dong ten bang tren cung.
- Co file co 2-3 dong tieu de.
- Co file co nhieu bang trong cung mot sheet.
- Giai phap row/cell giu du lieu day du, it sai hon doan nham.

Cach lam:

- Doc workbook.
- Lap qua tung sheet.
- Bo qua sheet rong.
- Lap qua row co du lieu.
- Moi cell co gia tri se output theo dang `Column N: value`.
- Khong can giu style, color, formula layout, merged cells trong milestone nay.
- Neu cell la formula, lay gia tri ma thu vien doc duoc.

Output mau:

```text
Document: report.xlsx
File type: XLSX
Strategy: raw sheet/row/cell matrix

Sheet: Inventory

Row 1:
Column 1: Bao cao ton kho thang 6

Row 3:
Column 1: Ma hang
Column 2: Ten hang
Column 3: So luong

Row 4:
Column 1: A01
Column 2: Bolt
Column 3: 20

Sheet: Orders

Row 1:
Column 1: Bang don hang
```

Metadata:

```json
{
  "strategy": "raw_sheet_row_cell_matrix",
  "sheetCount": 2,
  "sheets": ["Inventory", "Orders"],
  "totalRows": 250,
  "maxColumnCount": 12
}
```

Future improvement:

- Detect table regions trong moi sheet.
- Guess title/header.
- Tach nhieu table thanh sections.
- Xu ly merged cells tot hon.

### DOCX parser

Quyet dinh:

- DOCX co the co van ban thuong va table xen ke.
- Parser phai giu thu tu xuat hien trong file.
- Khong doc tat ca paragraphs truoc roi tables sau, vi se lam sai ngu canh.

Cach lam:

- Doc document body theo block order.
- Moi block co the la:
  - paragraph
  - table
- Paragraph rong thi bo qua.
- Table convert thanh row/cell text.
- Neu table co header ro rang thi co the ghi `Row 1` van theo cell, chua can doan header phuc tap.

Output mau:

```text
Document: hr-policy.docx
File type: DOCX

Paragraph:
Quy dinh nghi phep nam

Paragraph:
Nhan vien chinh thuc duoc huong che do nghi phep nhu sau.

Table 1:
Row 1:
Column 1: Loai nhan vien
Column 2: So ngay nghi
Column 3: Ghi chu

Row 2:
Column 1: Nhan vien chinh thuc
Column 2: 12 ngay/nam
Column 3: Khong ap dung thu viec

Paragraph:
Ngay nghi chua su dung se duoc xu ly theo quy dinh cong ty.
```

Metadata:

```json
{
  "strategy": "docx_block_order",
  "paragraphCount": 20,
  "tableCount": 2
}
```

Future improvement:

- Doc header/footer neu can.
- Doc footnote/comment neu can.
- Xu ly image alt text neu co.

### PDF parser

Quyet dinh:

- Milestone 5 chi xu ly PDF co text layer.
- PDF scan/anh de Milestone 9 OCR.
- Parser phai giu page marker de ve sau citation va chunking de hon.
- Khong cleanup PDF qua manh.

Cach lam:

- Mo PDF.
- Dem page count.
- Lap tung page.
- Extract text tu text layer.
- Cleanup nhe theo tung page.
- Neu page co text thi output:

```text
Page N:
...
```

- Neu tong text qua it hoac khong co page nao co text:
  - `success=false`
  - `errorMessage = "No text layer detected. OCR is required."`
  - metadata `likelyScanned = true`

Output mau:

```text
Document: safety-policy.pdf
File type: PDF
Strategy: pdf text layer
PDF pages: 3

Page 1:
Quy dinh an toan lao dong

Nhan vien phai mang day du bao ho lao dong khi vao khu vuc san xuat.

Page 2:
Khu vuc nguy hiem...
```

Metadata:

```json
{
  "strategy": "pdf_text_layer",
  "pageCount": 3,
  "pagesWithText": 2,
  "emptyPages": [3],
  "likelyScanned": false
}
```

## Luu y rieng cho PDF va chunking o Milestone 6

Milestone 5 chua chunk, nhung PDF parser phai tao output than thien voi chunking.

Rui ro can tranh:

- Cleanup qua manh lam mat paragraph breaks.
- Noi nhieu doan khac nhau thanh 1 block dai.
- Xoa het dong trong khien chunker khong biet ranh gioi doan.
- Page text bi cat lung tung lam chunk cat giua cau.
- Header/footer/page number lap lai qua nhieu page gay nhieu.

Giai phap tam thoi:

- Giu marker `Page N`.
- Giu toi da 1-2 dong trong giua paragraph.
- Khong noi toan bo page thanh mot dong duy nhat.
- Khong xoa punctuation.
- Khong cat text trong Milestone 5.
- Neu page qua dai, de Milestone 6 chunker xu ly theo paragraph va sentence boundary.
- Neu parser phat hien text qua it, bao OCR required thay vi tao extracted text rong.

Ghi chu cho Milestone 6:

- Chunker nen uu tien split theo:
  1. page boundary
  2. paragraph boundary
  3. sentence boundary
  4. moi cat theo size neu bat buoc
- Chunker khong nen cat ngay giua cau neu con boundary gan do.
- Chunker nen co overlap nho de giu ngu canh.
- Chunk metadata nen giu `pageNumber` neu text den tu PDF.

## Tieng Viet va tieng Anh

Nguyen tac:

- Giu Unicode goc.
- Uu tien encoding UTF-8/UTF-8-SIG.
- TXT/CSV co fallback `cp1258`, `latin-1`.
- Khong bo dau tieng Viet.
- Khong lowercase toan bo text.
- Khong xoa ky tu dac biet neu co kha nang la ma hang, ma nhan vien, ma phong ban.
- Giu label `Column`, `Row`, `Sheet`, `Page`, `Paragraph` bang tieng Anh de format on dinh trong parser.
- Noi dung cell/paragraph giu nguyen tieng Viet/tieng Anh.

## Config can co

Phia C#:

```json
{
  "PythonService": {
    "BaseUrl": "http://localhost:8000",
    "TimeoutSeconds": 60
  }
}
```

Phia Python co the them sau:

```text
MAX_EXTRACTED_TEXT_CHARS
MAX_ROWS_PER_SHEET
MAX_SHEETS_PER_WORKBOOK
MAX_PDF_PAGES
```

Milestone 5 co the de default trong code/config don gian, chua can hardening nang.

## Audit log

Nen log:

- `document_ingest_started`
- `document_ingest_completed`
- `document_ingest_failed`

Metadata an toan:

```json
{
  "documentId": "...",
  "extension": ".xlsx",
  "parserName": "xlsx_parser",
  "characterCount": 12000,
  "pageCount": null,
  "status": "extracted"
}
```

Khong log:

- extracted text full
- noi dung file
- API key
- cookie/token
- full local path neu khong can

## Error handling

Python:

- File path khong ton tai -> `success=false`.
- Extension khong support -> `success=false`.
- Parser exception -> bat loi va tra `success=false`.
- PDF scan/no text layer -> `success=false`, error OCR required.

C#:

- Python service khong chay -> document `failed`, error message ro rang.
- Python timeout -> document `failed`.
- Python `success=false` -> document `failed`.
- Python success -> upsert `DocumentExtraction`, document `extracted`.
- Deleted document -> khong ingest.
- Guest/anonymous -> khong ingest.

## Test cases

Python service:

- `GET /health` tra ok.
- `.txt` UTF-8 tieng Viet parse dung dau.
- `.txt` file rong tra warning/failed ro rang.
- `.csv` co ten bang dong dau parse theo row/cell.
- `.csv` co nhieu dong tieu de khong crash.
- `.xlsx` nhieu sheet parse sheet/row/cell.
- `.xlsx` sheet co 2 bang trong mot sheet van giu du row/cell.
- `.docx` paragraph + table xen ke giu dung thu tu.
- `.pdf` text layer parse theo page.
- `.pdf` scan/no text layer tra OCR required.
- File path sai tra failed ro rang.

ASP.NET Core integration:

- Upload file `.txt`, ingest thanh cong, SQL co `DocumentExtraction`.
- Upload file `.csv`, ingest thanh cong.
- Upload file `.xlsx`, ingest thanh cong.
- Upload file `.docx`, ingest thanh cong.
- Upload file `.pdf` text layer, ingest thanh cong.
- Python service tat -> document status `failed`.
- Guest ingest bi chan.
- Anonymous ingest bi chan.
- Employee ingest duoc document accessLevel `employee`/`guest`.
- Employee khong ingest duoc document accessLevel `admin`.
- Admin ingest moi document chua deleted.
- Deleted document khong ingest duoc.
- Audit log co ingest action, khong chua full extracted text.

## Dau hieu hoan thanh

- `ai-service-python` chay rieng va co Swagger docs.
- ASP.NET Core goi duoc Python service.
- Parse duoc `.txt`, `.csv`, `.xlsx`, `.docx`, `.pdf` text layer.
- Extracted text duoc luu vao SQL trong `DocumentExtractions`.
- Document status cap nhat dung `processing`, `extracted`, `failed`.
- Parser loi khong lam crash Python service hoac C# backend.
- Output parser du du lieu cho Milestone 6 chunking.
- Quyen ingest duoc enforce.

## Chua lam trong Milestone 5

- Chunking that su.
- Embedding.
- Qdrant.
- RAG.
- OCR PDF scan.
- Background job queue.
- Retry/backoff nang cao.
- Virus scanning.
- Detect table regions nang cao.
- Guess header rows nang cao.
- PDF table extraction nang cao.
- Download/preview file.

## Can update sau khi hoan thanh

- Parser library thuc te da chon.
- Contract Python-C# thuc te neu co thay doi.
- Status thuc te.
- Migration thuc te.
- Test cases da pass.
- Loi parsing thuc te gap phai.
- Gioi han size/page/row neu da them.
