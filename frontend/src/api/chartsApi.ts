import { apiRequest } from "./httpClient";
import type { ChartRequest, ChartResponse } from "../types/charts";

export function createChart(documentId: string, request: ChartRequest) {
  return apiRequest<ChartResponse>(`/api/documents/${documentId}/dataset/chart`, {
    method: "POST",
    body: request
  });
}
