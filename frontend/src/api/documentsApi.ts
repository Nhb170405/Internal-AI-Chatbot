import { apiRequest } from "./httpClient";
import type { DocumentAccessLevel, DocumentChunk, DocumentListItem, DocumentMetadata } from "../types/documents";

export function listDocuments() {
  return apiRequest<DocumentListItem[]>("/api/documents");
}

export function getDocument(documentId: string) {
  return apiRequest<DocumentListItem>(`/api/documents/${documentId}`);
}

export function uploadDocument(file: File, accessLevel: DocumentAccessLevel) {
  const formData = new FormData();
  formData.append("file", file);
  formData.append("accessLevel", accessLevel);

  return apiRequest<DocumentListItem>("/api/documents/upload", {
    method: "POST",
    formData
  });
}

export function deleteDocument(documentId: string) {
  return apiRequest<void>(`/api/documents/${documentId}`, {
    method: "DELETE"
  });
}

export function restoreDocument(documentId: string) {
  return apiRequest<DocumentListItem>(`/api/documents/${documentId}/restore`, {
    method: "POST"
  });
}

export function getDocumentMetadata(documentId: string) {
  return apiRequest<DocumentMetadata>(`/api/documents/${documentId}/metadata`);
}

export function ingestDocument(documentId: string) {
  return apiRequest<unknown>(`/api/documents/${documentId}/ingest`, { method: "POST" });
}

export function chunkDocument(documentId: string) {
  return apiRequest<unknown>(`/api/documents/${documentId}/chunk`, { method: "POST" });
}

export function indexDocument(documentId: string) {
  return apiRequest<unknown>(`/api/documents/${documentId}/index`, { method: "POST" });
}

export function listDocumentChunks(documentId: string) {
  return apiRequest<DocumentChunk[]>(`/api/documents/${documentId}/chunks`);
}
