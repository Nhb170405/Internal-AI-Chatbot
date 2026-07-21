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

export class ApiRequestError extends Error {
  status: number;

  constructor(message: string, status: number) {
    super(message);
    this.name = "ApiRequestError";
    this.status = status;
  }
}

export async function apiRequest<TResponse>(
  path: string,
  options: RequestOptions = {}
): Promise<TResponse> {
  const headers = new Headers();

  if (!options.formData) {
    headers.set("Content-Type", "application/json");
  }

  const response = await fetch(buildApiUrl(path), {
    method: options.method ?? "GET",
    headers,
    credentials: "include",
    body: options.formData ?? (options.body ? JSON.stringify(options.body) : undefined)
  });

  if (!response.ok) {
    const message = await readErrorMessage(response);
    throw new ApiRequestError(
      message ?? getDefaultErrorMessage(response.status),
      response.status
    );
  }

  if (response.status === 204) {
    return undefined as TResponse;
  }

  return (await response.json()) as TResponse;
}

function getDefaultErrorMessage(status: number) {
  if (status === 400) {
    return "Dữ liệu gửi lên không hợp lệ. Vui lòng kiểm tra lại.";
  }

  if (status === 401) {
    return "Đăng nhập thất bại. Vui lòng kiểm tra lại email hoặc mật khẩu.";
  }

  if (status === 403) {
    return "Bạn không có quyền thực hiện thao tác này.";
  }

  if (status === 404) {
    return "Không tìm thấy dữ liệu yêu cầu.";
  }

  if (status === 429) {
    return "Bạn thao tác quá nhanh. Vui lòng thử lại sau.";
  }

  if (status >= 500) {
    return "Máy chủ đang gặp lỗi. Vui lòng thử lại sau.";
  }

  return `Yêu cầu thất bại với mã lỗi ${status}.`;
}

async function readErrorMessage(response: Response) {
  const contentType = response.headers.get("content-type") ?? "";

  if (contentType.includes("application/json")) {
    const body = (await response.json().catch(() => null)) as {
      message?: string;
      Message?: string;
      errorMessage?: string;
      ErrorMessage?: string;
      title?: string;
      Title?: string;
    } | null;

    return body?.message
      ?? body?.Message
      ?? body?.errorMessage
      ?? body?.ErrorMessage
      ?? body?.title
      ?? body?.Title
      ?? null;
  }

  const text = await response.text().catch(() => "");
  return text.trim() || null;
}
