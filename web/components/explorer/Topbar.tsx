"use client";

import Link from "next/link";
import {
  ArrowLeft,
  ArrowRight,
  FolderPlus,
  List as ListIcon,
  ShieldCheck,
  SignIn,
  SignOut,
  UploadSimple,
} from "@phosphor-icons/react";
import { selectCanBack, selectCanForward, selectCurrentFolderId, useUiStore } from "@/store/uiStore";
import { Breadcrumb } from "./Breadcrumb";
import { SearchBox } from "./SearchBox";

export function Topbar({
  isAdmin,
  onNewFolder,
  onUpload,
  onLogout,
}: {
  isAdmin: boolean;
  onNewFolder: () => void;
  onUpload: () => void;
  onLogout: () => void;
}) {
  const { back, forward, setDrawerOpen } = useUiStore();
  const canBack = useUiStore(selectCanBack);
  const canForward = useUiStore(selectCanForward);
  const current = useUiStore(selectCurrentFolderId);
  const atRoot = current === null;

  const navBtn = "rounded-lg p-1.5 text-zinc-600 transition-colors enabled:hover:bg-zinc-200/70 disabled:opacity-30";

  return (
    <header className="flex flex-col gap-2 border-b border-zinc-200 bg-white px-3 py-2 md:flex-row md:items-center">
      <div className="flex items-center gap-1">
        <button onClick={() => setDrawerOpen(true)} className="rounded-lg p-1.5 text-zinc-600 hover:bg-zinc-200/70 md:hidden" aria-label="Mở cây thư mục">
          <ListIcon size={18} />
        </button>
        <button onClick={back} disabled={!canBack} className={navBtn} aria-label="Quay lại" title="Quay lại">
          <ArrowLeft size={18} />
        </button>
        <button onClick={forward} disabled={!canForward} className={navBtn} aria-label="Tiến tới" title="Tiến tới">
          <ArrowRight size={18} />
        </button>
      </div>

      <div className="min-w-0 flex-1 rounded-lg bg-zinc-100 px-2 py-1">
        <Breadcrumb folderId={current} />
      </div>

      <SearchBox />

      <div className="flex items-center gap-2">
        {isAdmin ? (
          <>
            <span className="hidden items-center gap-1 rounded-full bg-emerald-50 px-2.5 py-1 text-xs font-medium text-emerald-700 sm:inline-flex">
              <ShieldCheck size={14} weight="fill" /> Admin
            </span>
            <button
              onClick={onUpload}
              disabled={atRoot}
              title={atRoot ? "Chọn một thư mục để tải ảnh lên" : "Tải ảnh lên"}
              className="inline-flex items-center gap-1.5 rounded-lg bg-zinc-900 px-3 py-1.5 text-sm font-medium text-white transition-all hover:bg-zinc-800 active:scale-[0.99] disabled:opacity-40"
            >
              <UploadSimple size={16} /> Tải lên
            </button>
            <button
              onClick={onNewFolder}
              className="inline-flex items-center gap-1.5 rounded-lg border border-zinc-300 px-3 py-1.5 text-sm font-medium text-zinc-700 transition-colors hover:bg-zinc-100"
            >
              <FolderPlus size={16} /> Thư mục
            </button>
            <button onClick={onLogout} title="Đăng xuất" className="rounded-lg p-1.5 text-zinc-500 transition-colors hover:bg-zinc-100 hover:text-zinc-800">
              <SignOut size={18} />
            </button>
          </>
        ) : (
          <Link
            href="/login"
            className="inline-flex items-center gap-1.5 rounded-lg border border-zinc-300 px-3 py-1.5 text-sm font-medium text-zinc-700 transition-colors hover:bg-zinc-100"
          >
            <SignIn size={16} /> Đăng nhập admin
          </Link>
        )}
      </div>
    </header>
  );
}
