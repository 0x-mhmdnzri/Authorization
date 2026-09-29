import { getApiUrl } from "./api";

function authHeaders(accessToken: string): HeadersInit {
  return {
    Authorization: `Bearer ${accessToken}`,
    "Content-Type": "application/json",
  };
}

export type AdminUser = {
  id: string;
  email: string;
  firstName?: string;
  lastName?: string;
  department?: string;
  clearanceLevel?: string;
  isActive: boolean;
  isGod: boolean;
  createdAt: string;
};

export type SectionPerm = {
  id: string;
  sectionKey: string;
  sectionTitle: string;
  canRead: boolean;
  canWrite: boolean;
  grantedAt: string;
  expiresAt?: string;
  grantedById: string;
};

export type RoleItem = {
  id: string;
  name: string;
  description?: string;
  createdAt?: string;
};

export async function listUsers(accessToken: string): Promise<AdminUser[]> {
  const res = await fetch(`${getApiUrl()}/api/admin/users`, {
    headers: authHeaders(accessToken),
    cache: "no-store",
  });
  if (!res.ok) throw new Error("Failed to list users");
  return res.json();
}

export async function createUser(
  accessToken: string,
  body: {
    email: string;
    password: string;
    firstName?: string;
    lastName?: string;
    department?: string;
    clearanceLevel?: string;
    roles?: string[];
  }
) {
  const res = await fetch(`${getApiUrl()}/api/admin/users`, {
    method: "POST",
    headers: authHeaders(accessToken),
    body: JSON.stringify(body),
  });
  const data = await res.json().catch(() => ({}));
  if (!res.ok) throw new Error(data.message || JSON.stringify(data) || "Create failed");
  return data;
}

export async function assignSectionPermission(
  accessToken: string,
  body: {
    userId: string;
    sectionKey: string;
    canRead: boolean;
    canWrite: boolean;
    expiresAt?: string;
  }
) {
  const res = await fetch(`${getApiUrl()}/api/admin/users/section-permissions`, {
    method: "POST",
    headers: authHeaders(accessToken),
    body: JSON.stringify(body),
  });
  const data = await res.json().catch(() => ({}));
  if (!res.ok) throw new Error(data.message || "Assign failed");
  return data;
}

export async function getUserSectionPermissions(
  accessToken: string,
  userId: string
): Promise<SectionPerm[]> {
  const res = await fetch(`${getApiUrl()}/api/admin/users/${userId}/section-permissions`, {
    headers: authHeaders(accessToken),
    cache: "no-store",
  });
  if (!res.ok) throw new Error("Failed to load section permissions");
  return res.json();
}

export async function listRoles(accessToken: string): Promise<RoleItem[]> {
  const res = await fetch(`${getApiUrl()}/api/rbac/roles`, {
    headers: authHeaders(accessToken),
    cache: "no-store",
  });
  if (!res.ok) throw new Error("Failed to list roles");
  return res.json();
}

export async function createRole(
  accessToken: string,
  body: { name: string; description?: string }
) {
  const res = await fetch(`${getApiUrl()}/api/rbac/roles`, {
    method: "POST",
    headers: authHeaders(accessToken),
    body: JSON.stringify(body),
  });
  const data = await res.json().catch(() => ({}));
  if (!res.ok) throw new Error(data.message || "Create role failed");
  return data;
}

export async function assignRole(
  accessToken: string,
  body: { userId: string; roleName: string }
) {
  const res = await fetch(`${getApiUrl()}/api/rbac/assign-role`, {
    method: "POST",
    headers: authHeaders(accessToken),
    body: JSON.stringify(body),
  });
  const data = await res.json().catch(() => ({}));
  if (!res.ok) throw new Error(data.message || "Assign role failed");
  return data;
}

export async function removeRole(
  accessToken: string,
  body: { userId: string; roleName: string }
) {
  const res = await fetch(`${getApiUrl()}/api/rbac/remove-role`, {
    method: "DELETE",
    headers: authHeaders(accessToken),
    body: JSON.stringify(body),
  });
  const data = await res.json().catch(() => ({}));
  if (!res.ok) throw new Error(data.message || "Remove role failed");
  return data;
}

export async function getUserRoles(accessToken: string, userId: string) {
  const res = await fetch(`${getApiUrl()}/api/rbac/users/${userId}/roles`, {
    headers: authHeaders(accessToken),
    cache: "no-store",
  });
  if (!res.ok) throw new Error("Failed to load user roles");
  return res.json();
}

/** Generic GET helper for model demo endpoints */
export async function apiGet<T = unknown>(accessToken: string, path: string): Promise<T> {
  const res = await fetch(`${getApiUrl()}${path}`, {
    headers: authHeaders(accessToken),
    cache: "no-store",
  });
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { message?: string }).message || `GET ${path} failed (${res.status})`);
  }
  return res.json();
}
