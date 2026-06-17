/* eslint-disable @next/next/no-img-element */
"use client";

import { useDraggable, useDroppable } from "@dnd-kit/core";
import { Folder as FolderIcon } from "@phosphor-icons/react";
import type { ImageDto, SubFolder } from "@/lib/types";

function useMergedRef(...refs: Array<(node: HTMLElement | null) => void>) {
  return (node: HTMLElement | null) => refs.forEach((r) => r(node));
}

interface CommonProps {
  variant: "grid" | "list";
  selected: boolean;
  draggable: boolean;
  onClick: (e: React.MouseEvent) => void;
  onDoubleClick: () => void;
  onContextMenu: (e: React.MouseEvent) => void;
}

export function FolderCell({
  folder,
  ...p
}: CommonProps & { folder: SubFolder }) {
  const drag = useDraggable({ id: `folder:${folder.id}`, data: { type: "folder", id: folder.id }, disabled: !p.draggable });
  const drop = useDroppable({ id: `dropfolder:${folder.id}`, data: { type: "folder", id: folder.id } });
  const ref = useMergedRef(drag.setNodeRef, drop.setNodeRef);

  const ring = drop.isOver
    ? "ring-2 ring-blue-500 bg-blue-50"
    : p.selected
      ? "ring-2 ring-blue-500/70 bg-blue-50"
      : "ring-1 ring-transparent hover:bg-zinc-100";

  if (p.variant === "list") {
    return (
      <tr
        ref={ref as never}
        {...drag.attributes}
        {...drag.listeners}
        onClick={p.onClick}
        onDoubleClick={p.onDoubleClick}
        onContextMenu={p.onContextMenu}
        className={`cursor-default select-none ${p.selected ? "bg-blue-50" : "hover:bg-zinc-50"} ${drop.isOver ? "bg-blue-100" : ""}`}
      >
        <td className="flex items-center gap-2 px-3 py-2">
          <FolderIcon size={18} weight="fill" className="text-amber-400" />
          <span className="truncate">{folder.name}</span>
        </td>
        <td className="px-3 py-2 text-right font-mono text-xs text-zinc-500">
          {folder.subFolderCount} thư mục · {folder.imageCount} ảnh
        </td>
        <td className="px-3 py-2 text-right text-xs text-zinc-400">—</td>
      </tr>
    );
  }

  return (
    <button
      ref={ref as never}
      {...drag.attributes}
      {...drag.listeners}
      onClick={p.onClick}
      onDoubleClick={p.onDoubleClick}
      onContextMenu={p.onContextMenu}
      className={`flex select-none flex-col items-center gap-1.5 rounded-xl p-3 text-center transition-colors ${ring}`}
    >
      <FolderIcon size={56} weight="fill" className="text-amber-400" />
      <span className="line-clamp-2 w-full text-xs leading-snug text-zinc-700">{folder.name}</span>
      <span className="font-mono text-[10px] text-zinc-400">{folder.imageCount} ảnh</span>
    </button>
  );
}

export function ImageCell({
  image,
  ...p
}: CommonProps & { image: ImageDto }) {
  const drag = useDraggable({ id: `image:${image.id}`, data: { type: "image", id: image.id }, disabled: !p.draggable });

  if (p.variant === "list") {
    return (
      <tr
        ref={drag.setNodeRef as never}
        {...drag.attributes}
        {...drag.listeners}
        onClick={p.onClick}
        onDoubleClick={p.onDoubleClick}
        onContextMenu={p.onContextMenu}
        className={`cursor-default select-none ${p.selected ? "bg-blue-50" : "hover:bg-zinc-50"}`}
      >
        <td className="flex items-center gap-2 px-3 py-2">
          <img src={image.thumbUrl} alt={image.name} className="h-7 w-7 rounded object-cover" loading="lazy" />
          <span className="truncate">{image.name}</span>
        </td>
        <td className="px-3 py-2 text-right font-mono text-xs text-zinc-500">
          {image.width && image.height ? `${image.width}×${image.height}` : "—"}
        </td>
        <td className="px-3 py-2 text-right text-xs text-zinc-400">Ảnh</td>
      </tr>
    );
  }

  return (
    <button
      ref={drag.setNodeRef as never}
      {...drag.attributes}
      {...drag.listeners}
      onClick={p.onClick}
      onDoubleClick={p.onDoubleClick}
      onContextMenu={p.onContextMenu}
      className={`group flex select-none flex-col gap-1.5 rounded-xl p-2 transition-colors ${
        p.selected ? "ring-2 ring-blue-500 bg-blue-50" : "ring-1 ring-transparent hover:bg-zinc-100"
      }`}
    >
      <div className="relative aspect-square w-full overflow-hidden rounded-lg bg-zinc-100">
        <img
          src={image.thumbUrl}
          alt={image.name}
          loading="lazy"
          className="h-full w-full object-cover transition-transform duration-300 group-hover:scale-[1.03]"
        />
      </div>
      <span className="line-clamp-1 px-1 text-left text-xs text-zinc-700">{image.name}</span>
    </button>
  );
}
