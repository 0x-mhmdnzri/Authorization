import type { NextAuthOptions } from "next-auth";
import CredentialsProvider from "next-auth/providers/credentials";
import { backendLogin, backendRefresh } from "./api";

async function refreshAccessToken(token: {
  refreshToken?: string;
  accessToken?: string;
  accessTokenExpires?: number;
  refreshTokenExpires?: number;
  [key: string]: unknown;
}) {
  try {
    if (!token.refreshToken) throw new Error("No refresh token");

    const refreshed = await backendRefresh(token.refreshToken);

    return {
      ...token,
      accessToken: refreshed.accessToken,
      accessTokenExpires: new Date(refreshed.accessTokenExpiration).getTime(),
      refreshToken: refreshed.refreshToken,
      refreshTokenExpires: new Date(refreshed.refreshTokenExpiration).getTime(),
      error: undefined,
    };
  } catch {
    return { ...token, error: "RefreshAccessTokenError" };
  }
}

export const authOptions: NextAuthOptions = {
  providers: [
    CredentialsProvider({
      id: "credentials",
      name: "Credentials",
      credentials: {
        email: { label: "Email", type: "email" },
        password: { label: "Password", type: "password" },
      },
      async authorize(credentials) {
        if (!credentials?.email || !credentials?.password) return null;

        try {
          const data = await backendLogin(credentials.email, credentials.password);

          return {
            id: data.userId,
            email: data.email,
            name: data.email,
            accessToken: data.accessToken,
            refreshToken: data.refreshToken,
            accessTokenExpires: new Date(data.accessTokenExpiration).getTime(),
            refreshTokenExpires: new Date(data.refreshTokenExpiration).getTime(),
            roles: data.roles ?? [],
            isGod: data.isGod ?? false,
          };
        } catch {
          return null;
        }
      },
    }),
  ],
  session: {
    strategy: "jwt",
    maxAge: 7 * 24 * 60 * 60, // align with refresh token lifetime
  },
  pages: {
    signIn: "/login",
  },
  callbacks: {
    async jwt({ token, user }) {
      // Initial sign-in
      if (user) {
        return {
          ...token,
          accessToken: user.accessToken,
          refreshToken: user.refreshToken,
          accessTokenExpires: user.accessTokenExpires,
          refreshTokenExpires: user.refreshTokenExpires,
          roles: user.roles,
          isGod: user.isGod,
          department: user.department,
          clearance: user.clearance,
          sub: user.id,
        };
      }

      // Access token still valid
      if (token.accessTokenExpires && Date.now() < token.accessTokenExpires - 60_000) {
        return token;
      }

      // Access expired → refresh
      return refreshAccessToken(token);
    },
    async session({ session, token }) {
      session.accessToken = token.accessToken;
      session.refreshToken = token.refreshToken;
      session.accessTokenExpires = token.accessTokenExpires;
      session.error = token.error;

      if (session.user) {
        session.user.id = (token.sub as string) || "";
        session.user.roles = token.roles || [];
        session.user.isGod = token.isGod || false;
        session.user.department = token.department;
        session.user.clearance = token.clearance;
      }

      return session;
    },
  },
  secret: process.env.NEXTAUTH_SECRET,
};
