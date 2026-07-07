# Milestone 7: Embedding And Qdrant

Trang thai: Completed

Ngay cap nhat: 2026-06-27

## Muc tieu

- Convert `DocumentChunks` thanh embedding vectors.
- Luu vectors vao Qdrant.
- Search semantic bang query cua user.
- Ket qua search tra ve chunk lien quan kem score va metadata source.
- Chuan bi nen tang cho Milestone 8: RAG co citation.

Sau Milestone 6, pipeline hien tai la:

```text
Documents
 -> DocumentExtractions
 -> DocumentChunks
```

Milestone 7 them lop vector:

```text
Documents
 -> DocumentExtractions
 -> DocumentChunks
 -> Qdrant points/vectors
```

## Ly do lam milestone nay

- RAG can tim dung doan tai lieu lien quan truoc khi goi OpenAI tao cau tra loi.
- Embedding bien text thanh vector semantic, giup search theo y nghia chu khong chi keyword.
- Qdrant luu vectors va payload metadata de search nhanh va filter theo permission.
- Neu khong co retrieval, chatbot se phai gui qua nhieu text vao prompt, ton token va kem chinh xac.

## Kien thuc can hoc

- Embedding la gi.
- Vector similarity la gi.
- Cosine similarity.
- Qdrant collection.
- Qdrant point id.
- Vector va payload.
- Upsert/search/delete points.
- Payload filter.
- Tai sao can filter `accessLevel` khi search.
- Tai sao indexing va search nen tach rieng voi chat answer.

## Quyet dinh kien truc Milestone 7

Chot huong:

```text
ASP.NET Core:
- dieu phoi auth/permission/database/API.
- doc DocumentChunks tu SQL.
- goi Python service de index/search vector.

Python FastAPI:
- tao embedding.
- lam viec voi Qdrant.
- cung cap endpoint /index-document va /search.

Qdrant:
- luu vector cua tung chunk.
- luu payload metadata can de truy vet va filter.
```

Ly do chon Python cho embedding/Qdrant:

- Python co ecosystem AI/vector tot.
- Qdrant Python client pho bien.
- Sau nay metadata extraction, reranking, hybrid search cung de lam ben Python.
- ASP.NET Core van giu vai tro backend chinh va permission boundary.

Trade-off:

- Python service can cau hinh OpenAI key/Qdrant URL rieng.
- C# phai gui chunks sang Python khi index.
- Co them HTTP hop giua C# va Python.

Chap nhan trade-off nay de giu module AI/vector tap trung trong Python.

## Pham vi Milestone 7

Lam:

- Cai Qdrant local bang Docker.
- Tao Python config cho OpenAI embedding va Qdrant.
- Tao Python embedding service.
- Tao Python Qdrant service.
- Tao Python endpoint index document chunks.
- Tao Python endpoint search semantic.
- Tao C# `PythonVectorClient`.
- Tao C# `DocumentIndexingService`.
- Tao API:
  - `POST /api/documents/{documentId}/index`
  - `GET /api/documents/search`
- Update `Document.Status = indexed` khi index thanh cong.
- Audit log indexing/search co ban.

Chua lam:

- Chua tao cau tra loi RAG bang OpenAI chat.
- Chua citation UI day du.
- Chua reranking.
- Chua hybrid metadata search.
- Chua background indexing job.
- Chua permission nang cao theo department/user cu the.
- Chua hard delete vectors theo retention job.

## Qdrant collection thuc te

Collection:

```text
internal_documents
```

Distance:

```text
Cosine
```

Vector size:

```text
phu thuoc embedding model duoc cau hinh.
Voi text-embedding-3-small hien tai: 1536.
```

Payload moi point:

```text
documentId
chunkId
chunkIndex
accessLevel
documentStatus
originalFileName
content
metadata
```

Ghi chu quan trong:

- Qdrant collection co vector size co dinh tai thoi diem tao collection.
- Neu test dev bang vector size nho, vi du 3, sau do index OpenAI embedding 1536 thi se loi.
- Khi doi embedding model lam vector size thay doi, can tao collection moi hoac recreate collection cu.
- Code Python da lay vector size tu embedding thuc te de goi `ensure_collection(vector_size)`.
- Neu collection da ton tai nhung vector size khac, service nen bao loi ro thay vi fail mo ho.

Sau nay co the them:

```text
departmentId
allowedRole
allowedUserIds
documentType
reportPeriod
keywords
pageNumber
```

## Python module da tao

```text
ai-service-python/
  app/
    api/
      vector.py
    models/
      vector_index_request.py
      vector_index_response.py
      vector_search_request.py
      vector_search_response.py
    embedding/
      __init__.py
      embedding_service.py
    vector/
      __init__.py
      qdrant_service.py
      vector_models.py
```

### Python config thuc te

Doc tu environment variables:

```text
OPENAI_API_KEY
OPENAI_EMBEDDING_MODEL=text-embedding-3-small
QDRANT_URL=http://localhost:6333
QDRANT_COLLECTION=internal_documents
```

Khong hard-code API key vao source code.

### Python endpoint thuc te

`POST /index-document`

- Input:
  - `documentId`
  - `originalFileName`
  - `accessLevel`
  - `documentStatus`
  - `chunks[]`
- Flow:
  - validate chunks khong rong.
  - lay content cua tung chunk.
  - goi OpenAI embedding.
  - lay vector size tu embedding tra ve.
  - ensure Qdrant collection.
  - upsert points vao Qdrant.
- Output:
  - `success`
  - `collectionName`
  - `indexedCount`
  - `errorMessage`

`POST /search`

- Input:
  - `query`
  - `topK`
  - `allowedAccessLevels`
- Flow:
  - validate query khong rong.
  - embed query.
  - Qdrant search voi filter `accessLevel in allowedAccessLevels`.
  - map hits ve response.
- Output:
  - `success`
  - `hits[]`
  - `errorMessage`

## ASP.NET Core module da tao

```text
backend-dotnet/
  Contracts/
    Documents/
      DocumentIndexResponse.cs
      DocumentSearchResponse.cs
      DocumentSearchResultItem.cs
    Python/
      PythonVectorIndexRequest.cs
      PythonVectorIndexResponse.cs
      PythonVectorSearchRequest.cs
      PythonVectorSearchResponse.cs

  Infrastructure/
    Python/
      PythonVectorClient.cs

  Modules/
    Documents/
      DocumentIndexingService.cs
      DocumentsController.cs
      DocumentStatus.cs
```

### C# flow thuc te

`PythonVectorClient`

- La client HTTP tu C# sang Python vector service.
- Co 2 ham chinh:
  - `IndexDocumentAsync`
  - `SearchAsync`
- Dung `JsonSerializerDefaults.Web` de serialize camelCase phu hop voi Python FastAPI.
- Neu Python tra HTTP non-success thi throw `InvalidOperationException`.

`DocumentIndexingService.IndexAsync`

- Check user authenticated.
- Chi `admin` va `employee` duoc index.
- Query document chua bi deleted.
- Admin index duoc moi access level.
- Employee index duoc document access level `employee` va `guest`.
- Chi chap nhan document status `chunked` hoac `indexed`.
- Lay `DocumentChunks` theo `DocumentId`, sap xep theo `ChunkIndex`.
- Build `PythonVectorIndexRequest`.
- Goi Python `/index-document`.
- Neu Python success:
  - set `Document.Status = indexed`.
  - audit `document_index_completed`.
- Neu Python fail:
  - set `Document.Status = failed`.
  - audit `document_index_failed`.

`DocumentIndexingService.SearchAsync`

- Check user authenticated.
- Validate query khong rong.
- Validate `topK` trong khoang 1..20.
- Build `allowedAccessLevels` theo role:
  - admin: `admin`, `employee`, `guest`
  - employee: `employee`, `guest`
  - guest: `guest`
- Goi Python `/search`.
- Map hits thanh `DocumentSearchResultItem`.
- Audit:
  - `document_vector_search_started`
  - `document_vector_search_completed`
  - `document_vector_search_failed`

## API du kien

### C# Backend

Index document da chunk:

```http
POST /api/documents/{documentId}/index
```

Search semantic:

```http
GET /api/documents/search?query=nghi%20phep&topK=5
```

### Python service

Index chunks vao Qdrant:

```http
POST /index-document
```

Search Qdrant:

```http
POST /search
```

## Qdrant local dev

Container Docker:

```text
internal-chatbot-qdrant
```

Ports:

```text
6333: REST API
6334: gRPC
```

Volume:

```text
internal_chatbot_qdrant_data_v2
```

Ghi chu:

- Neu Qdrant khong start duoc sau khi da test vector size sai, co the collection/volume dang bi loi.
- Huong xu ly dev:
  - xoa collection sai, hoac
  - dung volume moi.
- Day la van de dev/test, khong phai loi logic cua RAG.

## Flow indexing

```text
Swagger/Frontend
 -> POST /api/documents/{documentId}/index
 -> DocumentsController.Index
 -> DocumentIndexingService.IndexAsync
 -> check authenticated
 -> check role admin/employee
 -> query document theo AccessLevel
 -> document phai status chunked/indexed
 -> lay DocumentChunks tu SQL
 -> build PythonVectorIndexRequest
 -> PythonVectorClient.IndexDocumentAsync
 -> Python POST /index-document
 -> Python tao embedding cho tung chunk
 -> Python tao Qdrant collection neu chua co
 -> Python upsert points vao Qdrant
 -> C# set Document.Status = indexed
 -> audit document_index_completed
 -> return DocumentIndexResponse
```

Neu goi index lan 2:

- Python upsert lai cung point id/chunk id.
- Qdrant khong tao duplicate point.
- Day la hanh vi mong muon cho re-index co ban.

## Flow search

```text
Swagger/Frontend
 -> GET /api/documents/search?query=...
 -> DocumentsController.Search
 -> DocumentIndexingService.SearchAsync
 -> check authenticated
 -> lay role
 -> build allowed accessLevels:
      admin: admin, employee, guest
      employee: employee, guest
      guest: guest
 -> PythonVectorClient.SearchAsync
 -> Python POST /search
 -> Python tao embedding cho query
 -> Qdrant search topK voi payload filter accessLevel
 -> return hits score + payload
 -> C# return DocumentSearchResponse
```

Ket qua search hien tai la retrieval-level result, chua phai cau tra loi chatbot:

```text
query
success
count
results[]
  score
  documentId
  chunkId
  chunkIndex
  content
```

Milestone 8 moi lay `results[]` nay dua vao prompt OpenAI de sinh answer + citation.

## Ghi chu citation cho Milestone 8

- Milestone 7 search theo `chunk`, khong search theo ca document nguyen.
- Mot cau query co the tra ve nhieu chunk thuoc nhieu document khac nhau.
- Vi vay response search phai giu du:
  - `documentId`
  - `chunkId`
  - `chunkIndex`
  - `score`
  - `content`
- Sang Milestone 8, citation se duoc tao tu danh sach chunks nay.
- Khong duoc gia dinh moi cau tra loi chi co mot source duy nhat.
- Neu co 5 hits tu 3 documents, RAG response co the co 3 citations hoac 5 citations tuy cach gom source.
- Citation dep hon sau nay nen bo sung:
  - `originalFileName`
  - `pageNumber` nullable
  - `snippet`
  - `accessLevel`
  - `metadata`

## Permission du kien

Index:

```text
admin:
- index moi document chua deleted

employee:
- index document accessLevel employee/guest

guest:
- khong index

anonymous:
- khong index
```

Search:

```text
admin:
- search admin/employee/guest

employee:
- search employee/guest

guest:
- search guest

anonymous:
- khong search
```

Ly do:

- Search cung la read operation, nen theo `AccessLevel`.
- Index la write/processing operation, nen chi admin/employee.

## Error handling du kien

- Document chua chunk:
  - C# tra 400 invalid document state.
- Document khong ton tai/khong co quyen:
  - C# tra 404 de khong leak document admin-level.
- Qdrant tat:
  - document status co the set failed neu dang index.
  - return success=false voi safe message.
- OpenAI embedding loi:
  - return success=false voi safe message.
- Search query rong:
  - return 400.

## Loi va quyet dinh da gap trong luc lam

### 1. Vector size mismatch

Da gap khi dev test Qdrant bang vector size 3, sau do index OpenAI embedding 1536.

Nguyen nhan:

- Qdrant collection da tao voi vector size 3.
- OpenAI model `text-embedding-3-small` tra vector size 1536.
- Mot collection Qdrant khong the vua nhan vector size 3 vua nhan vector size 1536.

Huong xu ly:

- Recreate collection/volume trong moi truong dev.
- Trong code, lay vector size tu embedding thuc te truoc khi ensure collection.
- Sau nay neu doi embedding model, can co ke hoach migration/re-index.

### 2. Search semantic dung chu de nhung chua dung section

Da test voi PDF toan lop 7.

Ket qua:

- Chunk dau dung bai va dung phan `TOM TAT LY THUYET`.
- Cac chunk tiep theo co the bi keo sang phan bai tap vi cung co nhieu tu khoa giong query: goc, tia phan giac, goc ke bu.

Ket luan:

- Day la gioi han cua semantic search baseline.
- Chua phai bug nghiem trong.
- Qdrant hien chi biet chunk nao gan nghia voi query, chua hieu cau truc section cua tai lieu.

Huong cai tien sau:

- Them metadata section khi chunk:
  - `lessonTitle`
  - `sectionTitle`
  - `pageNumber`
- Neu query co "tom tat ly thuyet", uu tien chunk co section/title tuong ung.
- Milestone 8 co the them rerank nhe:
  - cong diem chunk chua `TOM TAT LY THUYET`.
  - tru diem chunk chua `BAI TAP`, `TU LUYEN`, `HUONG DAN GIAI`.

### 3. Ky hieu toan hoc trong PDF co the bi loi

Tieng Viet trong response Swagger van dung.

Mot so ky hieu toan hoc/goc/do co the bi loi do PDF font/symbol encoding.

Ket luan:

- Khong phai loi UTF-8 toan cuc.
- La gioi han cua PDF text layer extraction.
- OCR sau nay co the giup mot so file scan, nhung OCR cong thuc/toan hoc van la bai kho rieng.

## Test cases da pass / can pass

1. Qdrant health:
   - Qdrant local chay duoc.

2. Index document:
   - upload -> ingest -> chunk -> index.
   - document status thanh `indexed`.
   - Qdrant co points.

3. Search semantic:
   - search keyword gan noi dung chunk.
   - ket qua tra chunk lien quan.
   - co score.
   - co documentId/chunkId/chunkIndex.

4. Re-index:
   - goi index lan 2.
   - Qdrant khong bi duplicate points.

5. Permission:
   - employee search khong thay admin-level document.
   - guest chi search thay guest-level document.
   - anonymous khong search.

6. Failure:
   - Qdrant tat -> API khong crash.
   - query rong -> 400.

## Dau hieu hoan thanh

- Qdrant co collection `internal_documents`.
- Document `chunked` co the index thanh Qdrant.
- Document status cap nhat thanh `indexed`.
- Search semantic hoat dong.
- Search result co:
  - score
  - documentId
  - chunkId
  - chunkIndex
  - content
- Permission search dung theo `AccessLevel`.
- San sang qua Milestone 8 de dua search result vao OpenAI prompt va tra citation.

## Tong ket hoan thanh

- Embedding model hien tai: `text-embedding-3-small`.
- Vector size thuc te: `1536`.
- Qdrant URL local: `http://localhost:6333`.
- Collection: `internal_documents`.
- Search da filter theo `accessLevel`.
- C# backend van la noi check auth/permission.
- Python service chi lam AI/vector work.
- Milestone 7 da san sang lam nen tang cho Milestone 8 RAG.

## Viec de danh cho cac milestone sau

- Milestone 8:
  - RAG answer bang OpenAI.
  - Citation tu nhieu chunks/documents.
  - PromptBuilder.
  - Rerank nhe neu can.
- Milestone 14:
  - Permission nang cao theo department/user.
  - Qdrant payload filter phuc tap hon.
- Milestone 15:
  - Indexing background job.
  - Retry khi Python/Qdrant/OpenAI loi.
- Milestone 16:
  - Cost tracking embedding.
  - Rate limit search/index.
  - Prompt injection guard cho RAG.
