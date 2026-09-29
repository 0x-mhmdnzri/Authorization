const API_URL = process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || "http://localhost:8080";

export type AuthTokens = {
  accessToken: string;
  accessTokenExpiration: string;
  refreshToken: string;
  refreshTokenExpiration: string;
  userId: string;
  email: string;
  roles: string[];
  isGod: boolean;
};

export type MenuItem = {
  key: string;
  title: string;
  description?: string;
  href?: string;
  icon?: string;
  authorizationMethods: string;
  sortOrder: number;
  canRead: boolean;
  canWrite: boolean;
  children: MenuItem[];
};

export async function backendLogin(email: string, password: string): Promise<AuthTokens> {
  const res = await fetch(`${API_URL}/api/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password }),
  });

  if (!res.ok) {
    const body = await res.json().catch(() => ({}));
    throw new Error(body.message || "Invalid credentials");
  }

  return res.json();
}

export async function backendRefresh(refreshToken: string): Promise<{
  accessToken: string;
  accessTokenExpiration: string;
  refreshToken: string;
  refreshTokenExpiration: string;
}> {
  const res = await fetch(`${API_URL}/api/auth/refresh`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ refreshToken }),
  });

  if (!res.ok) throw new Error("Refresh failed");
  return res.json();
}

export async function backendRenew(refreshToken: string): Promise<{
  accessToken: string;
  accessTokenExpiration: string;
  refreshToken: string;
  refreshTokenExpiration: string;
}> {
  const res = await fetch(`${API_URL}/api/auth/renew`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ refreshToken }),
  });

  if (!res.ok) throw new Error("Renew failed");
  return res.json();
}

export async function backendRevoke(refreshToken: string): Promise<void> {
  await fetch(`${API_URL}/api/auth/revoke`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ refreshToken }),
  });
}

export async function fetchMenu(accessToken: string): Promise<MenuItem[]> {
  const res = await fetch(`${API_URL}/api/menu`, {
    headers: { Authorization: `Bearer ${accessToken}` },
    cache: "no-store",
  });

  if (!res.ok) throw new Error("Failed to load menu");
  return res.json();
}

export async function backendMe(accessToken: string) {
  const res = await fetch(`${API_URL}/api/auth/me`, {
    headers: { Authorization: `Bearer ${accessToken}` },
    cache: "no-store",
  });
  if (!res.ok) throw new Error("Failed to load profile");
  return res.json();
}

export function getApiUrl() {
  return API_URL;
}
