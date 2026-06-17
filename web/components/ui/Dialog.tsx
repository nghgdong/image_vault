"use client";

import { useEffect } from "react";
import { X } from "@phosphor-icons/react";

export function Dialog({
  open,
  onClose,
  title,
  children,
}: {
  open: boolean;
  onClose: () => void;
  title: string;
  children: React.ReactNode;
}) {
  useEffect(() => {
    if (!open) return;
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") onClose();
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [open, onClose]);

  if (!open) return null;

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-zinc-900/40 p-4 backdrop-blur-[2px]"
      onMouseDown={onClose}
    >
      <div
        className="w-full max-w-md rounded-2xl border border-zinc-200 bg-white shadow-[0_20px_50px_-15px_rgba(0,0,0,0.25)]"
        onMouseDown={(e) => e.stopPropagation()}
      >
        <div className="flex items-center justify-between border-b border-zinc-100 px-5 py-3.5">
          <h3 className="text-sm font-semibold tracking-tight">{title}</h3>
          <button
            onClick={onClose}
            aria-label="Đóng"
            className="rounded-md p-1 text-zinc-400 transition-colors hover:bg-zinc-100 hover:text-zinc-700"
          >
            <X size={18} />
          </button>
        </div>
        <div className="p-5">{children}</div>
      </div>
    </div>
  );
}
