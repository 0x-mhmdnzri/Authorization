import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "Authorization Admin",
  description: "Admin panel for multi-model authorization demo",
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="en">
      <body className="min-h-screen antialiased">{children}</body>
    </html>
  );
}
