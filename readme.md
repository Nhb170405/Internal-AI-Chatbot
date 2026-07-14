# Internal AI Chatbot
An end-to-end enterprise-style AI assistant with ASP.NET Core, React, FastAPI, SQL Server, RAG, background jobs, and Azure deployment.

Live demo: https://purple-coast-0345b9800.7.azurestaticapps.net/login  
Repository: https://github.com/Nhb170405/Internal-AI-Chatbot

Demo account:
- Admin: `admin@company.com`
- Password: `Admin@123`

> Note: Demo credentials are intentionally public for reviewer/interviewer access. Do not use these credentials in production.

## 1. Project Overview

Internal AI Chatbot is an end-to-end AI assistant system designed for internal company use.  
The system allows employees and admins to upload documents, process files, extract text, chunk content, index documents into a vector database, and ask questions through a chatbot interface.

The project combines:

- Full-stack web development
- Authentication and role-based authorization
- Document processing pipeline
- RAG architecture
- OpenAI integration
- Python-based file analysis service
- SQL Server persistence
- Vector search
- Background job processing
- Azure cloud deployment

## 2. Key Features

### Authentication & Authorization

- Cookie-based authentication
- Role-based access control:
  - Guest
  - Employee
  - Admin
- Admin-only user management endpoint
- Secure password hashing using ASP.NET Core `IPasswordHasher`
- Guest session support
- Audit logging for login, logout, document actions, and chat usage

### AI Chat

- Chat interface connected to backend API
- OpenAI Chat Completions integration
- Token usage tracking
- Chat history by session
- Rate limiting for chat requests
- Safe audit logging without storing full user prompts in audit metadata

### Document Management

- Upload PDF, DOCX, XLSX, CSV, and TXT files
- File size validation
- Extension whitelist
- Content-Type validation
- Safe server-side file naming using document ID
- Soft delete and restore support
- Access level control for Guest, Employee, and Admin documents

### RAG Pipeline

- Document ingestion
- Text extraction
- Chunking
- Vector indexing
- Semantic document search
- Retrieval-based Q&A

### Python AI Service

- FastAPI microservice for document processing
- OCR support
- Dataset/table processing
- Chart generation
- Vector operations
- Called only by the ASP.NET Core backend

### Admin Features

- List users
- Create employee accounts
- Role-protected admin APIs
- Audit log support
- Dashboard foundation for system monitoring

### Deployment

- Frontend deployed to Azure Static Web Apps
- Backend deployed to Azure Container Apps
- Python service deployed as a separate container service
- SQL Server used as relational database
- Environment-based configuration

## 2. Key Features

### Authentication & Authorization

- Cookie-based authentication
- Role-based access control:
  - Guest
  - Employee
  - Admin
- Admin-only user management endpoint
- Secure password hashing using ASP.NET Core `IPasswordHasher`
- Guest session support
- Audit logging for login, logout, document actions, and chat usage

### AI Chat

- Chat interface connected to backend API
- OpenAI Chat Completions integration
- Token usage tracking
- Chat history by session
- Rate limiting for chat requests
- Safe audit logging without storing full user prompts in audit metadata

### Document Management

- Upload PDF, DOCX, XLSX, CSV, and TXT files
- File size validation
- Extension whitelist
- Content-Type validation
- Safe server-side file naming using document ID
- Soft delete and restore support
- Access level control for Guest, Employee, and Admin documents

### RAG Pipeline

- Document ingestion
- Text extraction
- Chunking
- Vector indexing
- Semantic document search
- Retrieval-based Q&A

### Python AI Service

- FastAPI microservice for document processing
- OCR support
- Dataset/table processing
- Chart generation
- Vector operations
- Called only by the ASP.NET Core backend

### Admin Features

- List users
- Create employee accounts
- Role-protected admin APIs
- Audit log support
- Dashboard foundation for system monitoring

### Deployment

- Frontend deployed to Azure Static Web Apps
- Backend deployed to Azure Container Apps
- Python service deployed as a separate container service
- SQL Server used as relational database
- Environment-based configuration

## 4. Architecture

The system is designed as a multi-service architecture.

## 5. Security Design

Security was considered from the beginning of the project.

### Implemented

- Cookie authentication with `HttpOnly`
- Secure cookie policy in production
- Explicit CORS allowed origins
- Role-based authorization
- Admin-only user management APIs
- Password hashing with `IPasswordHasher`
- Rate limiting for login, chat, and upload APIs
- Upload validation:
  - File size limit
  - Extension whitelist
  - Content-Type check
  - Safe server-side file names
- Security headers:
  - `X-Content-Type-Options`
  - `X-Frame-Options`
  - `Referrer-Policy`
  - `Permissions-Policy`
- Global exception middleware to avoid leaking stack traces to clients
- Audit logging for sensitive actions
- Python service is designed to be called only by the backend

### Known Limitations

- Demo credentials are public for reviewer access.
- CSRF protection should be strengthened before production use.
- More fine-grained department-level permissions can be added.
- File content validation can be improved with magic-byte checks.
- Production deployment should use private/internal ingress for Python service

## 6. Authentication & Roles

The system supports three user roles:

| Role | Description | Permissions |
|---|---|---|
| Guest | Temporary user session | Can access guest-level documents and chat |
| Employee | Internal company user | Can access employee-level and guest-level documents |
| Admin | System administrator | Can manage users, documents, and system data |

Authentication is implemented using ASP.NET Core cookie authentication.

The backend does not trust role values from the frontend. Roles are loaded from backend user data and stored as claims after successful login.

## 7. Document Processing Pipeline

The document pipeline is one of the core parts of the system.

flowchart TD
    A[Upload File] --> B[Validate File]
    B --> C[Save File]
    C --> D[Create Document Record]
    D --> E[Enqueue Background Job]
    E --> F[Python Ingestion Service]
    F --> G[Extract Text / OCR / Table Data]
    G --> H[Save Extraction Result]
    H --> I[Chunk Document]
    I --> J[Index into Vector DB]
    J --> K[Ready for RAG Search]

## 8. RAG and AI Workflow

The chatbot does not simply send every user question directly to the model.

Depending on the request type, the backend can:

- Answer simple chat messages directly
- Search internal documents
- Retrieve relevant chunks
- Analyze spreadsheet/table data
- Generate charts
- Return deterministic results without using LLM tokens when possible

This hybrid approach reduces token usage and improves answer reliability.

## 9. Backend Modules

The backend is organized by modules:

backend-dotnet/
├── Modules/
│   ├── Auth/
│   ├── Users/
│   ├── Sessions/
│   ├── Chat/
│   ├── Documents/
│   ├── Rag/
│   ├── Datasets/
│   ├── Charts/
│   ├── Admin/
│   └── BackgroundJobs/
├── Infrastructure/
│   ├── Persistence/
│   ├── OpenAI/
│   ├── Python/
│   ├── Storage/
│   ├── Security/
│   └── Errors/
└── Contracts/

## 10. Frontend Overview

The frontend is built with React, Vite, and TypeScript.

Main frontend responsibilities:

- Login page
- Guest login
- Chat UI
- Document upload UI
- API client abstraction
- Role-aware navigation
- Error handling

The frontend uses `VITE_API_BASE_URL` to connect to the deployed backend API.

## 11. Python Service Overview

The Python service is responsible for AI/data-heavy tasks that are better handled outside the C# backend.

Responsibilities:

- OCR
- Document ingestion
- Text extraction
- Chunking support
- Vector operations
- Dataset analysis
- Chart generation

The Python service is not intended to be called directly by the frontend.  
It is used as an internal service behind the ASP.NET Core backend.

## 12. Database Design

The system uses SQL Server for structured data.

Main data groups:

- Users
- Guest sessions
- Chat sessions
- Chat messages
- Documents
- Document extractions
- Document chunks
- Dataset metadata
- Audit logs
- Background job metadata

SQL Server is used for durable application state, while vector search is handled separately by the vector database.


## 13. Deployment

The project is deployed on Azure.

| Component | Deployment |
|---|---|
| Frontend | Azure Static Web Apps |
| Backend | Azure Container Apps |
| Python AI Service | Azure Container Apps |
| Database | SQL Server / Azure SQL-ready |
| File storage | Local storage for development, Azure Blob-ready |
| Background jobs | Hangfire |

### Production configuration

Runtime secrets and environment-specific values are configured through environment variables, not hard-coded in source code.

Examples:
- `OpenAI__ApiKey`
- `ConnectionStrings__DefaultConnection`
- `PythonService__BaseUrl`
- `Cors__AllowedOrigins__0`
- `VITE_API_BASE_URL`

## 14. What I Learned

Through this project, I practiced:

- Designing a real multi-service architecture
- Building ASP.NET Core APIs
- Implementing cookie authentication
- Designing role-based authorization
- Working with SQL Server and EF Core
- Building a Python FastAPI service
- Integrating OpenAI API
- Designing a RAG pipeline
- Handling file upload securely
- Using background jobs for long-running tasks
- Deploying full-stack services to Azure
- Debugging CORS, cookies, environment variables, and cloud deployment issues


## 18. Roadmap

Planned improvements:

- Add CSRF protection for cookie-based authentication
- Add department-level permissions
- Add invite-based employee account creation
- Improve document chunking quality
- Add magic-byte file validation
- Add admin dashboard charts
- Add streaming AI responses
- Add more advanced tool-calling workflow
- Add Azure Blob Storage as production file storage
- Add CI/CD for backend and Python services
- Add automated tests


## 18. Roadmap

Planned improvements:

- Add CSRF protection for cookie-based authentication
- Add department-level permissions
- Add invite-based employee account creation
- Improve document chunking quality
- Add magic-byte file validation
- Add admin dashboard charts
- Add streaming AI responses
- Add more advanced tool-calling workflow
- Add Azure Blob Storage as production file storage
- Add CI/CD for backend and Python services
- Add automated tests


## 19. Screenshots

Add screenshots here:
- Login page
- Chat interface
- Document upload
- Admin user management
- Azure deployment
- Example RAG answer
- Example table/chart analysis


## 20. Repository Structure

```text
Internal-AI-Chatbot/
├── frontend/              # React + Vite frontend
├── backend-dotnet/        # ASP.NET Core Web API
├── ai-service-python/     # Python FastAPI AI service
├── docs/                  # Development milestones and design notes
├── docker-compose.yml     # Local multi-service setup
└── README.md
```

# Thứ tự README mình khuyên dùng

Để README nhìn chuyên nghiệp, bạn nên đặt thứ tự như này:

```text
1. Title + short description + live demo
2. Project overview
3. Key features
4. Architecture diagram
5. Tech stack
6. Security design
7. Document/RAG pipeline
8. Deployment
9. Local setup
10. Demo guide
11. Screenshots
12. What I learned
13. Roadmap
14. Known limitations
```
