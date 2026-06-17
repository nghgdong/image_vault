"use client";

import { GridFour, ListBullets, SortAscending, SortDescending } from "@phosphor-icons/react";
import { useUiStore } from "@/store/uiStore";
import type { SortField } from "@/lib/types";

export function Toolbar({ selectedCount }: { selectedCount: number }) {
  const { viewMode, setViewMode, sort, order, setSort } = useUiStore();

  return (
    <div className="flex items-center justify-between gap-3 border-b border-zinc-200 px-4 py-2">
      <div className="inline-flex overflow-hidden rounded-lg border border-zinc-200">
        <button
          onClick={() => setViewMode("icon")}
          aria-pressed={viewMode === "icon"}
          title="Xem dạng lưới"
          className={`flex items-center gap-1.5 px-2.5 py-1.5 text-sm transition-colors ${
            viewMode === "icon" ? "bg-zinc-900 text-white" : "text-zinc-600 hover:bg-zinc-100"
          }`}
        >
          <GridFour size={16} /> Lưới
        </button>
        <button
          onClick={() => setViewMode("list")}
          aria-pressed={viewMode === "list"}
          title="Xem dạng danh sách"
          className={`flex items-center gap-1.5 px-2.5 py-1.5 text-sm transition-colors ${
            viewMode === "list" ? "bg-zinc-900 text-white" : "text-zinc-600 hover:bg-zinc-100"
          }`}
        >
          <ListBullets size={16} /> Danh sách
        </button>
      </div>

      <div className="flex items-center gap-2">
        {selectedCount > 0 && (
          <span className="text-sm text-zinc-500">{selectedCount} đã chọn</span>
        )}
        <select
          value={sort}
          onChange={(e) => setSort(e.target.value as SortField, order)}
          className="rounded-lg border border-zinc-200 bg-white px-2 py-1.5 text-sm outline-none focus:border-blue-500"
        >
          <option value="name">Tên</option>
          <option value="date">Ngày</option>
        </select>
        <button
          onClick={() => setSort(sort, order === "asc" ? "desc" : "asc")}
          title={order === "asc" ? "Tăng dần" : "Giảm dần"}
          className="rounded-lg border border-zinc-200 p-1.5 text-zinc-600 transition-colors hover:bg-zinc-100"
        >
          {order === "asc" ? <SortAscending size={16} /> : <SortDescending size={16} />}
        </button>
      </div>
    </div>
  );
}
