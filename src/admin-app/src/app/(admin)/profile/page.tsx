import { getServerSession } from "next-auth";
import { authOptions } from "@/lib/auth";
import { backendMe } from "@/lib/api";
import { PageHeader } from "@/components/PageHeader";

export default async function ProfilePage() {
  const session = await getServerSession(authOptions);
  if (!session?.accessToken) return null;

  let me: Record<string, unknown> = {};
  try {
    me = await backendMe(session.accessToken);
  } catch {
    me = {};
  }

  return (
    <div className="space-y-6 max-w-xl">
      <PageHeader
        title="My Access"
        description="Current identity claims from the access token and /api/auth/me."
        methods="CBAC,RBAC"
      />

      <div className="rounded-xl border border-slate-200 bg-white p-5 space-y-3 text-sm">
        {Object.entries({
          Id: me.id ?? session.user?.id,
          Email: me.email ?? session.user?.email,
          "First name": me.firstName,
          "Last name": me.lastName,
          Department: me.department ?? session.user?.department,
          Clearance: me.clearanceLevel ?? session.user?.clearance,
          Roles: Array.isArray(me.roles) ? (me.roles as string[]).join(", ") : session.user?.roles?.join(", "),
          "Is GOD": String(me.isGod ?? session.user?.isGod),
        }).map(([k, v]) => (
          <div key={k} className="flex justify-between gap-4 border-b border-slate-50 pb-2">
            <span className="text-slate-400">{k}</span>
            <span className="font-medium text-right">{String(v ?? "—")}</span>
          </div>
        ))}
      </div>
    </div>
  );
}
