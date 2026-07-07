export type UserRole = "anonymous" | "guest" | "employee" | "admin";

export type CurrentUser = {
  userId: string | null;
  guestSessionId: string | null;
  displayName: string;
  email: string | null;
  role: UserRole;
  departmentId: string | null;
  expiresAt: string | null;
};

export type LoginRequest = {
  email: string;
  password: string;
};

export type GuestLoginRequest = {
  displayName: string;
};
