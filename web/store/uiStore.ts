"use client";

import { create } from "zustand";
import type { SortField, SortOrder, ViewMode } from "@/lib/types";

export type SelectedItem = { type: "folder" | "image"; id: string };

interface UiState {
  // view
  viewMode: ViewMode;
  setViewMode: (v: ViewMode) => void;
  sort: SortField;
  order: SortOrder;
  setSort: (sort: SortField, order: SortOrder) => void;

  // navigation history (null = root ảo)
  history: (string | null)[];
  index: number;
  navigateTo: (id: string | null) => void;
  back: () => void;
  forward: () => void;

  // sidebar tree expand
  expanded: Record<string, boolean>;
  toggleExpand: (id: string) => void;
  setExpanded: (id: string, open: boolean) => void;

  // selection (trong content pane hiện tại)
  selection: SelectedItem[];
  setSelection: (s: SelectedItem[]) => void;
  clearSelection: () => void;

  // mobile drawer
  drawerOpen: boolean;
  setDrawerOpen: (open: boolean) => void;
}

export const useUiStore = create<UiState>((set, get) => ({
  viewMode: "icon",
  setViewMode: (viewMode) => set({ viewMode }),
  sort: "name",
  order: "asc",
  setSort: (sort, order) => set({ sort, order }),

  history: [null],
  index: 0,
  navigateTo: (id) => {
    const { history, index } = get();
    if (history[index] === id) return;
    const next = history.slice(0, index + 1);
    next.push(id);
    set({ history: next, index: next.length - 1, selection: [] });
  },
  back: () => {
    const { index } = get();
    if (index > 0) set({ index: index - 1, selection: [] });
  },
  forward: () => {
    const { history, index } = get();
    if (index < history.length - 1) set({ index: index + 1, selection: [] });
  },

  expanded: {},
  toggleExpand: (id) => set((s) => ({ expanded: { ...s.expanded, [id]: !s.expanded[id] } })),
  setExpanded: (id, open) => set((s) => ({ expanded: { ...s.expanded, [id]: open } })),

  selection: [],
  setSelection: (selection) => set({ selection }),
  clearSelection: () => set({ selection: [] }),

  drawerOpen: false,
  setDrawerOpen: (drawerOpen) => set({ drawerOpen }),
}));

export const selectCurrentFolderId = (s: UiState) => s.history[s.index];
export const selectCanBack = (s: UiState) => s.index > 0;
export const selectCanForward = (s: UiState) => s.index < s.history.length - 1;
