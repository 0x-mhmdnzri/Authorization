"use client";

import { FormEvent, useState } from "react";
import { useSession } from "next-auth/react";
import { useRouter } from "next/navigation";

export function CreateUserForm() {
  const { data: session } = useSession();
  const router = useRouter();
  const [loading, setLoading] = useState(false);
  const [msg, setMsg] = useState<string | null>(null);
  const [err, setErr] = useState<string | null>(null);

  async function onSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (!session?.accessToken) return;

    const fd = new FormData(e.currentTarget);
    setLoading(true);
    setMsg(null);
    setErr(null);

    try {
      const res = await fetch("/api/proxy/admin/users", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          email: fd.get("email"),
          password: fd.get("password"),
          firstName: fd.get("firstName") || undefined,
          lastName: fd.get("lastName") || undefined,
          department: fd.get("department") || undefined,
          clearanceLevel: fd.get("clearanceLevel") || undefined,
          roles: [String(fd.get("role") || "User")],
        }),
      });
      const data = await res.json().catch(() => ({}));
      if (!res.ok) throw new Error(data.message || "Failed");
      setMsg(`Created ${data.email || fd.get("email")}`);
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
      <h2 className="text-sm font-semibold text-slate-800">Create user</h2>
      <p className="text-xs text-slate-400">Method: RBAC (role assignment) + Identity</p>

      <input name="email" type="email" required placeholder="Email" className="input" />
      <input name="password" type="password" required minLength={6} placeholder="Password" className="input" />
      <div className="grid grid-cols-2 gap-2">
        <input name="firstName" placeholder="First name" className="input" />
        <input name="lastName" placeholder="Last name" className="input" />
      </div>
      <div className="grid grid-cols-2 gap-2">
        <input name="department" placeholder="Department" className="input" />
        <select name="clearanceLevel" className="input">
          <option value="">Clearance…</option>
          <option>Public</option>
          <option>Confidential</option>
          <option>Secret</option>
          <option>TopSecret</option>
        </select>
      </div>
      <select name="role" className="input" defaultValue="User">
        <option>User</option>
        <option>Manager</option>
        <option>Admin</option>
      </select>

      {msg && <p className="text-xs text-emerald-600">{msg}</p>}
      {err && <p className="text-xs text-rose-600">{err}</p>}

      <button type="submit" disabled={loading} className="btn-primary">
        {loading ? "Creating…" : "Create user"}
      </button>

      <style jsx>{`
        .input {
          width: 100%;
          border-radius: 0.5rem;
          border: 1px solid #cbd5e1;
          padding: 0.5rem 0.75rem;
          font-size: 0.875rem;
        }
        .input:focus {
          outline: none;
          box-shadow: 0 0 0 2px #6366f1;
        }
        .btn-primary {
          width: 100%;
          border-radius: 0.5rem;
          background: #4f46e5;
          color: white;
          font-size: 0.875rem;
          font-weight: 500;
          padding: 0.5rem;
        }
        .btn-primary:hover {
          background: #4338ca;
        }
        .btn-primary:disabled {
          opacity: 0.6;
        }
      `}</style>
    </form>
  );
}
