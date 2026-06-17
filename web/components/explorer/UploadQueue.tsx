/* eslint-disable @next/next/no-img-element */
"use client";

import { ArrowClockwise, CheckCircle, Spinner, WarningCircle, X } from "@phosphor-icons/react";

export type QueueStatus = "pending" | "uploading" | "success" | "error";

export interface QueueItem {
  id: string;
  name: string;
  previewUrl?: string;
  status: QueueStatus;
  error?: string;
}

export function UploadQueue({
  items,
  onClose,
  onRetry,
}: {
  items: QueueItem[];
  onClose: () => void;
  onRetry: (id: string) => void;
}) {
  if (items.length === 0) return null;
  const done = items.every((i) => i.status === "success" || i.status === "error");
  const ok = items.filter((i) => i.status === "success").length;

  return (
    <div className="fixed bottom-4 right-4 z-40 w-80 overflow-hidden rounded-2xl border border-zinc-200 bg-white shadow-[0_16px_40px_-12px_rgba(0,0,0,0.3)]">
      <div className="flex items-center justify-between border-b border-zinc-100 px-4 py-2.5">
        <span className="text-sm font-semibold tracking-tight">
          Tải lên {ok}/{items.length}
        </span>
        {done && (
          <button onClick={onClose} aria-label="Đóng" className="rounded-md p-1 text-zinc-400 hover:bg-zinc-100 hover:text-zinc-700">
            <X size={16} />
          </button>
        )}
      </div>
      <ul className="max-h-72 divide-y divide-zinc-50 overflow-y-auto scroll-thin">
        {items.map((it) => (
          <li key={it.id} className="flex items-center gap-3 px-4 py-2.5">
            <div className="h-9 w-9 shrink-0 overflow-hidden rounded bg-zinc-100">
              {it.previewUrl && <img src={it.previewUrl} alt="" className="h-full w-full object-cover" />}
            </div>
            <div className="min-w-0 flex-1">
              <p className="truncate text-xs font-medium text-zinc-700">{it.name}</p>
              {it.status === "error" && <p className="truncate text-[11px] text-rose-600">{it.error}</p>}
              {it.status === "pending" && <p className="text-[11px] text-zinc-400">Đang chờ</p>}
              {it.status === "uploading" && <p className="text-[11px] text-blue-600">Đang tải…</p>}
            </div>
            <div className="shrink-0">
              {it.status === "success" && <CheckCircle size={20} weight="fill" className="text-emerald-500" />}
              {it.status === "uploading" && <Spinner size={18} className="animate-spin text-blue-500" />}
              {it.status === "pending" && <Spinner size={18} className="text-zinc-300" />}
              {it.status === "error" && (
                <button onClick={() => onRetry(it.id)} title="Thử lại" className="flex items-center gap-1 text-rose-600 hover:text-rose-700">
                  <WarningCircle size={18} weight="fill" />
                  <ArrowClockwise size={14} />
                </button>
              )}
            </div>
          </li>
        ))}
      </ul>
    </div>
  );
}
