import { getServerSession } from "next-auth";
import { redirect } from "next/navigation";
import { authOptions } from "@/lib/auth";
import { fetchMenu } from "@/lib/api";
import { Sidebar } from "@/components/Sidebar";
import { SignOutButton } from "@/components/SignOutButton";

export default async function AdminLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const session = await getServerSession(authOptions);

  if (!session?.accessToken || session.error === "RefreshAccessTokenError") {
    redirect("/login");
  }

  let menu: Awaited<ReturnType<typeof fetchMenu>> = [];
  try {
    menu = await fetchMenu(session.accessToken);
  } catch {
    // Menu load failure should not crash the shell; show empty nav
    menu = [];
  }

  return (
    <div className="flex min-h-screen">
      {/* Menu is fully server-rendered from GET /api/menu */}
      <Sidebar
        items={menu}
        userEmail={session.user?.email}
        isGod={session.user?.isGod}
      />

      <div className="flex-1 flex flex-col min-w-0">
        <header className="h-14 border-b border-slate-200 bg-white flex items-center justify-between px-6 shrink-0">
          <div className="text-sm text-slate-500">
            {session.user?.isGod ? "GOD session" : session.user?.roles?.join(", ")}
          </div>
          <SignOutButton />
        </header>

        <main className="flex-1 p-6 overflow-auto">{children}</main>
      </div>
    </div>
  );
}
