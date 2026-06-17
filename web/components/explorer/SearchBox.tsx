"use client";

import { useEffect, useState } from "react";
import { MagnifyingGlass, X } from "@phosphor-icons/react";
import { useUiStore } from "@/store/uiStore";

export function SearchBox() {
  const searchQuery = useUiStore((s) => s.searchQuery);
  const setSearchQuery = useUiStore((s) => s.setSearchQuery);
  const [text, setText] = useState(searchQuery);

  // debounce input -> store (store điều khiển kết quả ở khung nội dung)
  useEffect(() => {
    const t = setTimeout(() => setSearchQuery(text.trim()), 250);
    return () => clearTimeout(t);
  }, [text, setSearchQuery]);

  // đồng bộ khi store bị xóa nơi khác (vd điều hướng folder)
  useEffect(() => {
    if (searchQuery === "" && text !== "") setText("");
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [searchQuery]);

  return (
    <div className="relative w-full sm:w-64">
      <div className="flex items-center gap-1.5 rounded-lg border border-zinc-200 bg-white px-2.5 py-1.5 focus-within:border-blue-500 focus-within:ring-2 focus-within:ring-blue-500/20">
        <MagnifyingGlass size={15} className="shrink-0 text-zinc-400" />
        <input
          value={text}
          onChange={(e) => setText(e.target.value)}
          placeholder="Tìm thư mục, ảnh…"
          className="w-full bg-transparent text-sm outline-none placeholder:text-zinc-400"
        />
        {text && (
          <button onClick={() => setText("")} aria-label="Xóa tìm kiếm" className="shrink-0 text-zinc-400 hover:text-zinc-600">
            <X size={14} />
          </button>
        )}
      </div>
    </div>
  );
}
