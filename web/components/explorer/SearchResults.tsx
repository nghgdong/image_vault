/* eslint-disable @next/next/no-img-element */
"use client";

import { Folder as FolderIcon, MagnifyingGlass, X } from "@phosphor-icons/react";
import { useSearch } from "@/lib/hooks";
import { useUiStore } from "@/store/uiStore";
import type { ImageDto, ImageHit } from "@/lib/types";
import { Skeleton } from "@/components/ui/Skeleton";

function hitToImage(h: ImageHit): ImageDto {
  return { id: h.id, name: h.name, url: h.url, thumbUrl: h.thumbUrl, width: h.width, height: h.height };
}

export function SearchResults({
  query,
  onOpenImage,
}: {
  query: string;
  onOpenImage: (img: ImageDto) => void;
}) {
  const navigateTo = useUiStore((s) => s.navigateTo);
  const clearSearch = useUiStore((s) => s.clearSearch);
  const { data, isFetching } = useSearch(query);

  const folders = data?.folders ?? [];
  const images = data?.images ?? [];
  const total = folders.length + images.length;
  const empty = !isFetching && total === 0;

  return (
    <div className="flex h-full flex-col">
      <div className="flex items-center justify-between gap-3 border-b border-zinc-200 px-4 py-2">
        <div className="flex min-w-0 items-center gap-2 text-sm">
          <MagnifyingGlass size={16} className="shrink-0 text-zinc-400" />
          <span className="truncate text-zinc-600">
            Kết quả cho <span className="font-medium text-zinc-900">“{query}”</span>
            {!isFetching && <span className="text-zinc-400"> · {total} mục</span>}
          </span>
        </div>
        <button
          onClick={clearSearch}
          className="inline-flex shrink-0 items-center gap-1 rounded-lg border border-zinc-200 px-2.5 py-1.5 text-sm text-zinc-600 transition-colors hover:bg-zinc-100"
        >
          <X size={14} /> Xóa tìm kiếm
        </button>
      </div>

      <div className="flex-1 overflow-y-auto p-4 scroll-thin">
        {isFetching && total === 0 && (
          <div className="grid grid-cols-[repeat(auto-fill,minmax(104px,1fr))] gap-2">
            {Array.from({ length: 8 }).map((_, i) => (
              <Skeleton key={i} className="aspect-square" />
            ))}
          </div>
        )}

        {empty && (
          <div className="flex h-full flex-col items-center justify-center gap-2 text-zinc-400">
            <MagnifyingGlass size={44} weight="thin" />
            <p className="text-sm">Không tìm thấy mục nào khớp “{query}”.</p>
          </div>
        )}

        {folders.length > 0 && (
          <>
            <h3 className="mb-2 text-xs font-medium uppercase tracking-wide text-zinc-400">Thư mục</h3>
            <div className="mb-6 grid grid-cols-[repeat(auto-fill,minmax(104px,1fr))] gap-1">
              {folders.map((f) => (
                <button
                  key={f.id}
                  onClick={() => navigateTo(f.id)}
                  className="flex select-none flex-col items-center gap-1.5 rounded-xl p-3 text-center ring-1 ring-transparent transition-colors hover:bg-zinc-100"
                >
                  <FolderIcon size={56} weight="fill" className="text-amber-400" />
                  <span className="line-clamp-2 w-full text-xs leading-snug text-zinc-700">{f.name}</span>
                </button>
              ))}
            </div>
          </>
        )}

        {images.length > 0 && (
          <>
            <h3 className="mb-2 text-xs font-medium uppercase tracking-wide text-zinc-400">Ảnh</h3>
            <div className="grid grid-cols-[repeat(auto-fill,minmax(104px,1fr))] gap-1">
              {images.map((img) => (
                <button
                  key={img.id}
                  onDoubleClick={() => onOpenImage(hitToImage(img))}
                  onClick={() => onOpenImage(hitToImage(img))}
                  className="group flex select-none flex-col gap-1.5 rounded-xl p-2 ring-1 ring-transparent transition-colors hover:bg-zinc-100"
                >
                  <div className="relative aspect-square w-full overflow-hidden rounded-lg bg-zinc-100">
                    <img
                      src={img.thumbUrl}
                      alt={img.name}
                      loading="lazy"
                      className="h-full w-full object-cover transition-transform duration-300 group-hover:scale-[1.03]"
                    />
                  </div>
                  <span className="line-clamp-1 px-1 text-left text-xs text-zinc-700">{img.name}</span>
                </button>
              ))}
            </div>
          </>
        )}
      </div>
    </div>
  );
}
