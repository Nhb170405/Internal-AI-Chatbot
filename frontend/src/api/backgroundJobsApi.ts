import { apiRequest } from "./httpClient";
import type { DocumentProcessingJob, DocumentProcessingJobLog } from "../types/backgroundJobs";

export function getLatestJobByDocument(documentId: string) {
  return apiRequest<DocumentProcessingJob>(`/api/background-jobs/document/${documentId}`);
}

export function listJobLogs(jobId: string) {
  return apiRequest<DocumentProcessingJobLog[]>(`/api/background-jobs/${jobId}/logs`);
}

export function retryJob(jobId: string) {
  return apiRequest<void>(`/api/background-jobs/${jobId}/retry`, {
    method: "POST"
  });
}
