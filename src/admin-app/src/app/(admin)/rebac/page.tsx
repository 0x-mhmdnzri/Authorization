import { PageHeader } from "@/components/PageHeader";

export default function RebacPage() {
  return (
    <div className="space-y-6">
      <PageHeader
        title="Relations (ReBAC)"
        description="Zanzibar-style relation tuples. Use API: POST /api/rebac/tuples, POST /api/rebac/check."
        methods="ReBAC"
      />
      <div className="rounded-xl border border-slate-200 bg-white p-5 text-sm text-slate-600">
        <p>List tuples for an object via:</p>
        <code className="block mt-2 text-xs bg-slate-50 p-2 rounded">GET /api/rebac/tuples/&#123;type&#125;/&#123;id&#125;</code>
        <p className="mt-3">Full interactive graph UI can be expanded in a later iteration.</p>
      </div>
    </div>
  );
}
