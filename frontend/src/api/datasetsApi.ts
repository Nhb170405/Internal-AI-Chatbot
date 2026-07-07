import { apiRequest } from "./httpClient";
import type { DatasetAnalyzeRequest, DatasetAnalyzeResponse, DatasetProfileResponse } from "../types/datasets";

export function profileDataset(documentId: string) {
  return apiRequest<DatasetProfileResponse>(`/api/documents/${documentId}/dataset/profile`, {
    method: "POST"
  });
}

export function getDatasetProfiles(documentId: string) {
  return apiRequest<DatasetProfileResponse>(`/api/documents/${documentId}/dataset/profile`);
}

export function analyzeDataset(documentId: string, request: DatasetAnalyzeRequest) {
  return apiRequest<DatasetAnalyzeResponse>(`/api/documents/${documentId}/dataset/analyze`, {
    method: "POST",
    body: request
  });
}
