import { FormEvent, KeyboardEvent, useEffect, useMemo, useRef, useState } from "react";
import { sendAssistantMessage } from "../../api/chatApi";
import { listDocuments } from "../../api/documentsApi";
import { Badge } from "../../components/ui/Badge";
import { Button } from "../../components/ui/Button";
import type { AssistantChatResponse, Citation } from "../../types/chat";
import { useAuth } from "../auth/useAuth";

type ChatThreadItem = {
  id: string;
  userMessage: string;
  response: AssistantChatResponse;
};

type LocalChatSession = {
  id: string;
  title: string;
  createdAt: string;
  updatedAt: string;
  items: ChatThreadItem[];
};

type LocalChatState = {
  activeSessionId: string;
  sessions: LocalChatSession[];
};

const assistantRules = [
  "Bạn có thể hỏi đáp hoặc yêu cầu tóm tắt nội dung trong file PDF, DOCX và TXT đã tải lên.",
  "Với file CSV và XLSX, chatbot hỗ trợ xem cột, đếm dòng, tính tổng, trung bình, nhóm dữ liệu và tìm giá trị cao nhất.",
  "Khi hỏi về dữ liệu bảng, hãy nêu rõ tên file, tên cột và tên sheet nếu file có nhiều sheet.",
  "Kết quả có thể kém chính xác với file scan mờ, bảng phức tạp, dữ liệu sai định dạng hoặc câu hỏi thiếu thông tin.",
  "Không gửi mật khẩu, API key hoặc dữ liệu cá nhân nhạy cảm vào cuộc trò chuyện."
];

export function ChatPage() {
  const { currentUser } = useAuth();
  const storageKey = `factory-chatbot-chat-sessions:${currentUser?.userId ?? currentUser?.email ?? currentUser?.role ?? "anonymous"}`;
  const [message, setMessage] = useState("");
  const [chatState, setChatState] = useState<LocalChatState>(() => loadStoredChatState(storageKey));
  const [isSending, setIsSending] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [documentNames, setDocumentNames] = useState<Record<string, string>>({});
  const scrollRef = useRef<HTMLDivElement | null>(null);

  const activeSession = chatState.sessions.find((session) => session.id === chatState.activeSessionId) ?? chatState.sessions[0];
  const items = activeSession?.items ?? [];

  useEffect(() => {
    setChatState(loadStoredChatState(storageKey));
  }, [storageKey]);

  useEffect(() => {
    let isCurrent = true;

    void listDocuments()
      .then((documents) => {
        if (isCurrent) {
          setDocumentNames(
            Object.fromEntries(documents.map((document) => [document.id, document.originalFileName]))
          );
        }
      })
      .catch(() => {
        // Citation vẫn có thể hiển thị một tên thay thế nếu danh sách tài liệu chưa tải được.
      });

    return () => {
      isCurrent = false;
    };
  }, []);

  useEffect(() => {
    localStorage.setItem(storageKey, JSON.stringify(chatState));
  }, [chatState, storageKey]);

  useEffect(() => {
    scrollRef.current?.scrollTo({ top: scrollRef.current.scrollHeight, behavior: "smooth" });
  }, [items, errorMessage]);

  const lastResponse = items[items.length - 1]?.response;
  const totalThreadTokens = useMemo(
    () => items.reduce((sum, item) => sum + (item.response.totalTokens ?? 0), 0),
    [items]
  );

  async function submitMessage() {
    const trimmedMessage = message.trim();
    if (!trimmedMessage || isSending || !activeSession) {
      return;
    }

    setErrorMessage(null);
    setIsSending(true);

    try {
      const response = await sendAssistantMessage({
        message: trimmedMessage,
        topK: 10
      });

      const now = new Date().toISOString();
      const newItem: ChatThreadItem = {
        id: crypto.randomUUID(),
        userMessage: trimmedMessage,
        response
      };

      setChatState((current) => ({
        activeSessionId: activeSession.id,
        sessions: current.sessions.map((session) =>
          session.id === activeSession.id
            ? {
                ...session,
                title: session.items.length === 0 ? makeSessionTitle(trimmedMessage) : session.title,
                updatedAt: now,
                items: [...session.items, newItem]
              }
            : session
        )
      }));

      setMessage("");
    } catch (err) {
      setErrorMessage(err instanceof Error ? err.message : "Không gửi được tin nhắn. Kiểm tra backend hoặc Assistant module.");
    } finally {
      setIsSending(false);
    }
  }

  function handleSend(event: FormEvent) {
    event.preventDefault();
    void submitMessage();
  }

  function handleTextareaKeyDown(event: KeyboardEvent<HTMLTextAreaElement>) {
    if (event.key === "Enter" && !event.shiftKey) {
      event.preventDefault();
      void submitMessage();
    }
  }

  function createSession() {
    const session = createEmptySession();
    setChatState((current) => ({
      activeSessionId: session.id,
      sessions: [session, ...current.sessions]
    }));
    setErrorMessage(null);
  }

  function deleteSession(sessionId: string) {
    setChatState((current) => {
      const remaining = current.sessions.filter((session) => session.id !== sessionId);
      const sessions = remaining.length > 0 ? remaining : [createEmptySession()];
      const activeSessionId = current.activeSessionId === sessionId ? sessions[0].id : current.activeSessionId;

      return {
        activeSessionId,
        sessions
      };
    });
    setErrorMessage(null);
  }

  function selectSession(sessionId: string) {
    setChatState((current) => ({
      ...current,
      activeSessionId: sessionId
    }));
    setErrorMessage(null);
  }

  return (
    <section className="chat-layout">
      <aside className="chat-sessions">
        <div className="split-header">
          <h2>Chat</h2>
          <Badge tone="neutral">{chatState.sessions.length}</Badge>
        </div>

        <button className="button button-primary new-chat-button" type="button" onClick={createSession}>
          Cuộc trò chuyện mới
        </button>

        <div className="session-list">
          {chatState.sessions.map((session) => (
            <div className={`session-row ${session.id === activeSession?.id ? "active" : ""}`} key={session.id}>
              <button type="button" onClick={() => selectSession(session.id)}>
                <strong>{session.title}</strong>
                <span>{session.items.length} tin nhắn</span>
              </button>
              <button className="session-delete-button" type="button" onClick={() => deleteSession(session.id)} title="Xóa phiên chat">
                ×
              </button>
            </div>
          ))}
        </div>

        <div className="chat-session-meta">
          <span>{totalThreadTokens} tokens trong phiên đang chọn</span>
        </div>
      </aside>

      <div className="chat-thread">
        <div className="thread-scroll" ref={scrollRef}>
          {items.length === 0 ? (
            <div className="empty-state chat-empty-state">
              <h1>Hỏi chatbot nội bộ</h1>
              <p>Enter để gửi, Shift+Enter để xuống dòng.</p>

              <div className="chat-rule-card">
                <strong>Gợi ý sử dụng</strong>
                <ul>
                  {assistantRules.map((rule) => (
                    <li key={rule}>{rule}</li>
                  ))}
                </ul>
              </div>
            </div>
          ) : (
            items.map((item) => (
              <div className="message-pair" key={item.id}>
                <article className="user-message">
                  <p>{item.userMessage}</p>
                </article>

                <article className="assistant-message">
                  <div className="message-meta">
                    <Badge tone={routeTone(item.response.route)}>{item.response.route}</Badge>
                    <span>{item.response.totalTokens ?? 0} tokens</span>
                  </div>

                  <p className="answer-text">{item.response.answer}</p>

                  {item.response.needsUserAction && (
                    <div className="notice-box">
                      <strong>Cần thao tác thêm</strong>
                      <span>{item.response.suggestedAction ?? "user_action_required"}</span>
                    </div>
                  )}

                  {item.response.warnings.length > 0 && (
                    <ul className="warning-list">
                      {item.response.warnings.map((warning) => (
                        <li key={warning}>{warning}</li>
                      ))}
                    </ul>
                  )}

                  {item.response.data ? <JsonPreview value={item.response.data} /> : null}
                </article>
              </div>
            ))
          )}

          {errorMessage && (
            <article className="error-message">
              <p>{errorMessage}</p>
            </article>
          )}
        </div>

        <form className="chat-input" onSubmit={handleSend}>
          <textarea
            value={message}
            onChange={(event) => setMessage(event.target.value)}
            onKeyDown={handleTextareaKeyDown}
            placeholder="Nhập câu hỏi..."
            disabled={isSending}
          />

          <Button type="submit" disabled={isSending}>
            {isSending ? "Đang gửi" : "Gửi"}
          </Button>
        </form>
      </div>

      <aside className="citation-panel">
        <h2>Nguồn tham khảo</h2>

        {lastResponse?.citations?.length ? (
          lastResponse.citations.map((citation) => (
            <div className="citation-item" key={citation.chunkId}>
              <strong>{documentNames[citation.documentId] ?? `Tài liệu ${citation.documentId.slice(0, 8)}`}</strong>
              <span>{formatCitationLocation(citation)}</span>
            </div>
          ))
        ) : (
          <p className="muted">Citation sẽ hiện ở đây khi RAG trả về nguồn.</p>
        )}
      </aside>
    </section>
  );
}

function routeTone(route: AssistantChatResponse["route"]) {
  if (route === "tool_calling") {
    return "info";
  }

  if (route === "rag") {
    return "info";
  }

  if (route === "dataset_profile" || route === "dataset_analyze" || route === "chart") {
    return "success";
  }

  if (route === "unsupported") {
    return "warning";
  }

  return "neutral";
}

function JsonPreview({ value }: { value: unknown }) {
  return (
    <details className="json-preview">
      <summary>Dữ liệu trả về</summary>
      <pre>{JSON.stringify(value, null, 2)}</pre>
    </details>
  );
}

function loadStoredChatState(storageKey: string): LocalChatState {
  const raw = localStorage.getItem(storageKey);
  if (!raw) {
    const session = createEmptySession();
    return {
      activeSessionId: session.id,
      sessions: [session]
    };
  }

  try {
    const parsed = JSON.parse(raw) as LocalChatState;
    if (!parsed.sessions?.length || !parsed.activeSessionId) {
      throw new Error("Invalid chat state.");
    }

    return parsed;
  } catch {
    const session = createEmptySession();
    return {
      activeSessionId: session.id,
      sessions: [session]
    };
  }
}

function createEmptySession(): LocalChatSession {
  const now = new Date().toISOString();

  return {
    id: crypto.randomUUID(),
    title: "Cuộc trò chuyện mới",
    createdAt: now,
    updatedAt: now,
    items: []
  };
}

function makeSessionTitle(message: string) {
  const normalized = message.replace(/\s+/g, " ").trim();
  const maxLength = 30;

  if (normalized.length <= maxLength) {
    return normalized;
  }

  const candidate = normalized.slice(0, maxLength + 1);
  const lastSpace = candidate.lastIndexOf(" ");
  const shortened = lastSpace >= 16 ? candidate.slice(0, lastSpace) : normalized.slice(0, maxLength);

  return `${shortened.trimEnd()}...`;
}

function formatCitationLocation(citation: Citation) {
  if (citation.pageNumber) {
    return `Trang ${citation.pageNumber}`;
  }

  // PDF parser chèn nhãn "Page N:" vào nội dung; dùng nhãn này trong lúc
  // backend chưa lưu pageNumber riêng trong metadata của chunk.
  const pageMatch = citation.snippet.match(/(?:Page|Trang)\s+(\d+)\s*:/i);
  return pageMatch ? `Trang ${pageMatch[1]}` : "Không có thông tin trang";
}
