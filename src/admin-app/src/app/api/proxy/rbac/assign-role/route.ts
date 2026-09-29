import { getServerSession } from "next-auth";
import { NextRequest, NextResponse } from "next/server";
import { authOptions } from "@/lib/auth";
import { assignRole } from "@/lib/admin-api";

export async function POST(req: NextRequest) {
  const session = await getServerSession(authOptions);
  if (!session?.accessToken) return NextResponse.json({ message: "Unauthorized" }, { status: 401 });
  try {
    const body = await req.json();
    return NextResponse.json(await assignRole(session.accessToken, body));
  } catch (e) {
    return NextResponse.json({ message: e instanceof Error ? e.message : "Error" }, { status: 400 });
  }
}
