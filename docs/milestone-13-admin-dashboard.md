# Milestone 13: Frontend Foundation And Role-Based UI

Trang thai: MVP Completed

Ngay cap nhat: 2026-07-03

## Tong ket MVP da lam

Milestone 13 da tao duoc frontend app dau tien va admin area co ban cho Factory Chatbot V2.

Nhung phan da co:

- Frontend React/Vite/TypeScript trong `frontend`.
- Login page cho guest/employee/admin.
- Role-based layout: sidebar, topbar, content area.
- Chat page goi `/api/assistant/chat`.
- Assistant backend module lam cua vao chat duy nhat va route co ban:
  - chitchat
  - rag
  - dataset_profile
  - dataset_analyze/chart o muc co ban
- Document page:
  - list/upload/filter/search.
  - upload xong tu dong chay pipeline co ban: ingest -> chunk -> index.
  - file bang `.csv/.xlsx/.xls` tu dong profile sau upload.
  - hien status `indexed` va badge `profiled` neu co table profile.
  - delete dang la soft delete.
- Dataset page:
  - chon file bang.
  - chay profile.
  - chay analyze bang pandas.
- Chart page:
  - tao chart tu dataset analysis.
  - backend expose chart image qua endpoint an toan hon local path.
  - frontend hien anh chart bang chartUrl.
- Admin area co ban:
  - Overview
  - Users
  - Usage
  - Audit Logs
- UI da polish o muc MVP:
  - tong mau sang.
  - action button gon hon.
  - khung chat/citation co scroll rieng.
  - bo top search khong can thiet.
  - enter gui chat, shift+enter xuong dong.

Nhung phan da co nhung chua production-perfect:

- Chat sessions tren frontend moi luu localStorage, chua dong bo backend.
- Admin metrics con la dashboard van hanh co ban, chua phai monitoring chuan production.
- User active/inactive la trang thai account, khong phai online/offline.
- Restore/permanent delete chua co Recycle Bin UI.
- Delete document tren UI da dung duoc nhung chua co man hinh xem deleted documents.
- Token usage chua duoc gom day du cho moi route assistant/tool.

Quyet dinh:

- Chap nhan Milestone 13 la MVP completed.
- Cac phan restore bin, chat session backend, guest rate limit, dashboard metric chuan, background pipeline se dua sang milestone sau.
- Khong tiep tuc nhoi them vao Milestone 13 de tranh lam frontend/backend roi va kho debug.

## Quyet dinh da chot

Milestone 13 khong chi la Admin Dashboard. Day la milestone dung frontend app dau tien cho he thong Factory Chatbot V2.

Huong chot:

- Man hinh dau tien la Login.
- Sau login, giao dien thay doi theo role: guest, employee, admin.
- Chat la man hinh trung tam cua san pham.
- Chat UI chi co mot o nhap duy nhat. User khong phai chon Basic/RAG/Dataset/Chart bang tay.
- Backend se co `Assistant` module lam router de quyet dinh dung tool nao.
- Admin Dashboard la mot khu vuc rieng chi admin thay duoc.
- Frontend uu tien clean, toi gian, sang, de dung lau, khong lam landing page.

## Muc tieu

- Tao frontend app trong thu muc `frontend`.
- Co login page va role-based app layout.
- Co chat UI dung duoc cho guest, employee, admin.
- Co Assistant endpoint lam cua vao chat duy nhat: frontend goi mot API, backend tu routing.
- Co documents UI de xem/upload/xu ly tai lieu theo role.
- Co dataset/chart UI co ban de goi cac API da lam o milestone 11-12.
- Co admin area de admin quan sat he thong.

## Ly do lam milestone nay

- Backend da co nhieu API: auth, chat, documents, ingest, chunk, index, RAG, OCR, metadata, dataset analysis, chart.
- Neu chi test bang Swagger thi kho thay san pham that.
- Can mot UI thong nhat de user dang nhap, chat, xem citation, upload document, xem ket qua dataset/chart.
- Admin can noi quan sat he thong ma khong vao SQL Server thu cong.
- Neu ChatPage goi truc tiep RAG cho moi message, nhung cau nhu `hello` van ton nhieu token vi bi dua context vao prompt.
- Neu bat user chon Basic/RAG/Dataset bang tay thi UX kem va sau nay nang cap router se phai doi lai giao dien.

## Assistant Module

Milestone 13 chot them backend module moi:

```text
backend-dotnet/Modules/Assistant
```

Vai tro:

- Lam mot cua vao duy nhat cho chat frontend.
- Nhan cau hoi tu user.
- Kiem tra auth/role.
- Phan loai intent cua cau hoi.
- Goi dung service da co: ChatService, RagService, DocumentMetadataService, DatasetProfileService, DatasetAnalysisService, ChartService.
- Tra ve mot response thong nhat de frontend render.

Frontend khong nen tu quyet dinh logic nghiep vu sau:

- Co document nao ton tai?
- User co quyen doc document do khong?
- Nen dung RAG hay dataset analysis?
- Dataset profile da co chua?
- Chart co tao duoc khong?

Nhung viec nay thuoc backend.

### Assistant Flow

```text
User
  -> Frontend ChatPage
  -> POST /api/assistant/chat
  -> AssistantController
  -> AssistantService
  -> AssistantRouter
       |-- chitchat/basic
       |     -> ChatService / OpenAI basic chat
       |
       |-- rag
       |     -> RagService
       |
       |-- document_metadata
       |     -> DocumentMetadataService / DocumentMetadataRoutingService
       |
       |-- dataset_profile
       |     -> DatasetProfileService
       |
       |-- dataset_analyze
       |     -> DatasetAnalysisService
       |
       |-- chart
             -> ChartService
  -> AssistantChatResponse
  -> Frontend render answer/citations/data/chart/suggestions
```

### Route Types

```text
chitchat
rag
document_metadata
dataset_profile
dataset_analyze
chart
unsupported
```

Route examples:

```text
"hello"
-> chitchat

"quy dinh nghi phep cua cong ty la gi?"
-> rag

"Real_Estate.xlsx co nhung cot nao?"
-> dataset_profile hoac document_metadata

"top 10 giao dich gia tri lon nhat"
-> dataset_analyze

"ve bieu do doanh thu theo thang"
-> chart
```

### Routing Strategy

Khong lam LLM router phuc tap ngay tu dau.

Chot huong hybrid theo tung cap:

1. Rule-based router truoc.
2. Neu cau hoi ro rang thi khong goi LLM classifier.
3. Neu cau hoi mo ho thi tam thoi tra ve `unsupported` hoac `needs_clarification`.
4. Sau nay moi them LLM classifier hoac semantic router.

Ly do:

- Giam token cost.
- De debug cho nguoi moi.
- Khong phinh scope cua Milestone 13.
- Van giu duoc kien truc de nang cap.

### Vi sao khong de frontend routing?

Frontend chi nen la UI.

Routing can biet:

- Current user role.
- Access level cua document.
- Document metadata trong SQL.
- Dataset profiles.
- Tool nao backend dang co.
- Log audit va token usage.

Neu lam routing o frontend, logic se bi loang, kho bao mat, kho debug.

## Role-Based UI

```text
Login Page
   |
   |-- Guest Login
   |      -> Guest Workspace
   |          -> Chat
   |          -> Guest Documents
   |          -> Dataset/Profile/Analyze/Chart voi file guest neu can
   |
   |-- Employee Login
   |      -> Employee Workspace
   |          -> Chat
   |          -> Documents
   |          -> RAG Search
   |          -> Dataset Analysis
   |          -> Charts
   |
   |-- Admin Login
          -> Admin Workspace
              -> Chat
              -> Documents
              -> RAG Search
              -> Dataset Analysis
              -> Charts
              -> Admin Dashboard
              -> Users
              -> Audit Logs
              -> Usage
```

## Phan quyen UI

Guest:

- Dang nhap bang guest login.
- Chat duoc neu backend cho phep.
- Xem document access level `guest`.
- Profile/analyze/chart voi file `guest`.
- Khong upload.
- Khong delete/restore.
- Khong vao admin area.

Employee:

- Dang nhap bang email/password.
- Chat/RAG.
- Xem document `employee` va `guest`.
- Upload document level `employee` hoac `guest`.
- Ingest/chunk/index/profile/analyze/chart voi document duoc phep.
- Khong vao admin area.

Admin:

- Dang nhap bang email/password.
- Co tat ca chuc nang employee.
- Xem document moi access level.
- Upload moi access level.
- Vao admin area.
- Xem users, audit logs, usage, document status.
- Thuc hien cac thao tac van hanh neu backend da co API.

Anonymous:

- Chi thay login page.
- Khong chat.
- Khong xem document.
- Khong profile/analyze/chart.

## Yeu cau UI/UX

- Style toi gian nhung tinh te.
- Tong mau sang, de chiu khi dung lau.
- Bo cuc ro rang, thuan tien.
- Khong lam hero/landing page.
- Uu tien tool noi bo: nhieu thong tin nhung phai de scan.
- Khong dung mau qua choi hoac gradient nang.

Design direction:

```text
Background: #F7F8FA / #F8FAFC
Surface:    #FFFFFF
Border:     #E5E7EB
Text main:  #111827
Text muted: #6B7280
Primary:    blue/teal diu
Success:    green nhe
Warning:    amber nhe
Danger:     red nhe
Radius:     6-8px
Shadow:     rat nhe hoac khong can
```

## Layout tong the

```text
frontend
└── App
    ├── Login Page
    └── Authenticated App Layout
        ├── Sidebar theo role
        ├── Topbar
        │   ├── Search
        │   ├── Current user
        │   └── Logout
        └── Main Content
            ├── Chat Page
            ├── Documents Page
            ├── Document Detail Page
            ├── Dataset Page
            ├── Charts Page
            └── Admin Pages neu role = admin
```

## Chat UI

Chat la man hinh chinh.

```text
Chat Page
├── Left panel: chat sessions/history
├── Center: message thread
├── Bottom: input box
└── Right panel optional: citations/sources/document context
```

Guest chat:

- UI gon nhat.
- It nut.
- Chi hien chat thread, input, citation neu co.

Employee/admin chat:

- Co chat sessions.
- Co citation/source panel.
- Co token usage co ban neu API tra ve.
- Khong can nut chon Basic/RAG/Dataset bang tay trong version chot.
- ChatPage goi `/api/assistant/chat`.
- Backend tra ve route da dung de UI co the hien thi nho neu can.
- Co link mo document/chunk lien quan.

## Admin Area

Admin area khong thay the chat UI. No la khu vuc rieng trong sidebar cua admin.

Pages du kien:

- Overview
- Documents
- Users
- Usage
- Audit Logs

Overview:

- Tong so documents.
- So document theo status: uploaded, extracted, chunked, indexed, failed.
- Token usage tong quan.
- Request gan day.
- Loi gan day.

Documents:

- Table documents.
- Filter theo status, access level, extension, date.
- Xem metadata.
- Xem extraction/chunk/index status.
- Re-ingest/re-chunk/re-index neu backend co API.

Users:

- List users.
- Role.
- IsActive.
- Usage theo user neu co.

Usage:

- Token usage theo user/date/model.
- Ban dau chi can table va summary.

Audit Logs:

- List audit actions.
- Filter action/user/resource/time.
- Khong hien metadata nhay cam.

## Frontend module du kien

Ten file co the thay doi tuy framework, nhung nen giu tu duy module:

```text
frontend/src/
├── app/
│   ├── App.tsx
│   ├── router.tsx
│   └── providers/
├── api/
│   ├── httpClient.ts
│   ├── authApi.ts
│   ├── chatApi.ts
│   ├── documentsApi.ts
│   ├── datasetsApi.ts
│   ├── chartsApi.ts
│   └── adminApi.ts
├── components/
│   ├── layout/
│   ├── ui/
│   └── feedback/
├── features/
│   ├── auth/
│   ├── chat/
│   ├── documents/
│   ├── datasets/
│   ├── charts/
│   └── admin/
├── styles/
│   └── globals.css
└── types/
```

## Backend API can dung lai

Auth:

```http
POST /api/auth/guest-login
POST /api/auth/login
POST /api/auth/logout
GET  /api/auth/me
```

Chat:

```http
POST /api/chat/message
GET  /api/chat/sessions
POST /api/chat/sessions
GET  /api/chat/sessions/{sessionId}/messages
POST /api/chat/sessions/{sessionId}/messages
POST /api/rag/chat
```

Assistant:

```http
POST /api/assistant/chat
```

Request du kien:

```json
{
  "message": "Real_Estate.xlsx co nhung cot nao?",
  "topK": 3
}
```

Response du kien:

```json
{
  "route": "dataset_profile",
  "answer": "File nay co cac cot: ...",
  "model": null,
  "promptTokens": null,
  "completionTokens": null,
  "totalTokens": null,
  "citations": [],
  "data": null,
  "chartPath": null,
  "warnings": [],
  "needsUserAction": false,
  "suggestedAction": null
}
```

Documents:

```http
GET    /api/documents
POST   /api/documents/upload
GET    /api/documents/{documentId}
DELETE /api/documents/{documentId}
POST   /api/documents/{documentId}/restore
POST   /api/documents/{documentId}/ingest
POST   /api/documents/{documentId}/chunk
POST   /api/documents/{documentId}/index
GET    /api/documents/search
```

Deleted documents / Recycle Bin nen bo sung sau:

```http
GET    /api/admin/deleted-documents
POST   /api/admin/deleted-documents/{documentId}/restore
DELETE /api/admin/deleted-documents/{documentId}/purge
```

Quyen du kien:

- `admin`: xem, restore, xoa vinh vien tat ca deleted documents.
- `employee`: xem, restore, xoa vinh vien deleted documents co access level `employee` hoac `guest`.
- `employee`: khong thao tac deleted documents cap `admin`.
- `guest`: khong co Recycle Bin.
- `anonymous`: khong co Recycle Bin.

Ly do tach Recycle Bin thanh page rieng:

- List document binh thuong khong bi roi voi file da xoa.
- Admin/employee co the thay ro file nao da xoa, ai xoa, xoa luc nao, con bao nhieu ngay truoc khi purge.
- Xoa vinh vien la thao tac nguy hiem nen can UI rieng va confirm ro rang.

Metadata:

```http
GET /api/documents/{documentId}/metadata
PUT /api/documents/{documentId}/metadata
```

Datasets/charts:

```http
POST /api/documents/{documentId}/dataset/profile
GET  /api/documents/{documentId}/dataset/profile
POST /api/documents/{documentId}/dataset/analyze
POST /api/documents/{documentId}/dataset/chart
```

Admin API neu thieu thi bo sung sau:

```http
GET /api/admin/overview
GET /api/admin/documents
GET /api/admin/users
GET /api/admin/usage
GET /api/admin/audit-logs
```

## Thu tu thuc hien

1. Chon frontend stack.
2. Tao frontend project trong `frontend`.
3. Tao design tokens co ban: color, spacing, border, radius.
4. Tao auth API client.
5. Tao Login page.
6. Tao app layout: sidebar, topbar, content.
7. Tao role-based navigation.
8. Tao Assistant backend module.
9. Tao Chat page goi `/api/assistant/chat`.
10. Tao Documents page.
11. Noi upload/list/detail/metadata.
12. Tao Dataset/Chart page.
13. Tao Admin overview/documents/users/audit/usage page.
14. Test role va permission.
15. Polish UI.

## Test cases

- Anonymous chi thay login page.
- Guest login xong thay guest workspace.
- Employee login xong thay employee workspace.
- Admin login xong thay admin workspace va admin pages.
- Guest khong thay upload/admin menu.
- Employee khong thay admin menu.
- Admin thay admin menu.
- Logout quay ve login page.
- Refresh page van goi `/api/auth/me` de khoi phuc user hien tai.
- Chat page gui message qua `/api/assistant/chat` duoc.
- Cau `hello` khong goi RAG va khong ton context token lon.
- Cau hoi tai lieu noi bo route sang RAG va hien citation neu co.
- Cau hoi cot/sheet cua file route sang metadata/profile neu co du lieu.
- Documents page filter/list duoc.
- Dataset analyze/chart goi API duoc.

## Dau hieu hoan thanh

- Co frontend app chay duoc.
- User co the login/logout.
- UI thay doi theo role.
- Chat UI dung duoc.
- Assistant module route duoc cac cau hoi co ban: chitchat, rag, dataset_profile.
- Documents UI dung duoc o muc co ban.
- Admin co trang quan sat he thong co ban.
- Giao dien clean, sang, de dung, khong roi.

## Chua nen lam o milestone nay

- UI animation phuc tap.
- Dashboard realtime.
- Multi-tenant.
- Advanced BI builder.
- Drag-and-drop layout.
- Websocket chat streaming neu backend chua san sang.
- State management qua phuc tap neu React state/context da du.

## Ghi chu de mo rong sau

- Neu sau nay lam multi-file analysis, frontend nen co page rieng cho report builder.
- Neu sau nay lam table renderer, co the them `features/tables`.
- Neu sau nay lam advanced document permission, sidebar/page action phai doc permission tu backend thay vi chi dua vao role.
- Chart file hien tai tra ve local path. Khi lam frontend that, can co endpoint download/view chart an toan thay vi hien raw local path.
- Them Recycle Bin / Deleted Documents page cho restore va delete permanently.
- Them quan ly nhieu chat sessions bang backend thay vi chi localStorage o frontend.
