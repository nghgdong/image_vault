"use client";

import { useEffect, useRef } from "react";
import type { Icon } from "@phosphor-icons/react";

export interface MenuItem {
  label: string;
  icon: Icon;
  onClick: () => void;
  danger?: boolean;
}

export function ContextMenu({
  x,
  y,
  items,
  onClose,
}: {
  x: number;
  y: number;
  items: MenuItem[];
  onClose: () => void;
}) {
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const close = () => onClose();
    const onKey = (e: KeyboardEvent) => e.key === "Escape" && onClose();
    window.addEventListener("click", close);
    window.addEventListener("scroll", close, true);
    window.addEventListener("keydown", onKey);
    return () => {
      window.removeEventListener("click", close);
      window.removeEventListener("scroll", close, true);
      window.removeEventListener("keydown", onKey);
    };
  }, [onClose]);

  // tránh tràn mép phải/đáy
  const style: React.CSSProperties = {
    top: Math.min(y, typeof window !== "undefined" ? window.innerHeight - items.length * 40 - 16 : y),
    left: Math.min(x, typeof window !== "undefined" ? window.innerWidth - 200 : x),
  };

  return (
    <div
      ref={ref}
      style={style}
      onClick={(e) => e.stopPropagation()}
      className="fixed z-50 min-w-[180px] overflow-hidden rounded-xl border border-zinc-200 bg-white py-1 shadow-[0_12px_32px_-8px_rgba(0,0,0,0.25)]"
    >
      {items.map((it, i) => {
        const I = it.icon;
        return (
          <button
            key={i}
            onClick={() => {
              it.onClick();
              onClose();
            }}
            className={`flex w-full items-center gap-2.5 px-3 py-2 text-left text-sm transition-colors hover:bg-zinc-100 ${
              it.danger ? "text-rose-600 hover:bg-rose-50" : "text-zinc-700"
            }`}
          >
            <I size={16} />
            {it.label}
          </button>
        );
      })}
    </div>
  );
}
