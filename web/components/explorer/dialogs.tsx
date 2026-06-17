"use client";

import { useEffect, useState } from "react";
import { Warning } from "@phosphor-icons/react";
import { Dialog } from "@/components/ui/Dialog";
import { ApiError } from "@/lib/api";
import { useCreateFolder, useDeleteFolder, useDeleteImage, useRenameFolder, useRenameImage } from "@/lib/hooks";

function errMsg(e: unknown) {
  return e instanceof ApiError ? e.message : "Đã xảy ra lỗi.";
}

const inputCls =
  "w-full rounded-lg border border-zinc-300 px-3 py-2 text-sm outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-500/20";
const primaryBtn =
  "rounded-lg bg-zinc-900 px-4 py-2 text-sm font-medium text-white transition-all hover:bg-zinc-800 active:scale-[0.99] disabled:opacity-60";
const ghostBtn = "rounded-lg px-4 py-2 text-sm text-zinc-600 transition-colors hover:bg-zinc-100";

export function NewFolderDialog({
  open,
  onClose,
  parentId,
}: {
  open: boolean;
  onClose: () => void;
  parentId: string | null;
}) {
  const [name, setName] = useState("");
  const [error, setError] = useState<string | null>(null);
  const create = useCreateFolder();

  useEffect(() => {
    if (open) {
      setName("");
      setError(null);
    }
  }, [open]);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    try {
      await create.mutateAsync({ name: name.trim(), parentId });
      onClose();
    } catch (err) {
      setError(errMsg(err));
    }
  }

  return (
    <Dialog open={open} onClose={onClose} title="Thư mục mới">
      <form onSubmit={submit} className="flex flex-col gap-3">
        <input autoFocus value={name} onChange={(e) => setName(e.target.value)} placeholder="Tên thư mục" className={inputCls} />
        {error && <p className="text-sm text-rose-600">{error}</p>}
        <div className="flex justify-end gap-2">
          <button type="button" onClick={onClose} className={ghostBtn}>Hủy</button>
          <button type="submit" disabled={!name.trim() || create.isPending} className={primaryBtn}>
            {create.isPending ? "Đang tạo…" : "Tạo"}
          </button>
        </div>
      </form>
    </Dialog>
  );
}

export function RenameDialog({
  open,
  onClose,
  target,
}: {
  open: boolean;
  onClose: () => void;
  target: { type: "folder" | "image"; id: string; name: string } | null;
}) {
  const [name, setName] = useState("");
  const [error, setError] = useState<string | null>(null);
  const renameFolder = useRenameFolder();
  const renameImage = useRenameImage();
  const pending = renameFolder.isPending || renameImage.isPending;

  useEffect(() => {
    if (open && target) {
      setName(target.name);
      setError(null);
    }
  }, [open, target]);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    if (!target) return;
    setError(null);
    try {
      if (target.type === "folder") await renameFolder.mutateAsync({ id: target.id, name: name.trim() });
      else await renameImage.mutateAsync({ id: target.id, name: name.trim() });
      onClose();
    } catch (err) {
      setError(errMsg(err));
    }
  }

  return (
    <Dialog open={open} onClose={onClose} title="Đổi tên">
      <form onSubmit={submit} className="flex flex-col gap-3">
        <input autoFocus value={name} onChange={(e) => setName(e.target.value)} className={inputCls} />
        {error && <p className="text-sm text-rose-600">{error}</p>}
        <div className="flex justify-end gap-2">
          <button type="button" onClick={onClose} className={ghostBtn}>Hủy</button>
          <button type="submit" disabled={!name.trim() || pending} className={primaryBtn}>
            {pending ? "Đang lưu…" : "Lưu"}
          </button>
        </div>
      </form>
    </Dialog>
  );
}

export function DeleteConfirmDialog({
  open,
  onClose,
  target,
}: {
  open: boolean;
  onClose: () => void;
  target: { type: "folder" | "image"; id: string; name: string } | null;
}) {
  const [error, setError] = useState<string | null>(null);
  const delFolder = useDeleteFolder();
  const delImage = useDeleteImage();
  const pending = delFolder.isPending || delImage.isPending;

  useEffect(() => {
    if (open) setError(null);
  }, [open]);

  async function confirm() {
    if (!target) return;
    setError(null);
    try {
      if (target.type === "folder") await delFolder.mutateAsync(target.id);
      else await delImage.mutateAsync(target.id);
      onClose();
    } catch (err) {
      setError(errMsg(err));
    }
  }

  return (
    <Dialog open={open} onClose={onClose} title={target?.type === "folder" ? "Xóa thư mục" : "Xóa ảnh"}>
      <div className="flex flex-col gap-4">
        <p className="text-sm text-zinc-700">
          Xóa <span className="font-medium">{target?.name}</span>
          {target?.type === "folder" ? " và toàn bộ nội dung bên trong" : ""}?
        </p>
        <div className="flex items-start gap-2 rounded-lg bg-amber-50 p-3 text-sm text-amber-800">
          <Warning size={18} weight="fill" className="mt-0.5 shrink-0 text-amber-500" />
          <span>
            Ảnh trên freeimage.host sẽ <strong>KHÔNG bị xóa</strong>, chỉ ẩn khỏi kho. Link trực tiếp vẫn truy cập được nếu ai đó còn giữ.
          </span>
        </div>
        {error && <p className="text-sm text-rose-600">{error}</p>}
        <div className="flex justify-end gap-2">
          <button type="button" onClick={onClose} className={ghostBtn}>Hủy</button>
          <button
            onClick={confirm}
            disabled={pending}
            className="rounded-lg bg-rose-600 px-4 py-2 text-sm font-medium text-white transition-all hover:bg-rose-700 active:scale-[0.99] disabled:opacity-60"
          >
            {pending ? "Đang xóa…" : "Xóa"}
          </button>
        </div>
      </div>
    </Dialog>
  );
}
