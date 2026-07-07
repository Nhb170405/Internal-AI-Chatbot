# Factory Chatbot V2 Roadmap

Ngay cap nhat: 2026-07-01

Roadmap hien tai gom 17 milestone.

## Completed

1. Project Foundation
2. Basic Chatbot Without RAG
3. Chat History And Token Usage
4. Document Upload And Metadata
5. Python FastAPI Ingestion
6. Chunking
7. Embedding And Qdrant
8. RAG Chat With Citation
9. OCR

## Planned

10. Document Metadata Routing
11. Dataset Profiling And Analysis
12. Chart Generation
13. Frontend Foundation And Role-Based UI
14. Advanced Document Permission
15. Background Jobs
16. Security And Cost Hardening
17. Deployment

## Ly do doi roadmap sau Milestone 9

Roadmap ban dau di tu OCR sang Admin Dashboard. Sau khi lam RAG va ban ve CSV/XLSX, minh chot them 3 milestone truoc Admin Dashboard:

- Metadata routing.
- Dataset profiling/analysis.
- Chart generation.

Ly do:

- He thong hien tai search chu yeu bang Qdrant chunks va filter accessLevel.
- Chua co metadata-first retrieval de tim dung file truoc khi search.
- CSV/XLSX khong nen chi dua vao RAG text neu cau hoi can tinh toan.
- Admin Dashboard se huu ich hon neu da co metadata/profile/chart result de quan sat.

## Huong hybrid cho tai lieu va du lieu bang

```text
Document RAG:
PDF/DOCX/TXT/OCR text
 -> extract
 -> chunk
 -> embedding/Qdrant
 -> RAG answer + citation

Dataset analysis:
CSV/XLSX
 -> metadata routing
 -> pandas profiling
 -> pandas/SQL analysis
 -> chart/result
 -> OpenAI dien giai ket qua neu can
```

## Huong dataset analysis hien tai

Chot quay ve huong don gian:

```text
User/API caller chon document va truyen request co cau truc
 -> operation/valueColumn/groupByColumn/sheetName ro rang
 -> pandas tinh toan
 -> tra JSON result
```

Chua lam natural-language planner, guided form, hay multi-file chart trong Milestone 11.
Nhung phan nay de backlog sau khi analysis engine on dinh.

## Thu tu logic sau Milestone 9

```text
Milestone 10: Tim dung file/dataset bang metadata truoc.
Milestone 11: Hieu file CSV/XLSX co sheet/cot/sample nao va tinh toan bang pandas.
Milestone 12: Tao chart tu ket qua phan tich.
Milestone 13: Tao frontend app dau tien: login, role-based UI, chat workspace, documents UI, dataset/chart UI, va admin area co ban.
```
