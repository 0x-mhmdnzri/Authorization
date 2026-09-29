import "next-auth";
import "next-auth/jwt";

declare module "next-auth" {
  interface Session {
    accessToken?: string;
    refreshToken?: string;
    accessTokenExpires?: number;
    error?: string;
    user: {
      id: string;
      email: string;
      name?: string | null;
      roles: string[];
      isGod: boolean;
      department?: string;
      clearance?: string;
    };
  }

  interface User {
    id: string;
    email: string;
    name?: string | null;
    accessToken: string;
    refreshToken: string;
    accessTokenExpires: number;
    refreshTokenExpires: number;
    roles: string[];
    isGod: boolean;
    department?: string;
    clearance?: string;
  }
}

declare module "next-auth/jwt" {
  interface JWT {
    accessToken?: string;
    refreshToken?: string;
    accessTokenExpires?: number;
    refreshTokenExpires?: number;
    roles?: string[];
    isGod?: boolean;
    department?: string;
    clearance?: string;
    error?: string;
  }
}
