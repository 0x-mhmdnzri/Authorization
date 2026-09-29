import { getServerSession } from "next-auth";
import { authOptions } from "@/lib/auth";
import { MethodBadge } from "@/components/MethodBadge";

const MODELS = [
  { name: "RBAC", desc: "Role-Based Access Control" },
  { name: "ABAC", desc: "Attribute-Based Access Control" },
  { name: "MAC", desc: "Mandatory Access Control" },
  { name: "DAC", desc: "Discretionary Access Control" },
  { name: "PBAC-Policy", desc: "Central Policy Decision Point" },
  { name: "PBAC-Purpose", desc: "Purpose-Bound Access" },
  { name: "RAdAC", desc: "Risk-Adaptive Access Control" },
  { name: "ReBAC", desc: "Relationship-Based Access Control" },
  { name: "PAC", desc: "Privileged / JIT Access" },
  { name: "CBAC", desc: "Context-Based Access Control" },
  { name: "RuBAC", desc: "Rule-Based Access Control" },
];

export default async function DashboardPage() {
  const session = await getServerSession(authOptions);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-slate-900">Dashboard</h1>
        <p className="text-sm text-slate-500 mt-1">
          Welcome, {session?.user?.email}
          {session?.user?.isGod && (
            <span className="ml-2 text-amber-600 font-semibold">(GOD)</span>
          )}
        </p>
      </div>

      <div className="rounded-xl border border-slate-200 bg-white p-5">
        <h2 className="text-sm font-semibold text-slate-700 mb-1">Session claims</h2>
        <p className="text-xs text-slate-400 mb-3">
          Method used: <MethodBadge methods="CBAC,RBAC" />
        </p>
        <dl className="grid grid-cols-2 gap-3 text-sm">
          <div>
            <dt className="text-slate-400">Roles</dt>
            <dd className="font-medium">{session?.user?.roles?.join(", ") || "—"}</dd>
          </div>
          <div>
            <dt className="text-slate-400">Department</dt>
            <dd className="font-medium">{session?.user?.department || "—"}</dd>
          </div>
          <div>
            <dt className="text-slate-400">Clearance</dt>
            <dd className="font-medium">{session?.user?.clearance || "—"}</dd>
          </div>
          <div>
            <dt className="text-slate-400">Is GOD</dt>
            <dd className="font-medium">{session?.user?.isGod ? "Yes" : "No"}</dd>
          </div>
        </dl>
      </div>

      <div>
        <h2 className="text-sm font-semibold text-slate-700 mb-3">
          Authorization models in this system
        </h2>
        <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-3">
          {MODELS.map((m) => (
            <div
              key={m.name}
              className="rounded-xl border border-slate-200 bg-white p-4 hover:border-brand-500 transition"
            >
              <MethodBadge methods={m.name} />
              <p className="text-sm text-slate-600 mt-2">{m.desc}</p>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
