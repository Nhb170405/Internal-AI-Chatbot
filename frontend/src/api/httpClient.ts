const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5055";

export function buildApiUrl(path: string) {
  if (path.startsWith("http://") || path.startsWith("https://")) {
    return path;
  }

  return `${API_BASE_URL}${path}`;
}

type RequestOptions = {
  method?: "GET" | "POST" | "PUT" | "DELETE";
  body?: unknown;
  formData?: FormData;
};

export async function apiRequest<TResponse>(path: string, options: RequestOptions = {}): Promise<TResponse> {
  const headers = new Headers();

  if (!options.formData) {
    headers.set("Content-Type", "application/json");
  }

  const response = await fetch(`${API_BASE_URL}${path}`, {
    method: options.method ?? "GET",
    headers,
    credentials: "include",
    body: options.formData ?? (options.body ? JSON.stringify(options.body) : undefined)
  });

  if (!response.ok) {
    const message = await readErrorMessage(response);
    throw new Error(message ?? `API request failed with status ${response.status}`);
  }

  if (response.status === 204) {
    return undefined as TResponse;
  }

  return (await response.json()) as TResponse;
}

async function readErrorMessage(response: Response) {
  const contentType = response.headers.get("content-type") ?? "";

  if (contentType.includes("application/json")) {
    const body = (await response.json().catch(() => null)) as { message?: string; errorMessage?: string; title?: string } | null;
    return body?.message ?? body?.errorMessage ?? body?.title ?? null;
  }

  const text = await response.text().catch(() => "");
  return text.trim() || null;
}
