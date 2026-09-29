export { default } from "next-auth/middleware";

export const config = {
  matcher: [
    "/dashboard/:path*",
    "/users/:path*",
    "/roles/:path*",
    "/resources/:path*",
    "/abac/:path*",
    "/pbac/:path*",
    "/purpose/:path*",
    "/radac/:path*",
    "/rebac/:path*",
    "/pac/:path*",
    "/cbac/:path*",
    "/rubac/:path*",
    "/profile/:path*",
  ],
};
