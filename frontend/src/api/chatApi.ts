import { apiRequest } from "./httpClient";
import type {
  AssistantChatRequest,
  AssistantChatResponse,
  ChatMessageRequest,
  ChatMessageResponse,
  RagChatRequest,
  RagChatResponse
} from "../types/chat";

export function sendBasicChatMessage(request: ChatMessageRequest) {
  return apiRequest<ChatMessageResponse>("/api/chat/message", {
    method: "POST",
    body: request
  });
}

export function sendRagChatMessage(request: RagChatRequest) {
  return apiRequest<RagChatResponse>("/api/rag/chat", {
    method: "POST",
    body: request
  });
}

export function sendAssistantMessage(request: AssistantChatRequest) {
  return apiRequest<AssistantChatResponse>("/api/assistant/chat", {
    method: "POST",
    body: request
  });
}
