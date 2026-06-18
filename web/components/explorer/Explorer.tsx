"use client";

import { useEffect, useRef, useState } from "react";
import {
  DndContext,
  PointerSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
} from "@dnd-kit/core";
import { ArrowsOutSimple, Copy, FolderPlus, PencilSimple, Trash, UploadSimple } from "@phosphor-icons/react";
import { useMoveFolder, useMoveImage, useUploadBatch } from "@/lib/hooks";
import { ImageFileMax } from "@/lib/constants";
import type { ImageDto } from "@/lib/types";
import { useAuthStore } from "@/store/authStore";
import { selectCurrentFolderId, useUiStore } from "@/store/uiStore";
import { ContentPane, type CtxTarget } from "./ContentPane";
import { FolderTree } from "./FolderTree";
import { Topbar } from "./Topbar";
import { Lightbox } from "./Lightbox";
import { ContextMenu, type MenuItem } from "./ContextMenu";
import { NewFolderDialog, RenameDialog, DeleteConfirmDialog, type DeleteTarget, type RenameTarget } from "./dialogs";
import { UploadQueue, type QueueItem } from "./UploadQueue";

export function Explorer() {
  const hydrate = useAuthStore((s) => s.hydrate);
  const isAdmin = useAuthStore((s) => s.isAdmin);
  const logout = useAuthStore((s) => s.logout);
  const current = useUiStore(selectCurrentFolderId);
  const { drawerOpen, setDrawerOpen, navigateTo } = useUiStore();

  const moveImage = useMoveImage();
  const moveFolder = useMoveFolder();
  const upload = useUploadBatch();

  const [lightbox, setLightbox] = useState<{ images: ImageDto[]; index: number } | null>(null);
  const [ctx, setCtx] = useState<{ x: number; y: number; target: CtxTarget } | null>(null);
  const [newFolderOpen, setNewFolderOpen] = useState(false);
  const [renameTarget, setRenameTarget] = useState<RenameTarget>(null);
  const [deleteTarget, setDeleteTarget] = useState<DeleteTarget>(null);
  const [queue, setQueue] = useState<QueueItem[]>([]);
  const [toast, setToast] = useState<string | null>(null);

  const fileInputRef = useRef<HTMLInputElement>(null);
  const fileMap = useRef<Map<string, File>>(new Map());

  useEffect(() => hydrate(), [hydrate]);

  function showToast(msg: string) {
    setToast(msg);
    setTimeout(() => setToast(null), 3500);
  }

  // ---------- drag & drop move ----------
  const sensors = useSensors(useSensor(PointerSensor, { activationConstraint: { distance: 6 } }));

  async function onDragEnd(e: DragEndEvent) {
    const a = e.active.data.current as { type: "folder" | "image"; id: string } | undefined;
    const o = e.over?.data.current as { type: "folder" | "root"; id?: string } | undefined;
    if (!a || !o || !isAdmin) return;
    const targetFolderId = o.type === "root" ? null : o.id ?? null;

    try {
      if (a.type === "image") {
        if (!targetFolderId) return; // ảnh phải thuộc một folder thật
        await moveImage.mutateAsync({ id: a.id, folderId: targetFolderId });
      } else {
        if (a.id === targetFolderId) return;
        await moveFolder.mutateAsync({ id: a.id, parentId: targetFolderId });
      }
    } catch (err) {
      showToast(err instanceof Error ? err.message : "Di chuyển thất bại.");
    }
  }

  // ---------- upload ----------
  function triggerPick() {
    if (!current) {
      showToast("Hãy mở một thư mục để tải ảnh lên.");
      return;
    }
    fileInputRef.current?.click();
  }

  function enqueueAndUpload(files: File[]) {
    if (!current) {
      showToast("Hãy mở một thư mục để tải ảnh lên.");
      return;
    }
    const newItems: QueueItem[] = [];
    let validCount = 0;
    for (const f of files) {
      const id = `${Date.now()}-${Math.random().toString(36).slice(2)}`;
      const preview = f.type.startsWith("image/") ? URL.createObjectURL(f) : undefined;
      if (!f.type.startsWith("image/")) {
        newItems.push({ id, name: f.name, previewUrl: preview, status: "error", error: "Không phải ảnh" });
      } else if (f.size > ImageFileMax) {
        newItems.push({ id, name: f.name, previewUrl: preview, status: "error", error: "Vượt quá 64MB" });
      } else if (validCount >= 20) {
        newItems.push({ id, name: f.name, previewUrl: preview, status: "error", error: "Vượt 20 file/lần" });
      } else {
        validCount++;
        fileMap.current.set(id, f);
        newItems.push({ id, name: f.name, previewUrl: preview, status: "pending" });
      }
    }
    setQueue((q) => [...q, ...newItems]);
    void runUpload(newItems.filter((i) => i.status === "pending"), current);
  }

  async function runUpload(items: QueueItem[], folderId: string) {
    if (items.length === 0) return;
    const ids = new Set(items.map((i) => i.id));
    setQueue((q) => q.map((i) => (ids.has(i.id) ? { ...i, status: "uploading" } : i)));

    const files = items.map((i) => fileMap.current.get(i.id)!).filter(Boolean);
    try {
      const res = await upload.mutateAsync({ folderId, files });
      setQueue((q) =>
        q.map((i) => {
          const idx = items.findIndex((it) => it.id === i.id);
          if (idx < 0) return i;
          const r = res.results[idx];
          return r && r.status === "success"
            ? { ...i, status: "success" }
            : { ...i, status: "error", error: r?.error ?? "Lỗi không rõ" };
        }),
      );
    } catch (err) {
      const msg = err instanceof Error ? err.message : "Tải lên thất bại.";
      setQueue((q) => q.map((i) => (ids.has(i.id) ? { ...i, status: "error", error: msg } : i)));
    }
  }

  function retry(id: string) {
    const item = queue.find((i) => i.id === id);
    if (!item || !current || !fileMap.current.get(id)) return;
    setQueue((q) => q.map((i) => (i.id === id ? { ...i, status: "pending" } : i)));
    void runUpload([{ ...item, status: "pending" }], current);
  }

  // ---------- context menu ----------
  function buildMenu(target: CtxTarget): MenuItem[] {
    if (target?.type === "image") {
      const img = target.image;
      const items: MenuItem[] = [
        { label: "Mở", icon: ArrowsOutSimple, onClick: () => setLightbox({ images: [img], index: 0 }) },
        { label: "Copy link", icon: Copy, onClick: () => void navigator.clipboard.writeText(img.url) },
      ];
      if (isAdmin) {
        items.push({ label: "Đổi tên", icon: PencilSimple, onClick: () => setRenameTarget({ type: "image", id: img.id, name: img.name }) });
        items.push({ label: "Xóa", icon: Trash, danger: true, onClick: () => setDeleteTarget({ items: [{ type: "image", id: img.id, name: img.name }] }) });
      }
      return items;
    }
    if (target?.type === "folder") {
      const items: MenuItem[] = [
        { label: "Mở", icon: ArrowsOutSimple, onClick: () => navigateTo(target.id) },
      ];
      if (isAdmin) {
        items.push({ label: "Đổi tên", icon: PencilSimple, onClick: () => setRenameTarget({ type: "folder", id: target.id, name: target.name }) });
        items.push({ label: "Xóa", icon: Trash, danger: true, onClick: () => setDeleteTarget({ items: [{ type: "folder", id: target.id, name: target.name }] }) });
      }
      return items;
    }
    // nền trống
    if (isAdmin) {
      return [
        { label: "Thư mục mới", icon: FolderPlus, onClick: () => setNewFolderOpen(true) },
        { label: "Tải ảnh lên", icon: UploadSimple, onClick: triggerPick },
      ];
    }
    return [];
  }

  function openContextMenu(e: React.MouseEvent, target: CtxTarget) {
    const items = buildMenu(target);
    if (items.length === 0) return;
    setCtx({ x: e.clientX, y: e.clientY, target });
  }

  return (
    <DndContext sensors={sensors} onDragEnd={onDragEnd}>
      <div className="flex h-[100dvh] flex-col bg-zinc-50">
        <Topbar
          isAdmin={isAdmin}
          onNewFolder={() => setNewFolderOpen(true)}
          onUpload={triggerPick}
          onLogout={logout}
        />

        <div className="flex min-h-0 flex-1">
          {/* Sidebar desktop */}
          <aside className="hidden w-60 shrink-0 border-r border-zinc-200 bg-white md:block">
            <FolderTree />
          </aside>

          {/* Sidebar drawer mobile */}
          {drawerOpen && (
            <div className="fixed inset-0 z-40 md:hidden" onClick={() => setDrawerOpen(false)}>
              <div className="absolute inset-0 bg-zinc-900/30" />
              <aside className="absolute left-0 top-0 h-full w-72 bg-white shadow-xl" onClick={(e) => e.stopPropagation()}>
                <FolderTree />
              </aside>
            </div>
          )}

          <main className="min-w-0 flex-1 bg-white">
            <ContentPane
              folderId={current}
              isAdmin={isAdmin}
              onActivateImage={(images, index) => setLightbox({ images, index })}
              onContextMenu={openContextMenu}
              onDeleteSelection={(items) => setDeleteTarget({ items })}
              onFilesDropped={enqueueAndUpload}
            />
          </main>
        </div>
      </div>

      {/* hidden file input */}
      <input
        ref={fileInputRef}
        type="file"
        accept="image/*"
        multiple
        hidden
        onChange={(e) => {
          const files = Array.from(e.target.files ?? []);
          e.target.value = "";
          if (files.length) enqueueAndUpload(files);
        }}
      />

      {lightbox && (
        <Lightbox
          images={lightbox.images}
          index={lightbox.index}
          onIndex={(i) => setLightbox((lb) => (lb ? { ...lb, index: i } : lb))}
          onClose={() => setLightbox(null)}
        />
      )}

      {ctx && <ContextMenu x={ctx.x} y={ctx.y} items={buildMenu(ctx.target)} onClose={() => setCtx(null)} />}

      <NewFolderDialog open={newFolderOpen} onClose={() => setNewFolderOpen(false)} parentId={current} />
      <RenameDialog open={!!renameTarget} onClose={() => setRenameTarget(null)} target={renameTarget} />
      <DeleteConfirmDialog
        open={!!deleteTarget}
        onClose={() => setDeleteTarget(null)}
        target={deleteTarget}
        onDeleted={() => useUiStore.getState().clearSelection()}
      />

      <UploadQueue items={queue} onClose={() => setQueue([])} onRetry={retry} />

      {toast && (
        <div className="fixed bottom-4 left-1/2 z-50 -translate-x-1/2 rounded-lg bg-zinc-900 px-4 py-2 text-sm text-white shadow-lg">
          {toast}
        </div>
      )}
    </DndContext>
  );
}
