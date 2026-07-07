import React from "react";
import ReactDOM from "react-dom/client";
import { BrowserRouter } from "react-router-dom";
import { App } from "./app/App";
import { AuthProvider } from "./features/auth/AuthProvider";
import "./styles/globals.css";

// Điểm bắt đầu của React app.
// BrowserRouter giúp app điều hướng theo URL.
// AuthProvider lưu thông tin đăng nhập cho toàn bộ app.
ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <BrowserRouter>
      <AuthProvider>
        <App />
      </AuthProvider>
    </BrowserRouter>
  </React.StrictMode>
);
