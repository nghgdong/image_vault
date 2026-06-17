"use client";

import { create } from "zustand";
import { clearToken, getToken, setToken } from "@/lib/api";

interface AuthState {
  token: string | null;
  isAdmin: boolean;
  hydrate: () => void;
  login: (token: string) => void;
  logout: () => void;
}

export const useAuthStore = create<AuthState>((set) => ({
  token: null,
  isAdmin: false,
  hydrate: () => {
    const t = getToken();
    set({ token: t, isAdmin: !!t });
  },
  login: (token: string) => {
    setToken(token);
    set({ token, isAdmin: true });
  },
  logout: () => {
    clearToken();
    set({ token: null, isAdmin: false });
  },
}));
