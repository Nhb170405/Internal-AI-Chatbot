# ADR-001: Document Soft Delete, Restore, And Retention Purge

Trang thai: Accepted

Ngay tao: 2026-06-18

Quyet dinh:
- DELETE document trong API se la soft delete, khong xoa file vat ly ngay.
- Document bi xoa se co Status = Deleted.
- Luu DeletedAt va DeletedByUserId de audit va restore.
- Document Status = Deleted bi an khoi list/get/search/RAG.
- Cho phep restore trong thoi gian retention neu file vat ly chua bi purge.
- File vat ly se duoc hard delete boi background job sau retention period.
- Retention mac dinh de xuat: 30 ngay.

Ly do:
- Tranh mat du lieu ngay khi admin/user xoa nham.
- Giu audit trail cho he thong noi bo.
- Tranh tinh trang SQL, file storage, chunks, Qdrant vectors bi lech nhau neu hard delete ngay.
- Cho phep background job xu ly purge an toan hon request HTTP.
- Sau nay co the purge chunk/vector cung documentId trong cung workflow.

Tac dong toi Milestone 4:
- Document can them Status = Deleted, DeletedAt, DeletedByUserId.
- DocumentService.DeleteAsync se set soft delete fields.
- DocumentService.RestoreAsync se clear soft delete fields va dua status ve Uploaded trong giai doan dau.
- List/GetById phai exclude Deleted.
- DocumentsController can endpoint restore.

Tac dong toi Milestone 7/8:
- Qdrant payload/search phai bo qua document deleted.
- RAG khong duoc dua chunk cua document deleted vao context.

Tac dong toi Milestone 15:
- Can DeletedDocumentPurgeJob.
- Job purge file vat ly sau retention.
- Job purge se xoa Qdrant vectors theo `documentId` truoc khi xoa file vat ly va SQL rows.
- Job purge se xoa cac du lieu phu thuoc trong SQL: job logs, processing jobs, table profiles, chunks, extractions, metadata.
- Neu Qdrant/file storage loi thi giu SQL row de lan sau retry duoc, tranh mat dau vet document.
- Audit log purge la best-effort: neu purge da thanh cong ma audit loi thi chi ghi warning, khong lam job fail gia.

Tac dong toi Milestone 16:
- Retention policy can cau hinh ro rang.
- Delete/restore/purge can audit log.
- Can test abuse: user khong restore/xoa vinh vien document vuot qua access scope.

Quyet dinh bo sung ve Recycle Bin / Deleted Documents:

- Nen co trang rieng kieu Recycle Bin / Deleted Documents thay vi tron vao list document binh thuong.
- Trang nay chi hien document `Status = Deleted`.
- `admin` co the xem, restore va xoa vinh vien tat ca document da delete.
- `employee` co the xem, restore va xoa vinh vien document trong pham vi access level employee tro xuong:
  - employee-level document
  - guest-level document
- `employee` khong duoc thao tac voi admin-level document da delete.
- `guest` khong co quyen restore/xoa vinh vien.
- `anonymous` khong co quyen restore/xoa vinh vien.
- Xoa vinh vien phai duoc audit log rieng, vi day la thao tac pha huy du lieu that.
- Frontend nen dat ten ro rang: Restore, Delete permanently, Deleted at, Deleted by, Remaining retention days.
- Neu co background purge job, nut Delete permanently la manual purge, con job la scheduled purge.

Cau hoi de xu ly sau:
- Khi restore, status nen ve Uploaded hay status truoc khi delete?
- Co can field PreviousStatus truoc khi delete khong?
- Sau purge co giu metadata document trong SQL khong?
- Co can them `PurgedAt`, `PurgedByUserId`, `PurgeReason` de audit khong?
- Co can API manual purge cho admin/employee theo scope khong, ngoai scheduled purge job?
