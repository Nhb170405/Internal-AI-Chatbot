# Milestone 6: Chunking

Trang thai: Completed

Ngay cap nhat: 2026-06-26

## Ket qua da hoan thanh

- Tao Python endpoint `POST /chunk`.
- Tao baseline chunker trong Python:
  - paragraph-aware
  - character-based
  - uu tien cat theo dau cau/khoang trang thay vi cat cung ngay giua tu
  - co overlap co ban giua cac chunk
- Tao C# `PythonChunkingClient` de backend goi Python `/chunk`.
- Tao entity/table `DocumentChunks`.
- Tao `DocumentChunkingService` de dieu phoi chunk workflow.
- Tao API C#:
  - `POST /api/documents/{documentId}/chunk`
  - `GET /api/documents/{documentId}/chunks`
- Them document status `chunked`.
- Chunk result duoc luu vao SQL Server.
- Re-chunk khong bi nhan doi chunks vi service xoa chunk cu truoc khi insert chunk moi.
- Permission da test:
  - admin chunk/read chunks moi document.
  - employee chunk/read chunks document accessLevel `employee`/`guest`.
  - employee khong read chunks document accessLevel `admin`; API tra `404 document_not_found` de khong leak su ton tai cua document.
  - guest duoc read chunks document accessLevel `guest`.
  - guest khong duoc chunk.
- Test voi file nhieu heading cho thay baseline chua toi uu heading-aware, nhung du chap nhan cho Milestone 6.

## Muc tieu

- Lay text da extract tu `DocumentExtractions`.
- Chia text dai thanh nhieu chunk nho hon.
- Moi chunk co metadata de truy vet ve document goc.
- Luu chunks vao SQL Server trong bang `DocumentChunks`.
- Chuan bi dau vao cho Milestone 7: embedding va Qdrant.

## Vi sao can chunking

LLM va embedding model khong nen nhan nguyen mot file dai:

- File dai ton token.
- Search semantic tren ca file lon se kem chinh xac.
- Chunk qua dai lam retrieval bi nhieu thong tin thua.
- Chunk qua ngan lam mat ngu canh.
- Chunk co metadata giup citation sau nay biet cau tra loi lay tu file nao, chunk nao.

Milestone 5 moi dung o buoc:

```text
Document file -> extracted text
```

Milestone 6 them buoc:

```text
extracted text -> chunks
```

Sau Milestone 6, flow du kien se la:

```text
Documents
 -> DocumentExtractions
 -> DocumentChunks
 -> Milestone 7: Qdrant vectors
 -> Milestone 8: RAG answer with citation
```

## Rule permission da chot

Quyen doc/ingest/chunk/search dua tren `Document.AccessLevel`.

```text
anonymous:
- khong doc
- khong upload
- khong ingest
- khong chunk

guest:
- doc accessLevel = guest
- khong upload
- khong ingest
- khong chunk

employee:
- doc/ingest/chunk accessLevel = employee hoac guest
- duoc thao tac voi file employee/guest cua employee khac
- upload accessLevel = employee hoac guest

admin:
- doc/ingest/chunk moi accessLevel
- upload moi accessLevel
```

Rieng delete/restore giu nhu Milestone 4:

```text
admin:
- delete/restore moi file

employee:
- chi delete/restore file do chinh employee do upload
```

`UploadedByUserId` van duoc giu de audit/debug va delete/restore owner check. Khong dung owner de chan read/ingest/chunk.

## Kien thuc can hoc

- Chunk size la gi.
- Chunk overlap la gi.
- Character-based chunking vs token-based chunking.
- Split theo paragraph/page marker.
- Metadata cho retrieval.
- Vi sao can idempotent re-chunk.
- Vi sao khong nen chunk truc tiep trong controller.

## Pham vi Milestone 6

Lam:

- Python endpoint `POST /chunk`.
- Python text chunker ban dau.
- C# `PythonChunkingClient`.
- C# `DocumentChunkingService`.
- Entity/table `DocumentChunks`.
- API C# `POST /api/documents/{documentId}/chunk`.
- API debug `GET /api/documents/{documentId}/chunks`.

Chua lam:

- Embedding.
- Qdrant.
- RAG prompt.
- Citation.
- OCR.
- Chunking theo token that su bang tokenizer.
- Semantic chunking bang AI.

## Python module thuc te

```text
ai-service-python/
  app/
    api/
      chunking.py
    models/
      chunk_request.py
      chunk_response.py
    chunking/
      __init__.py
      chunk_models.py
      text_chunker.py
```

## ASP.NET Core module thuc te

```text
backend-dotnet/
  Contracts/
    Documents/
      DocumentChunkResponse.cs
      DocumentChunkingResponse.cs
    Python/
      PythonChunkRequest.cs
      PythonChunkResponse.cs
      PythonChunkItem.cs

  Infrastructure/
    Python/
      PythonChunkingClient.cs

  Modules/
    Documents/
      DocumentChunk.cs
      DocumentChunkingService.cs
      DocumentStatus.cs
      DocumentsController.cs

  Infrastructure/
    Persistence/
      AppDbContext.cs
```

## Database thuc te

Bang moi:

```text
DocumentChunks
- Id
- DocumentId
- ChunkIndex
- Content
- CharacterCount
- StartOffset nullable
- EndOffset nullable
- MetadataJson nullable
- CreatedAt
```

Quan he:

```text
Documents 1 -> n DocumentChunks
```

Index nen co:

```text
DocumentId + ChunkIndex unique
```

Ly do:

- Mot document co nhieu chunk.
- `ChunkIndex` giup giu thu tu goc.
- Unique index giup tranh duplicate khi re-chunk.

## Status du kien

Status moi:

```text
chunked
```

Flow:

```text
uploaded -> processing -> extracted -> chunked
uploaded -> processing -> failed
extracted -> processing -> chunked
chunked -> processing -> chunked
```

Ghi chu:

- `processing` co the dung tam cho ingestion va chunking trong version dau.
- Sau nay neu can chi tiet hon co the tach `extracting`, `chunking`, `indexing`.

## Flow Python `/chunk`

```text
POST /chunk
 -> nhan documentId, text, chunkSize, chunkOverlap
 -> validate text khong rong
 -> normalize line endings
 -> split thanh paragraph
 -> neu paragraph qua dai thi cat mem theo dau cau/khoang trang
 -> gom paragraph den gan chunkSize
 -> them overlap neu config > 0
 -> return danh sach chunks
```

Response moi chunk:

```text
chunkIndex
content
characterCount
startOffset
endOffset
metadata
```

## Flow C# end-to-end

```text
Swagger C#
 -> POST /api/documents/{documentId}/chunk
 -> DocumentsController.Chunk
 -> DocumentChunkingService.ChunkAsync
 -> check authenticated
 -> check role admin/employee
 -> query document theo accessLevel
 -> document phai co status extracted/chunked
 -> lay DocumentExtraction.ExtractedText
 -> set Document.Status = processing
 -> audit document_chunk_started
 -> PythonChunkingClient.ChunkAsync
 -> POST ai-service-python /chunk
 -> Python tra chunks
 -> xoa chunks cu cua document
 -> insert chunks moi
 -> set Document.Status = chunked
 -> audit document_chunk_completed
 -> return DocumentChunkingResponse
```

Neu Python failed:

```text
Document.Status = failed
Document.ErrorMessage = safe message
audit document_chunk_failed
return success=false
```

## Thuat toan chunking thuc te

Dung character-based chunking de hoc va de debug.

Config mac dinh:

```text
chunkSize = 1200 characters
chunkOverlap = 150 characters
```

Y tuong:

- Normalize text.
- Tach paragraph bang blank line.
- Neu paragraph ngan: gom nhieu paragraph vao mot chunk.
- Neu paragraph qua dai: cat mem theo dau cau truoc, sau do moi toi newline/khoang trang/do dai cung.
- Overlap lay phan cuoi chunk truoc de them vao chunk sau.
- Overlap co cleanup don gian de tranh bat dau bang tu qua ngan/vo nghia.
- Khong tao chunk rong.

Trade-off:

- Character-based de lam, de nhin trong SQL.
- Chua toi uu chinh xac theo token.
- Sau nay neu can co the thay bang token-based chunker ma khong doi database qua nhieu.
- Chua heading-aware that su, nen file co nhieu tieu de co the chunk chua dep bang tai lieu paragraph thuan.

Quyet dinh da chot:

- Khong dung LangChain ngay o Milestone 6.
- Tu viet baseline de hieu ro chunking.
- Sau Milestone 7/8 neu retrieval kem thi co the so sanh voi `RecursiveCharacterTextSplitter` hoac viet strategy rieng theo file type.
- Chunking phu thuoc input/document type, khong co giai thuat tot nhat cho moi tai lieu.

## API du kien

```http
POST /api/documents/{documentId}/chunk
```

Dung de tao chunks cho document da extract.

```http
GET /api/documents/{documentId}/chunks
```

Dung de debug xem chunks da luu.

## Cach test

1. Text ngan:
   - upload txt ngan
   - ingest
   - chunk
   - ky vong 1 chunk

2. Text dai:
   - upload txt/docx dai
   - ingest
   - chunk
   - ky vong nhieu chunk

3. Re-chunk:
   - goi chunk lan 1
   - goi chunk lan 2
   - ky vong so chunk khong bi nhan doi

4. Document chua extract:
   - upload nhung chua ingest
   - goi chunk
   - ky vong 400 invalid state

5. Permission:
   - admin chunk moi document
   - employee chunk document accessLevel employee/guest
   - employee khong chunk document accessLevel admin
   - guest/anonymous khong chunk
   - guest read chunks cua document accessLevel `guest`

6. Debug chunks:
   - goi GET chunks
   - xem chunkIndex tang dung
   - content khong rong
   - DocumentId dung

## Test cases da pass

- Python `/chunk` voi text rong:
  - HTTP 200.
  - `success=false`.
  - `chunkCount=0`.
  - `errorMessage="Text is empty."`.
- Python `/chunk` voi text ngan:
  - `success=true`.
  - tao duoc chunk.
- Python `/chunk` voi text dai:
  - `success=true`.
  - tao nhieu chunks.
  - `chunkIndex` tang dung.
  - chunk content khong rong.
- C# `POST /api/documents/{documentId}/chunk`:
  - document extracted -> chunk thanh cong.
  - document status thanh `chunked`.
  - SQL co rows trong `DocumentChunks`.
- C# `GET /api/documents/{documentId}/chunks`:
  - tra ve chunks da luu trong SQL.
- Re-chunk:
  - khong nhan doi chunks cu.
- Permission:
  - employee khong doc chunks cua document admin-level, API tra `404 document_not_found`.
  - cac case con lai da pass theo rule permission.

## Loi/edge cases da gap

- Baseline ban dau cat giua cau, tao chunk ket thuc bang cum cut nghia nhu `"Day la"`.
- Da cai tien `_split_long_paragraph` de uu tien cat tai dau cau truoc khi cat theo khoang trang.
- Overlap ban dau co the bat dau bang tu ngan vo nghia nhu `"la"`.
- Da cai tien `_get_overlap_text` de cleanup overlap ngan/ky cuc.
- Text co nhieu heading lam chunk chua dep bang tai lieu paragraph thuan.
- Da chap nhan day la limitation cua baseline va de heading-aware/structure-aware chunking cho phase sau.

## Known limitations

- Chua token-based, nen chunk size tinh theo character.
- Chua heading-aware thuc su.
- Chua table-aware: CSV/XLSX da extract dang row/cell text, chunker van xu ly nhu text thuong.
- Chua luu pageNumber/source block vao `DocumentChunks`.
- `StartOffset`/`EndOffset` hien co the de null.
- Chua co evaluation bang recall@k vi chua den Milestone 7/8.

## Dau hieu hoan thanh

- Document da extracted co the chunk thanh mot hoac nhieu chunks.
- Chunks luu vao SQL.
- Re-chunk khong tao duplicate.
- Permission dung rule moi.
- Document status cap nhat thanh `chunked`.
- San sang dua `DocumentChunks` sang embedding o Milestone 7.

## Ghi chu cho Milestone 7

- Milestone 7 se dung `DocumentChunks` lam input de tao embeddings.
- Qdrant payload toi thieu nen co:
  - `documentId`
  - `chunkId`
  - `chunkIndex`
  - `accessLevel`
  - `status`
- Khi search vector, phai filter theo accessLevel va bo document deleted.
- Neu retrieval sau nay kem, quay lai cai tien chunker:
  - heading-aware
  - structure-aware
  - table-aware
  - LangChain RecursiveCharacterTextSplitter
