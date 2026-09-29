import { MethodBadge } from "./MethodBadge";

export function PageHeader({
  title,
  description,
  methods,
}: {
  title: string;
  description?: string;
  methods: string;
}) {
  return (
    <div className="mb-6">
      <div className="flex flex-wrap items-center gap-3">
        <h1 className="text-2xl font-bold text-slate-900">{title}</h1>
        <MethodBadge methods={methods} />
      </div>
      {description && <p className="text-sm text-slate-500 mt-1">{description}</p>}
      <p className="text-xs text-slate-400 mt-2">
        Authorization method(s) used for this section: <strong>{methods}</strong>
      </p>
    </div>
  );
}
