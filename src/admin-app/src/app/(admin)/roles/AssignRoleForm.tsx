"use client";

import { FormEvent, useState } from "react";
import { useRouter } from "next/navigation";
import type { AdminUser } from "@/lib/admin-api";

export function AssignRoleForm({
  users,
  roles,
}: {
  users: AdminUser[];
  roles: string[];
}) {
  const router = useRouter();
  const [loading, setLoading] = useState(false);
  const [msg, setMsg] = useState<string | null>(null);
  const [err, setErr] = useState<string | null>(null);

  async function onSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const fd = new FormData(e.currentTarget);
    setLoading(true);
    setMsg(null);
    setErr(null);
    try {
      const res = await fetch("/api/proxy/rbac/assign-role", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          userId: fd.get("userId"),
          roleName: fd.get("roleName"),
        }),
      });
      const data = await res.json().catch(() => ({}));
      if (!res.ok) throw new Error(data.message || "Failed");
      setMsg(data.message || "Assigned");
      router.refresh();
    } catch (ex) {
      setErr(ex instanceof Error ? ex.message : "Error");
    } finally {
      setLoading(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="rounded-xl border border-slate-200 bg-white p-5 space-y-3">
      <h2 className="text-sm font-semibold">Assign role to user</h2>
      <select name="userId" required className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm">
        <option value="">Select user…</option>
        {users.map((u) => (
          <option key={u.id} value={u.id}>{u.email}</option>
        ))}
      </select>
      <select name="roleName" required className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm">
        <option value="">Select role…</option>
        {roles.map((r) => (
          <option key={r} value={r}>{r}</option>
        ))}
      </select>
      {msg && <p className="text-xs text-emerald-600">{msg}</p>}
      {err && <p className="text-xs text-rose-600">{err}</p>}
      <button type="submit" disabled={loading} className="w-full rounded-lg bg-brand-600 text-white text-sm font-medium py-2 disabled:opacity-60">
        {loading ? "Assigning…" : "Assign role"}
      </button>
    </form>
  );
}
