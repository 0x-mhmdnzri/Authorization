"use client";

import { FormEvent, useState } from "react";
import { useRouter } from "next/navigation";

export function CreateRoleForm() {
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
      const res = await fetch("/api/proxy/rbac/roles", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          name: fd.get("name"),
          description: fd.get("description") || undefined,
        }),
      });
      const data = await res.json().catch(() => ({}));
      if (!res.ok) throw new Error(data.message || "Failed");
      setMsg(`Role ${fd.get("name")} created`);
      (e.target as HTMLFormElement).reset();
      router.refresh();
    } catch (ex) {
      setErr(ex instanceof Error ? ex.message : "Error");
    } finally {
      setLoading(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="rounded-xl border border-slate-200 bg-white p-5 space-y-3">
      <h2 className="text-sm font-semibold">Create role</h2>
      <input name="name" required placeholder="Role name" className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm" />
      <input name="description" placeholder="Description" className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm" />
      {msg && <p className="text-xs text-emerald-600">{msg}</p>}
      {err && <p className="text-xs text-rose-600">{err}</p>}
      <button type="submit" disabled={loading} className="w-full rounded-lg bg-brand-600 text-white text-sm font-medium py-2 disabled:opacity-60">
        {loading ? "Creating…" : "Create role"}
      </button>
    </form>
  );
}
