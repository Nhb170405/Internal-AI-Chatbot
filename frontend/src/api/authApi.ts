import { apiRequest } from "./httpClient";
import type { CurrentUser, GuestLoginRequest, LoginRequest } from "../types/auth";

export function getCurrentUser() {
  return apiRequest<CurrentUser>("/api/auth/me");
}

export function login(request: LoginRequest) {
  return apiRequest<CurrentUser>("/api/auth/login", {
    method: "POST",
    body: request
  });
}

export function guestLogin(request: GuestLoginRequest) {
  return apiRequest<CurrentUser>("/api/auth/guest-login", {
    method: "POST",
    body: request
  });
}

export function logout() {
  return apiRequest<void>("/api/auth/logout", {
    method: "POST"
  });
}
