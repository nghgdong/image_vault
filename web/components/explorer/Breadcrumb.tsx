"use client";

import { CaretRight, House } from "@phosphor-icons/react";
import { useBreadcrumb } from "@/lib/hooks";
import { useUiStore } from "@/store/uiStore";

export function Breadcrumb({ folderId }: { folderId: string | null }) {
  const navigateTo = useUiStore((s) => s.navigateTo);
  const { data } = useBreadcrumb(folderId);

  return (
    <nav className="flex min-w-0 items-center gap-1 overflow-x-auto text-sm scroll-thin" aria-label="Đường dẫn">
      <button
        onClick={() => navigateTo(null)}
        className={`flex shrink-0 items-center gap-1 rounded-md px-2 py-1 transition-colors hover:bg-zinc-200/60 ${
          folderId === null ? "font-medium text-zinc-900" : "text-zinc-500"
        }`}
      >
        <House size={15} weight={folderId === null ? "fill" : "regular"} />
        Root
      </button>

      {(data ?? []).map((c, i, arr) => (
        <div key={c.id} className="flex shrink-0 items-center gap-1">
          <CaretRight size={12} className="text-zinc-300" />
          <button
            onClick={() => navigateTo(c.id)}
            className={`truncate rounded-md px-2 py-1 transition-colors hover:bg-zinc-200/60 ${
              i === arr.length - 1 ? "font-medium text-zinc-900" : "text-zinc-500"
            }`}
          >
            {c.name}
          </button>
        </div>
      ))}
    </nav>
  );
}
