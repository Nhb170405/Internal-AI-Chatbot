export type ChatMessageRequest = {
  message: string;
};

export type RagChatRequest = {
  question: string;
  topK: number;
};

export type ChatMessageResponse = {
  answer: string;
  model: string;
  promptTokens: number | null;
  completionTokens: number | null;
  totalTokens: number | null;
};

export type Citation = {
  documentId: string;
  chunkId: string;
  chunkIndex: number;
  score: number;
  snippet: string;
  pageNumber: number | null;
};

export type RagChatResponse = ChatMessageResponse & {
  citations: Citation[];
};

export type AssistantChatRequest = {
  message: string;
  topK: number;
};

export type AssistantChatResponse = {
  route: "chitchat" | "rag" | "document_metadata" | "dataset_profile" | "dataset_analyze" | "chart" | "unsupported";
  answer: string;
  model: string | null;
  promptTokens: number | null;
  completionTokens: number | null;
  totalTokens: number | null;
  citations: Citation[];
  data: unknown;
  chartPath: string | null;
  warnings: string[];
  needsUserAction: boolean;
  suggestedAction: string | null;
};
