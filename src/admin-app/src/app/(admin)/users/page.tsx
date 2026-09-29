import { getServerSession } from "next-auth";
import { authOptions } from "@/lib/auth";
import { listUsers } from "@/lib/admin-api";
import { PageHeader } from "@/components/PageHeader";
import { DataTable } from "@/components/DataTable";
import { CreateUserForm } from "./CreateUserForm";
import { AssignSectionForm } from "./AssignSectionForm";
import Link from "next/link";

export default async function UsersPage() {
  const session = await getServerSession(authOptions);
  if (!session?.accessToken) return null;

  let users: Awaited<ReturnType<typeof listUsers>> = [];
  let error: string | null = null;

  try {
    users = await listUsers(session.accessToken);
  } catch (e) {
    error = e instanceof Error ? e.message : "Failed to load users";
  }

  const canWrite = session.user?.isGod || session.user?.roles?.includes("Admin");

  return (
    <div className="space-y-8">
      <PageHeader
        title="Users"
        description="Create users and grant section-level read/write access. GOD and Admin can manage everyone."
        methods="RBAC,DAC"
      />

      {error && (
        <div className="rounded-lg bg-rose-50 text-rose-700 text-sm px-4 py-3">{error}</div>
      )}

      <DataTable
        columns={[
          { key: "email", label: "Email" },
          { key: "name", label: "Name" },
          { key: "department", label: "Department" },
          { key: "clearance", label: "Clearance" },
          { key: "flags", label: "Flags" },
          { key: "actions", label: "" },
        ]}
        rows={users.map((u) => ({
          email: u.email,
          name: [u.firstName, u.lastName].filter(Boolean).join(" ") || "—",
          department: u.department || "—",
          clearance: u.clearanceLevel || "—",
          flags: (
            <span className="flex gap-1 flex-wrap">
              {u.isGod && (
                <span className="text-[10px] font-bold bg-amber-100 text-amber-800 px-1.5 py-0.5 rounded">
                  GOD
                </span>
              )}
              {!u.isActive && (
                <span className="text-[10px] font-bold bg-slate-100 text-slate-500 px-1.5 py-0.5 rounded">
                  inactive
                </span>
              )}
            </span>
          ),
          actions: (
            <Link
              href={`/users/${u.id}`}
              className="text-xs text-brand-600 hover:underline"
            >
              Permissions
            </Link>
          ),
        }))}
        empty="No users found"
      />

      {canWrite && (
        <div className="grid lg:grid-cols-2 gap-6">
          <CreateUserForm />
          <AssignSectionForm users={users.filter((u) => !u.isGod)} />
        </div>
      )}

      {!canWrite && (
        <p className="text-sm text-slate-400">You have read-only access to this section.</p>
      )}
    </div>
  );
}
