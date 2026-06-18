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
import { ImageFileMax, MaxBatchFiles } from "@/lib/constants";
import { ApiError } from "@/lib/api";
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
    // Validate type + size. KHÔNG giới hạn 20 ở đây — FE tự chia lô (xem runUpload).
    // Không tạo preview ở bước này: 1000 thumbnail decode cùng lúc làm treo trình duyệt.
    // Preview được tạo lazy theo từng lô đang chạy rồi revoke khi xong.
    const newItems: QueueItem[] = [];
    const valid: QueueItem[] = [];
    for (const f of files) {
      const id = `${Date.now()}-${Math.random().toString(36).slice(2)}`;
      if (!f.type.startsWith("image/")) {
        newItems.push({ id, name: f.name, status: "error", error: "Không phải ảnh" });
      } else if (f.size > ImageFileMax) {
        newItems.push({ id, name: f.name, status: "error", error: "Vượt quá 64MB" });
      } else {
        fileMap.current.set(id, f);
        const item: QueueItem = { id, name: f.name, status: "pending" };
        newItems.push(item);
        valid.push(item);
      }
    }
    setQueue((q) => [...q, ...newItems]);
    void runUpload(valid, current);
  }

  /** Gửi 1 lô; gặp 429 (rate limit ~30 req/phút) thì chờ rồi thử lại lô đó. */
  async function uploadWithRetry(folderId: string, files: File[]) {
    for (let attempt = 0; ; attempt++) {
      try {
        return await upload.mutateAsync({ folderId, files });
      } catch (err) {
        const tooMany = err instanceof ApiError && err.status === 429;
        if (!tooMany || attempt >= 15) throw err;
        await new Promise((r) => setTimeout(r, 5000)); // chờ cửa sổ rate-limit reset
      }
    }
  }

  /** Tải lên theo lô tuần tự (mỗi lô ≤ MaxBatchFiles) để tôn trọng giới hạn backend. */
  async function runUpload(items: QueueItem[], folderId: string) {
    if (items.length === 0) return;
    for (let i = 0; i < items.length; i += MaxBatchFiles) {
      await uploadChunk(items.slice(i, i + MaxBatchFiles), folderId);
    }
  }

  async function uploadChunk(chunk: QueueItem[], folderId: string) {
    if (chunk.length === 0) return;
    // Tạo preview chỉ cho lô đang chạy.
    const previews = new Map<string, string>();
    for (const it of chunk) {
      const f = fileMap.current.get(it.id);
      if (f) previews.set(it.id, URL.createObjectURL(f));
    }
    setQueue((q) =>
      q.map((i) => (previews.has(i.id) ? { ...i, status: "uploading", previewUrl: previews.get(i.id) } : i)),
    );

    const files = chunk.map((i) => fileMap.current.get(i.id)!).filter(Boolean);
    try {
      const res = await uploadWithRetry(folderId, files);
      setQueue((q) =>
        q.map((i) => {
          const idx = chunk.findIndex((it) => it.id === i.id);
          if (idx < 0) return i;
          const r = res.results[idx];
          return r && r.status === "success"
            ? { ...i, status: "success", previewUrl: undefined }
            : { ...i, status: "error", previewUrl: undefined, error: r?.error ?? "Lỗi không rõ" };
        }),
      );
    } catch (err) {
      const msg = err instanceof Error ? err.message : "Tải lên thất bại.";
      const ids = new Set(chunk.map((i) => i.id));
      setQueue((q) => q.map((i) => (ids.has(i.id) ? { ...i, status: "error", previewUrl: undefined, error: msg } : i)));
    } finally {
      // Giải phóng blob URL của lô để không giữ thumbnail decode trong bộ nhớ.
      for (const url of Array.from(previews.values())) URL.revokeObjectURL(url);
    }
  }

  function retry(id: string) {
    const item = queue.find((i) => i.id === id);
    if (!item || !current || !fileMap.current.get(id)) return;
    setQueue((q) => q.map((i) => (i.id === id ? { ...i, status: "pending" } : i)));
    void uploadChunk([{ ...item, status: "pending" }], current);
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
