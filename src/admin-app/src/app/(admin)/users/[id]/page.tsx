import { getServerSession } from "next-auth";
import { authOptions } from "@/lib/auth";
import { getUserSectionPermissions, getUserRoles } from "@/lib/admin-api";
import { PageHeader } from "@/components/PageHeader";
import { DataTable } from "@/components/DataTable";
import Link from "next/link";

export default async function UserDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const session = await getServerSession(authOptions);
  if (!session?.accessToken) return null;

  let perms: Awaited<ReturnType<typeof getUserSectionPermissions>> = [];
  let roles: { roles?: string[]; email?: string } = {};
  let error: string | null = null;

  try {
    [perms, roles] = await Promise.all([
      getUserSectionPermissions(session.accessToken, id),
      getUserRoles(session.accessToken, id),
    ]);
  } catch (e) {
    error = e instanceof Error ? e.message : "Failed to load";
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <Link href="/users" className="text-sm text-brand-600 hover:underline">
          ← Users
        </Link>
      </div>

      <PageHeader
        title={roles.email || `User ${id.slice(0, 8)}…`}
        description="Section permissions (DAC) and roles (RBAC) for this user."
        methods="RBAC,DAC"
      />

      {error && (
        <div className="rounded-lg bg-rose-50 text-rose-700 text-sm px-4 py-3">{error}</div>
      )}

      <div>
        <h2 className="text-sm font-semibold mb-2">Roles (RBAC)</h2>
        <div className="flex flex-wrap gap-2">
          {(roles.roles || []).map((r) => (
            <span
              key={r}
              className="rounded-full bg-indigo-100 text-indigo-800 text-xs font-medium px-3 py-1"
            >
              {r}
            </span>
          ))}
          {(roles.roles || []).length === 0 && (
            <span className="text-sm text-slate-400">No roles</span>
          )}
        </div>
      </div>

      <div>
        <h2 className="text-sm font-semibold mb-2">Section permissions (DAC)</h2>
        <DataTable
          columns={[
            { key: "section", label: "Section" },
            { key: "read", label: "Read" },
            { key: "write", label: "Write" },
            { key: "granted", label: "Granted" },
          ]}
          rows={perms.map((p) => ({
            section: (
              <span>
                <span className="font-medium">{p.sectionTitle}</span>
                <span className="text-xs text-slate-400 ml-2">{p.sectionKey}</span>
              </span>
            ),
            read: p.canRead ? "✓" : "—",
            write: p.canWrite ? "✓" : "—",
            granted: new Date(p.grantedAt).toLocaleString(),
          }))}
          empty="No section permissions assigned"
        />
      </div>
    </div>
  );
}
