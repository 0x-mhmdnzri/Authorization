import { getServerSession } from "next-auth";
import { authOptions } from "@/lib/auth";
import { apiGet } from "@/lib/admin-api";
import { PageHeader } from "./PageHeader";

export async function ModelListPage({
  title,
  description,
  methods,
  endpoint,
}: {
  title: string;
  description: string;
  methods: string;
  endpoint: string;
}) {
  const session = await getServerSession(authOptions);
  if (!session?.accessToken) return null;

  let data: unknown = null;
  let error: string | null = null;

  try {
    data = await apiGet(session.accessToken, endpoint);
  } catch (e) {
    error = e instanceof Error ? e.message : "Failed to load";
  }

  return (
    <div className="space-y-6">
      <PageHeader title={title} description={description} methods={methods} />

      {error && (
        <div className="rounded-lg bg-amber-50 text-amber-800 text-sm px-4 py-3">
          {error}
          <p className="text-xs mt-1 text-amber-600">
            Endpoint: <code>{endpoint}</code> — ensure you have permission and the API is running.
          </p>
        </div>
      )}

      {!error && (
        <pre className="rounded-xl border border-slate-200 bg-slate-900 text-slate-100 text-xs p-4 overflow-auto max-h-[32rem]">
          {JSON.stringify(data, null, 2)}
        </pre>
      )}
    </div>
  );
}
