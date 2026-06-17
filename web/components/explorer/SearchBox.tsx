/* eslint-disable @next/next/no-img-element */
"use client";

import { useEffect, useRef, useState } from "react";
import { Folder as FolderIcon, MagnifyingGlass, X } from "@phosphor-icons/react";
import { useSearch } from "@/lib/hooks";
import { useUiStore } from "@/store/uiStore";
import type { ImageDto, ImageHit } from "@/lib/types";

function hitToImage(h: ImageHit): ImageDto {
  return { id: h.id, name: h.name, url: h.url, thumbUrl: h.thumbUrl, width: h.width, height: h.height };
}

export function SearchBox({ onOpenImage }: { onOpenImage: (img: ImageDto) => void }) {
  const navigateTo = useUiStore((s) => s.navigateTo);
  const [text, setText] = useState("");
  const [debounced, setDebounced] = useState("");
  const [open, setOpen] = useState(false);
  const boxRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const t = setTimeout(() => setDebounced(text), 250);
    return () => clearTimeout(t);
  }, [text]);

  useEffect(() => {
    const onClick = (e: MouseEvent) => {
      if (boxRef.current && !boxRef.current.contains(e.target as Node)) setOpen(false);
    };
    window.addEventListener("mousedown", onClick);
    return () => window.removeEventListener("mousedown", onClick);
  }, []);

  const { data, isFetching } = useSearch(debounced);
  const folders = data?.folders ?? [];
  const images = data?.images ?? [];
  const hasResults = folders.length > 0 || images.length > 0;
  const showPanel = open && debounced.trim().length >= 1;

  function reset() {
    setText("");
    setDebounced("");
    setOpen(false);
  }

  return (
    <div ref={boxRef} className="relative w-full sm:w-64">
      <div className="flex items-center gap-1.5 rounded-lg border border-zinc-200 bg-white px-2.5 py-1.5 focus-within:border-blue-500 focus-within:ring-2 focus-within:ring-blue-500/20">
        <MagnifyingGlass size={15} className="shrink-0 text-zinc-400" />
        <input
          value={text}
          onChange={(e) => {
            setText(e.target.value);
            setOpen(true);
          }}
          onFocus={() => setOpen(true)}
          placeholder="Tìm thư mục, ảnh…"
          className="w-full bg-transparent text-sm outline-none placeholder:text-zinc-400"
        />
        {text && (
          <button onClick={reset} aria-label="Xóa" className="shrink-0 text-zinc-400 hover:text-zinc-600">
            <X size={14} />
          </button>
        )}
      </div>

      {showPanel && (
        <div className="absolute left-0 right-0 top-full z-50 mt-1 max-h-[60vh] overflow-y-auto rounded-xl border border-zinc-200 bg-white py-1 shadow-[0_12px_32px_-8px_rgba(0,0,0,0.25)] scroll-thin">
          {isFetching && !hasResults && <p className="px-3 py-2 text-sm text-zinc-400">Đang tìm…</p>}
          {!isFetching && !hasResults && <p className="px-3 py-2 text-sm text-zinc-400">Không có kết quả.</p>}

          {folders.length > 0 && (
            <div className="px-3 pb-1 pt-2 text-[11px] font-medium uppercase tracking-wide text-zinc-400">
              Thư mục
            </div>
          )}
          {folders.map((f) => (
            <button
              key={f.id}
              onClick={() => {
                navigateTo(f.id);
                reset();
              }}
              className="flex w-full items-center gap-2 px-3 py-2 text-left text-sm transition-colors hover:bg-zinc-100"
            >
              <FolderIcon size={16} weight="fill" className="shrink-0 text-amber-400" />
              <span className="truncate">{f.name}</span>
            </button>
          ))}

          {images.length > 0 && (
            <div className="px-3 pb-1 pt-2 text-[11px] font-medium uppercase tracking-wide text-zinc-400">
              Ảnh
            </div>
          )}
          {images.map((img) => (
            <button
              key={img.id}
              onClick={() => {
                onOpenImage(hitToImage(img));
                reset();
              }}
              className="flex w-full items-center gap-2 px-3 py-2 text-left text-sm transition-colors hover:bg-zinc-100"
            >
              <img src={img.thumbUrl} alt="" className="h-7 w-7 shrink-0 rounded object-cover" loading="lazy" />
              <span className="truncate">{img.name}</span>
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
