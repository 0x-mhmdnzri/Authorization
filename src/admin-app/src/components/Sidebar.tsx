import Link from "next/link";
import type { MenuItem } from "@/lib/api";
import { MethodBadge } from "./MethodBadge";

export function Sidebar({
  items,
  userEmail,
  isGod,
}: {
  items: MenuItem[];
  userEmail?: string;
  isGod?: boolean;
}) {
  return (
    <aside className="w-64 shrink-0 bg-slate-900 text-slate-100 flex flex-col min-h-screen">
      <div className="px-4 py-5 border-b border-slate-700">
        <div className="font-semibold text-sm tracking-wide">Authorization</div>
        <div className="text-xs text-slate-400 mt-0.5">Admin Panel</div>
      </div>

      <nav className="flex-1 overflow-y-auto py-3 px-2 space-y-0.5">
        {items.map((item) => (
          <div key={item.key}>
            <Link
              href={item.href || "#"}
              className="flex flex-col gap-1 rounded-lg px-3 py-2 hover:bg-slate-800 transition group"
            >
              <span className="text-sm font-medium group-hover:text-white">{item.title}</span>
              {item.authorizationMethods && (
                <MethodBadge methods={item.authorizationMethods} />
              )}
              {!item.canWrite && item.canRead && (
                <span className="text-[10px] text-slate-500">read-only</span>
              )}
            </Link>
            {item.children?.length > 0 && (
              <div className="ml-3 border-l border-slate-700 pl-2 mt-0.5 space-y-0.5">
                {item.children.map((child) => (
                  <Link
                    key={child.key}
                    href={child.href || "#"}
                    className="block rounded-lg px-3 py-1.5 text-xs text-slate-300 hover:bg-slate-800 hover:text-white"
                  >
                    {child.title}
                  </Link>
                ))}
              </div>
            )}
          </div>
        ))}
      </nav>

      <div className="px-4 py-3 border-t border-slate-700 text-xs text-slate-400">
        <div className="truncate">{userEmail}</div>
        {isGod && (
          <span className="inline-block mt-1 rounded bg-amber-500/20 text-amber-300 px-1.5 py-0.5 text-[10px] font-bold">
            GOD
          </span>
        )}
      </div>
    </aside>
  );
}
