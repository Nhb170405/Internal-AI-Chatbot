export type ProcessingJobStatus = "queued" | "running" | "completed" | "failed";

export type DocumentProcessingJob = {
  id: string;
  documentId: string;
  hangfireJobId: string | null;
  jobType: string;
  status: ProcessingJobStatus;
  attemptCount: number;
  maxAttempts: number;
  lastError: string | null;
  createdAt: string;
  updatedAt: string;
  startedAt: string | null;
  completedAt: string | null;
};

export type DocumentProcessingJobLog = {
  id: string;
  documentProcessingJobId: string;
  documentId: string;
  jobType: string;
  step: string;
  status: string;
  attempt: number;
  errorMessage: string | null;
  createdAt: string;
  startedAt: string | null;
  completedAt: string | null;
};
