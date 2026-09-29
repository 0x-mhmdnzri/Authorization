import clsx from "clsx";

const STYLE: Record<string, string> = {
  RBAC: "badge-rbac",
  ABAC: "badge-abac",
  MAC: "badge-mac",
  DAC: "badge-dac",
  "PBAC-Policy": "badge-pbac",
  "PBAC-Purpose": "badge-pbac",
  PBAC: "badge-pbac",
  RAdAC: "badge-radac",
  RADAC: "badge-radac",
  ReBAC: "badge-rebac",
  REBAC: "badge-rebac",
  PAC: "badge-pac",
  CBAC: "badge-cbac",
  RuBAC: "badge-rubac",
  RUBAC: "badge-rubac",
};

export function MethodBadge({ methods }: { methods: string }) {
  const parts = methods
    .split(",")
    .map((m) => m.trim())
    .filter(Boolean);

  return (
    <span className="inline-flex flex-wrap gap-1">
      {parts.map((m) => (
        <span
          key={m}
          className={clsx(
            "inline-flex items-center rounded px-1.5 py-0.5 text-[10px] font-semibold uppercase tracking-wide",
            STYLE[m] || "bg-slate-100 text-slate-700"
          )}
        >
          {m}
        </span>
      ))}
    </span>
  );
}
