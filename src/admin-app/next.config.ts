import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  output: "standalone",
  // Allow calling the .NET API from server components
  experimental: {
    serverActions: {
      bodySizeLimit: "2mb",
    },
  },
};

export default nextConfig;
