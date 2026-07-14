import { FormEvent, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Button } from "../../components/ui/Button";
import { TextInput } from "../../components/ui/TextInput";
import { useAuth } from "./useAuth";

export function LoginPage() {
  const navigate = useNavigate();
  const { loginWithPassword, loginAsGuest } = useAuth();
  const [email, setEmail] = useState("admin@company.com");
  const [password, setPassword] = useState("Admin@123");
  const [guestName, setGuestName] = useState("Guest");
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handlePasswordLogin(event: FormEvent) {
    event.preventDefault();
    setErrorMessage(null);
    setIsSubmitting(true);

    try {
      await loginWithPassword({ email, password });
      navigate("/chat");
    } catch (error) {
      setErrorMessage(error instanceof Error ? error.message : "Đăng nhập thất bại. Kiểm tra email hoặc mật khẩu.");
    } finally {
      setIsSubmitting(false);
    }
  }

  async function handleGuestLogin() {
    setErrorMessage(null);
    setIsSubmitting(true);

    try {
      await loginAsGuest({ displayName: guestName });
      navigate("/chat");
    } catch (error) {
      setErrorMessage(error instanceof Error ? error.message : "Không thể tạo phiên guest.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <main className="login-shell">
      <section className="login-panel">
        <div>
          <p className="eyebrow">Factory Chatbot V2</p>
          <h1>Đăng nhập hệ thống</h1>
          <p className="muted">
            Chọn tài khoản nội bộ hoặc dùng phiên guest để bắt đầu chat với tài liệu được phép.
          </p>
        </div>

        <form className="stack" onSubmit={handlePasswordLogin}>
          <TextInput label="Email" value={email} onChange={setEmail} />
          <TextInput label="Mật khẩu" type="password" value={password} onChange={setPassword} />
          <Button type="submit" disabled={isSubmitting}>
            Đăng nhập employee/admin
          </Button>
        </form>

        <div className="divider">hoặc</div>

        <div className="stack">
          <TextInput label="Tên guest" value={guestName} onChange={setGuestName} />
          <Button type="button" variant="secondary" onClick={handleGuestLogin} disabled={isSubmitting}>
            Vào bằng guest
          </Button>
        </div>

        {errorMessage && <p className="error-text">{errorMessage}</p>}
      </section>
    </main>
  );
}
