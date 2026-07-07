export type DocumentStatus =
  | "uploaded"
  | "processing"
  | "extracted"
  | "chunked"
  | "indexed"
  | "failed"
  | "deleted";

export type DocumentAccessLevel = "guest" | "employee" | "admin";

export type DocumentListItem = {
  id: string;
  originalFileName: string;
  extension: string;
  sizeBytes: number;
  status: DocumentStatus;
  accessLevel: DocumentAccessLevel;
  hasTableProfile?: boolean;
  processingJobId?: string | null;
  processingJobStatus?: string | null;
  createdAt?: string;
  uploadedAt?: string;
  updatedAt?: string;
  errorMessage?: string | null;
};

export type DocumentMetadata = {
  documentId: string;
  originalFileName: string;
  extension: string;
  contentType: string;
  sizeBytes: number;
  accessLevel: DocumentAccessLevel;
  title: string | null;
  description: string | null;
  reportType: string | null;
  reportDate: string | null;
  reportMonth: number | null;
  reportYear: number | null;
  department: string | null;
  sourceSystem: string | null;
  language: string;
  keywords: string[];
  tags: string[];
  detectedColumns: string[];
  sheetNames: string[];
  metadataCreatedAt?: string | null;
  metadataUpdatedAt?: string | null;
};

export type DocumentChunk = {
  id: string;
  documentId: string;
  chunkIndex: number;
  content: string;
  characterCount: number;
  startOffset: number | null;
  endOffset: number | null;
  createdAt: string;
};
