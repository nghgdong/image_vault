"use client";

import { useMemo, useState } from "react";
import { FolderOpen, UploadSimple } from "@phosphor-icons/react";
import { useContents } from "@/lib/hooks";
import { useUiStore, type SelectedItem } from "@/store/uiStore";
import type { ImageDto } from "@/lib/types";
import { Skeleton } from "@/components/ui/Skeleton";
import { Toolbar } from "./Toolbar";
import { FolderCell, ImageCell } from "./items";

export type CtxTarget =
  | { type: "folder"; id: string; name: string }
  | { type: "image"; image: ImageDto }
  | null;

export function ContentPane({
  folderId,
  isAdmin,
  onActivateImage,
  onContextMenu,
  onFilesDropped,
}: {
  folderId: string | null;
  isAdmin: boolean;
  onActivateImage: (images: ImageDto[], index: number) => void;
  onContextMenu: (e: React.MouseEvent, target: CtxTarget) => void;
  onFilesDropped: (files: File[]) => void;
}) {
  const { sort, order, viewMode, selection, setSelection, clearSelection } = useUiStore();
  const { data, isLoading, isError, error } = useContents(folderId, sort, order);
  const [dragOver, setDragOver] = useState(false);

  const ordered: SelectedItem[] = useMemo(() => {
    const f = (data?.subFolders ?? []).map((x) => ({ type: "folder" as const, id: x.id }));
    const i = (data?.images ?? []).map((x) => ({ type: "image" as const, id: x.id }));
    return [...f, ...i];
  }, [data]);

  const isSel = (type: "folder" | "image", id: string) =>
    selection.some((s) => s.type === type && s.id === id);

  function selectClick(e: React.MouseEvent, item: SelectedItem) {
    e.stopPropagation();
    if (e.metaKey || e.ctrlKey) {
      setSelection(
        isSel(item.type, item.id)
          ? selection.filter((s) => !(s.type === item.type && s.id === item.id))
          : [...selection, item],
      );
    } else if (e.shiftKey && selection.length) {
      const anchor = selection[selection.length - 1];
      const a = ordered.findIndex((o) => o.type === anchor.type && o.id === anchor.id);
      const b = ordered.findIndex((o) => o.type === item.type && o.id === item.id);
      if (a >= 0 && b >= 0) setSelection(ordered.slice(Math.min(a, b), Math.max(a, b) + 1));
    } else {
      setSelection([item]);
    }
  }

  function onDrop(e: React.DragEvent) {
    e.preventDefault();
    setDragOver(false);
    if (!isAdmin) return;
    const files = Array.from(e.dataTransfer.files).filter((f) => f.type.startsWith("image/") || true);
    if (files.length) onFilesDropped(files);
  }

  const images = data?.images ?? [];
  const subFolders = data?.subFolders ?? [];
  const empty = !isLoading && subFolders.length === 0 && images.length === 0;

  return (
    <div className="flex h-full flex-col">
      <Toolbar selectedCount={selection.length} />

      <div
        className="relative flex-1 overflow-y-auto p-4 scroll-thin"
        onClick={() => clearSelection()}
        onContextMenu={(e) => {
          e.preventDefault();
          onContextMenu(e, null);
        }}
        onDragOver={(e) => {
          if (!isAdmin) return;
          if (Array.from(e.dataTransfer.types).includes("Files")) {
            e.preventDefault();
            setDragOver(true);
          }
        }}
        onDragLeave={() => setDragOver(false)}
        onDrop={onDrop}
      >
        {isAdmin && dragOver && (
          <div className="pointer-events-none absolute inset-3 z-10 flex flex-col items-center justify-center gap-2 rounded-2xl border-2 border-dashed border-blue-400 bg-blue-50/80 text-blue-600">
            <UploadSimple size={32} />
            <p className="text-sm font-medium">Thả ảnh vào đây để tải lên</p>
          </div>
        )}

        {isLoading && (
          <div className="grid grid-cols-[repeat(auto-fill,minmax(104px,1fr))] gap-2">
            {Array.from({ length: 12 }).map((_, i) => (
              <Skeleton key={i} className="aspect-square" />
            ))}
          </div>
        )}

        {isError && (
          <div className="flex h-full flex-col items-center justify-center gap-1 text-center text-zinc-500">
            <p className="text-sm">Không tải được nội dung.</p>
            <p className="text-xs text-zinc-400">{(error as Error)?.message}</p>
          </div>
        )}

        {empty && (
          <div className="flex h-full flex-col items-center justify-center gap-2 text-zinc-400">
            <FolderOpen size={48} weight="thin" />
            <p className="text-sm">Thư mục trống</p>
            {isAdmin && <p className="text-xs">Kéo-thả ảnh vào đây hoặc dùng nút Tải lên.</p>}
          </div>
        )}

        {!isLoading && !empty && viewMode === "icon" && (
          <div className="grid grid-cols-[repeat(auto-fill,minmax(104px,1fr))] gap-1">
            {subFolders.map((f) => (
              <FolderCell
                key={f.id}
                variant="grid"
                folder={f}
                draggable={isAdmin}
                selected={isSel("folder", f.id)}
                onClick={(e) => selectClick(e, { type: "folder", id: f.id })}
                onDoubleClick={() => useUiStore.getState().navigateTo(f.id)}
                onContextMenu={(e) => {
                  e.preventDefault();
                  e.stopPropagation();
                  if (!isSel("folder", f.id)) setSelection([{ type: "folder", id: f.id }]);
                  onContextMenu(e, { type: "folder", id: f.id, name: f.name });
                }}
              />
            ))}
            {images.map((img, idx) => (
              <ImageCell
                key={img.id}
                variant="grid"
                image={img}
                draggable={isAdmin}
                selected={isSel("image", img.id)}
                onClick={(e) => selectClick(e, { type: "image", id: img.id })}
                onDoubleClick={() => onActivateImage(images, idx)}
                onContextMenu={(e) => {
                  e.preventDefault();
                  e.stopPropagation();
                  if (!isSel("image", img.id)) setSelection([{ type: "image", id: img.id }]);
                  onContextMenu(e, { type: "image", image: img });
                }}
              />
            ))}
          </div>
        )}

        {!isLoading && !empty && viewMode === "list" && (
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-zinc-200 text-left text-xs font-medium text-zinc-500">
                <th className="px-3 py-2">Tên</th>
                <th className="px-3 py-2 text-right">Kích thước</th>
                <th className="px-3 py-2 text-right">Loại</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-zinc-50">
              {subFolders.map((f) => (
                <FolderCell
                  key={f.id}
                  variant="list"
                  folder={f}
                  draggable={isAdmin}
                  selected={isSel("folder", f.id)}
                  onClick={(e) => selectClick(e, { type: "folder", id: f.id })}
                  onDoubleClick={() => useUiStore.getState().navigateTo(f.id)}
                  onContextMenu={(e) => {
                    e.preventDefault();
                    e.stopPropagation();
                    if (!isSel("folder", f.id)) setSelection([{ type: "folder", id: f.id }]);
                    onContextMenu(e, { type: "folder", id: f.id, name: f.name });
                  }}
                />
              ))}
              {images.map((img, idx) => (
                <ImageCell
                  key={img.id}
                  variant="list"
                  image={img}
                  draggable={isAdmin}
                  selected={isSel("image", img.id)}
                  onClick={(e) => selectClick(e, { type: "image", id: img.id })}
                  onDoubleClick={() => onActivateImage(images, idx)}
                  onContextMenu={(e) => {
                    e.preventDefault();
                    e.stopPropagation();
                    if (!isSel("image", img.id)) setSelection([{ type: "image", id: img.id }]);
                    onContextMenu(e, { type: "image", image: img });
                  }}
                />
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}
