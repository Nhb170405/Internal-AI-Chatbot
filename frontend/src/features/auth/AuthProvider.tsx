import { createContext, useEffect, useMemo, useState } from "react";
import { getCurrentUser, guestLogin, login, logout } from "../../api/authApi";
import type { CurrentUser, GuestLoginRequest, LoginRequest } from "../../types/auth";

type AuthContextValue = {
  currentUser: CurrentUser | null;
  isLoading: boolean;
  loginWithPassword: (request: LoginRequest) => Promise<void>;
  loginAsGuest: (request: GuestLoginRequest) => Promise<void>;
  logoutCurrentUser: () => Promise<void>;
};

export const AuthContext = createContext<AuthContextValue | null>(null);

type AuthProviderProps = {
  children: React.ReactNode;
};

export function AuthProvider({ children }: AuthProviderProps) {
  const [currentUser, setCurrentUser] = useState<CurrentUser | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    getCurrentUser()
      .then(setCurrentUser)
      .catch(() => setCurrentUser(null))
      .finally(() => setIsLoading(false));
  }, []);

  async function loginWithPassword(request: LoginRequest) {
    const user = await login(request);
    setCurrentUser(user);
  }

  async function loginAsGuest(request: GuestLoginRequest) {
    const user = await guestLogin(request);
    setCurrentUser(user);
  }

  async function logoutCurrentUser() {
    await logout();
    const anonymousUser = await getCurrentUser().catch(() => null);
    setCurrentUser(anonymousUser);
  }

  const value = useMemo<AuthContextValue>(
    () => ({
      currentUser,
      isLoading,
      loginWithPassword,
      loginAsGuest,
      logoutCurrentUser
    }),
    [currentUser, isLoading]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
