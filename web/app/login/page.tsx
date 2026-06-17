"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { ArrowLeft, LockKey } from "@phosphor-icons/react";
import Link from "next/link";
import { api, ApiError } from "@/lib/api";
import { useAuthStore } from "@/store/authStore";

export default function LoginPage() {
  const router = useRouter();
  const login = useAuthStore((s) => s.login);
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      const res = await api.login(username, password);
      login(res.token);
      router.push("/");
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Đăng nhập thất bại.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <main className="grid min-h-[100dvh] grid-cols-1 md:grid-cols-2">
      <div className="hidden flex-col justify-between bg-zinc-900 p-12 text-zinc-100 md:flex">
        <span className="font-mono text-sm tracking-tight text-zinc-400">image-vault</span>
        <div>
          <h1 className="text-4xl font-semibold tracking-tighter">Kho ảnh nội bộ</h1>
          <p className="mt-3 max-w-[42ch] leading-relaxed text-zinc-400">
            Quản lý cây thư mục và ảnh. Khu vực quản trị yêu cầu đăng nhập; phần duyệt ảnh mở công khai.
          </p>
        </div>
        <span className="text-xs text-zinc-500">Ảnh lưu trên freeimage.host — công khai vĩnh viễn.</span>
      </div>

      <div className="flex items-center justify-center p-6">
        <div className="w-full max-w-sm">
          <Link
            href="/"
            className="mb-8 inline-flex items-center gap-1.5 text-sm text-zinc-500 transition-colors hover:text-zinc-900"
          >
            <ArrowLeft size={16} /> Quay lại kho ảnh
          </Link>

          <div className="mb-6 flex items-center gap-2">
            <span className="flex h-9 w-9 items-center justify-center rounded-lg bg-zinc-900 text-white">
              <LockKey size={18} weight="bold" />
            </span>
            <h2 className="text-xl font-semibold tracking-tight">Đăng nhập quản trị</h2>
          </div>

          <form onSubmit={onSubmit} className="flex flex-col gap-4">
            <div className="flex flex-col gap-1.5">
              <label htmlFor="username" className="text-sm font-medium text-zinc-700">
                Tên đăng nhập
              </label>
              <input
                id="username"
                value={username}
                onChange={(e) => setUsername(e.target.value)}
                autoComplete="username"
                required
                className="rounded-lg border border-zinc-300 bg-white px-3 py-2 text-sm outline-none transition-colors focus:border-blue-500 focus:ring-2 focus:ring-blue-500/20"
              />
            </div>
            <div className="flex flex-col gap-1.5">
              <label htmlFor="password" className="text-sm font-medium text-zinc-700">
                Mật khẩu
              </label>
              <input
                id="password"
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                autoComplete="current-password"
                required
                className="rounded-lg border border-zinc-300 bg-white px-3 py-2 text-sm outline-none transition-colors focus:border-blue-500 focus:ring-2 focus:ring-blue-500/20"
              />
            </div>

            {error && (
              <p className="rounded-lg bg-rose-50 px-3 py-2 text-sm text-rose-700" role="alert">
                {error}
              </p>
            )}

            <button
              type="submit"
              disabled={loading}
              className="mt-2 rounded-lg bg-zinc-900 px-4 py-2.5 text-sm font-medium text-white transition-all hover:bg-zinc-800 active:scale-[0.99] disabled:opacity-60"
            >
              {loading ? "Đang đăng nhập…" : "Đăng nhập"}
            </button>
          </form>
        </div>
      </div>
    </main>
  );
}
