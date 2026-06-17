/* eslint-disable @next/next/no-img-element */
"use client";

import { useCallback, useEffect, useState } from "react";
import { CaretLeft, CaretRight, Copy, DownloadSimple, X } from "@phosphor-icons/react";
import type { ImageDto } from "@/lib/types";

export function Lightbox({
  images,
  index,
  onIndex,
  onClose,
}: {
  images: ImageDto[];
  index: number;
  onIndex: (i: number) => void;
  onClose: () => void;
}) {
  const [copied, setCopied] = useState(false);
  const img = images[index];

  const prev = useCallback(() => onIndex((index - 1 + images.length) % images.length), [index, images.length, onIndex]);
  const next = useCallback(() => onIndex((index + 1) % images.length), [index, images.length, onIndex]);

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") onClose();
      if (e.key === "ArrowLeft") prev();
      if (e.key === "ArrowRight") next();
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [onClose, prev, next]);

  if (!img) return null;

  async function copyLink() {
    await navigator.clipboard.writeText(img.url);
    setCopied(true);
    setTimeout(() => setCopied(false), 1500);
  }

  return (
    <div className="fixed inset-0 z-50 flex flex-col bg-zinc-950/95" onClick={onClose}>
      <div className="flex items-center justify-between p-4 text-zinc-200" onClick={(e) => e.stopPropagation()}>
        <div className="min-w-0">
          <p className="truncate text-sm font-medium">{img.name}</p>
          {img.width && img.height && (
            <p className="font-mono text-xs text-zinc-500">
              {img.width}×{img.height} · {index + 1}/{images.length}
            </p>
          )}
        </div>
        <div className="flex items-center gap-2">
          <button onClick={copyLink} className="flex items-center gap-1.5 rounded-lg bg-white/10 px-3 py-1.5 text-sm transition-colors hover:bg-white/20">
            <Copy size={16} /> {copied ? "Đã copy" : "Copy link"}
          </button>
          <a href={img.url} target="_blank" rel="noreferrer" download className="flex items-center gap-1.5 rounded-lg bg-white/10 px-3 py-1.5 text-sm transition-colors hover:bg-white/20">
            <DownloadSimple size={16} /> Tải
          </a>
          <button onClick={onClose} aria-label="Đóng" className="rounded-lg bg-white/10 p-1.5 transition-colors hover:bg-white/20">
            <X size={18} />
          </button>
        </div>
      </div>

      <div className="relative flex flex-1 items-center justify-center p-4" onClick={(e) => e.stopPropagation()}>
        {images.length > 1 && (
          <button onClick={prev} aria-label="Ảnh trước" className="absolute left-4 rounded-full bg-white/10 p-2 text-zinc-200 transition-colors hover:bg-white/20">
            <CaretLeft size={24} />
          </button>
        )}
        <img src={img.url} alt={img.name} className="max-h-full max-w-full rounded-lg object-contain" />
        {images.length > 1 && (
          <button onClick={next} aria-label="Ảnh sau" className="absolute right-4 rounded-full bg-white/10 p-2 text-zinc-200 transition-colors hover:bg-white/20">
            <CaretRight size={24} />
          </button>
        )}
      </div>
    </div>
  );
}
