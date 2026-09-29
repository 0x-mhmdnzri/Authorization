"use client";

import { FormEvent, useState } from "react";
import { useRouter } from "next/navigation";
import type { AdminUser } from "@/lib/admin-api";

const SECTIONS = [
  "dashboard", "users", "roles", "resources", "abac", "pbac",
  "purpose", "radac", "rebac", "pac", "cbac", "rubac", "profile",
];

export function AssignSectionForm({ users }: { users: AdminUser[] }) {
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
      const res = await fetch("/api/proxy/admin/users/section-permissions", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          userId: fd.get("userId"),
          sectionKey: fd.get("sectionKey"),
          canRead: fd.get("canRead") === "on",
          canWrite: fd.get("canWrite") === "on",
        }),
      });
      const data = await res.json().catch(() => ({}));
      if (!res.ok) throw new Error(data.message || "Failed");
      setMsg("Permission assigned");
      router.refresh();
    } catch (ex) {
      setErr(ex instanceof Error ? ex.message : "Error");
    } finally {
      setLoading(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="rounded-xl border border-slate-200 bg-white p-5 space-y-3">
      <h2 className="text-sm font-semibold text-slate-800">Assign section permission</h2>
      <p className="text-xs text-slate-400">Method: DAC (granter decides) constrained by RBAC</p>

      <select name="userId" required className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm">
        <option value="">Select user…</option>
        {users.map((u) => (
          <option key={u.id} value={u.id}>
            {u.email}
          </option>
        ))}
      </select>

      <select name="sectionKey" required className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm">
        {SECTIONS.map((s) => (
          <option key={s} value={s}>
            {s}
          </option>
        ))}
      </select>

      <label className="flex items-center gap-2 text-sm">
        <input type="checkbox" name="canRead" defaultChecked /> Can read
      </label>
      <label className="flex items-center gap-2 text-sm">
        <input type="checkbox" name="canWrite" /> Can write
      </label>

      {msg && <p className="text-xs text-emerald-600">{msg}</p>}
      {err && <p className="text-xs text-rose-600">{err}</p>}

      <button
        type="submit"
        disabled={loading}
        className="w-full rounded-lg bg-brand-600 hover:bg-brand-700 disabled:opacity-60 text-white text-sm font-medium py-2"
      >
        {loading ? "Saving…" : "Assign permission"}
      </button>
    </form>
  );
}
