"use client";

import { useDroppable } from "@dnd-kit/core";
import { CaretDown, CaretRight, Folder as FolderIcon, House } from "@phosphor-icons/react";
import { useContents } from "@/lib/hooks";
import { selectCurrentFolderId, useUiStore } from "@/store/uiStore";
import type { SubFolder } from "@/lib/types";

function TreeNode({ folder, depth }: { folder: SubFolder; depth: number }) {
  const expanded = useUiStore((s) => !!s.expanded[folder.id]);
  const toggleExpand = useUiStore((s) => s.toggleExpand);
  const navigateTo = useUiStore((s) => s.navigateTo);
  const current = useUiStore(selectCurrentFolderId);

  const { data } = useContents(folder.id, "name", "asc", expanded);
  const hasChildren = folder.subFolderCount > 0;
  const drop = useDroppable({ id: `tree:${folder.id}`, data: { type: "folder", id: folder.id } });

  return (
    <div>
      <div
        ref={drop.setNodeRef}
        className={`flex items-center gap-1 rounded-md py-1 pr-2 text-sm transition-colors ${
          current === folder.id ? "bg-blue-50 text-blue-700" : "hover:bg-zinc-100 text-zinc-700"
        } ${drop.isOver ? "ring-1 ring-blue-400" : ""}`}
        style={{ paddingLeft: depth * 14 + 4 }}
      >
        <button
          onClick={() => hasChildren && toggleExpand(folder.id)}
          className={`flex h-4 w-4 items-center justify-center text-zinc-400 ${hasChildren ? "" : "invisible"}`}
          aria-label={expanded ? "Thu gọn" : "Mở rộng"}
        >
          {expanded ? <CaretDown size={12} /> : <CaretRight size={12} />}
        </button>
        <button onClick={() => navigateTo(folder.id)} className="flex min-w-0 flex-1 items-center gap-1.5 text-left">
          <FolderIcon size={15} weight="fill" className="shrink-0 text-amber-400" />
          <span className="truncate">{folder.name}</span>
        </button>
      </div>

      {expanded &&
        (data?.subFolders ?? []).map((sf) => <TreeNode key={sf.id} folder={sf} depth={depth + 1} />)}
    </div>
  );
}

export function FolderTree() {
  const navigateTo = useUiStore((s) => s.navigateTo);
  const current = useUiStore(selectCurrentFolderId);
  const { data, isLoading } = useContents(null, "name", "asc");
  const rootDrop = useDroppable({ id: "tree:root", data: { type: "root" } });

  return (
    <div className="flex h-full flex-col overflow-y-auto p-2 scroll-thin">
      <button
        ref={rootDrop.setNodeRef}
        onClick={() => navigateTo(null)}
        className={`mb-1 flex items-center gap-1.5 rounded-md px-2 py-1.5 text-sm font-medium transition-colors ${
          current === null ? "bg-blue-50 text-blue-700" : "text-zinc-700 hover:bg-zinc-100"
        } ${rootDrop.isOver ? "ring-1 ring-blue-400" : ""}`}
      >
        <House size={15} weight={current === null ? "fill" : "regular"} />
        Root
      </button>

      {isLoading && <p className="px-2 text-xs text-zinc-400">Đang tải…</p>}
      {(data?.subFolders ?? []).map((sf) => (
        <TreeNode key={sf.id} folder={sf} depth={0} />
      ))}
    </div>
  );
}
