import { getServerSession } from "next-auth";
import { authOptions } from "@/lib/auth";
import { listRoles, listUsers } from "@/lib/admin-api";
import { PageHeader } from "@/components/PageHeader";
import { DataTable } from "@/components/DataTable";
import { CreateRoleForm } from "./CreateRoleForm";
import { AssignRoleForm } from "./AssignRoleForm";

export default async function RolesPage() {
  const session = await getServerSession(authOptions);
  if (!session?.accessToken) return null;

  let roles: Awaited<ReturnType<typeof listRoles>> = [];
  let users: Awaited<ReturnType<typeof listUsers>> = [];
  let error: string | null = null;

  try {
    roles = await listRoles(session.accessToken);
    try {
      users = await listUsers(session.accessToken);
    } catch {
      /* optional */
    }
  } catch (e) {
    error = e instanceof Error ? e.message : "Failed to load roles";
  }

  const canWrite = session.user?.isGod || session.user?.roles?.includes("Admin");

  return (
    <div className="space-y-8">
      <PageHeader
        title="Roles & Assignments"
        description="Classic RBAC: roles, create role, assign/remove role to users."
        methods="RBAC"
      />

      {error && (
        <div className="rounded-lg bg-rose-50 text-rose-700 text-sm px-4 py-3">{error}</div>
      )}

      <DataTable
        columns={[
          { key: "name", label: "Role" },
          { key: "description", label: "Description" },
          { key: "created", label: "Created" },
        ]}
        rows={roles.map((r) => ({
          name: <span className="font-medium">{r.name}</span>,
          description: r.description || "—",
          created: r.createdAt ? new Date(r.createdAt).toLocaleDateString() : "—",
        }))}
        empty="No roles"
      />

      {canWrite && (
        <div className="grid lg:grid-cols-2 gap-6">
          <CreateRoleForm />
          <AssignRoleForm
            users={users.filter((u) => !u.isGod)}
            roles={roles.map((r) => r.name)}
          />
        </div>
      )}
    </div>
  );
}
