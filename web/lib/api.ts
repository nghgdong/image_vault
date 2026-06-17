import type {
  BatchUploadResponse,
  BreadcrumbItem,
  FolderChildren,
  FolderCreated,
  ImageDetail,
  SearchResults,
  SortField,
  SortOrder,
} from "./types";

const BASE_URL =
  process.env.NEXT_PUBLIC_API_BASE_URL?.replace(/\/$/, "") ?? "http://localhost:8080/api";

const TOKEN_KEY = "iv_token";

export function getToken(): string | null {
  if (typeof window === "undefined") return null;
  return window.localStorage.getItem(TOKEN_KEY);
}
export function setToken(token: string) {
  window.localStorage.setItem(TOKEN_KEY, token);
}
export function clearToken() {
  window.localStorage.removeItem(TOKEN_KEY);
}

/** Lỗi từ API (ProblemDetails RFC 7807). */
export class ApiError extends Error {
  constructor(public status: number, message: string) {
    super(message);
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const token = getToken();
  const headers = new Headers(init?.headers);
  if (token) headers.set("Authorization", `Bearer ${token}`);

  const res = await fetch(`${BASE_URL}${path}`, { ...init, headers });

  if (res.status === 401) {
    clearToken();
  }

  if (!res.ok) {
    let detail = `HTTP ${res.status}`;
    try {
      const body = await res.json();
      detail = body.detail || body.title || detail;
    } catch {
      /* ignore */
    }
    throw new ApiError(res.status, detail);
  }

  if (res.status === 204) return undefined as T;
  return (await res.json()) as T;
}

function jsonBody(method: string, body: unknown): RequestInit {
  return {
    method,
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  };
}

export const api = {
  // --- auth ---
  login: (username: string, password: string) =>
    request<{ token: string; expiresAt: string }>("/auth/login", jsonBody("POST", { username, password })),

  // --- public reads ---
  getRoot: () => request<FolderChildren>("/folders/root"),
  getChildren: (
    id: string,
    opts: { page?: number; pageSize?: number; sort?: SortField; order?: SortOrder } = {},
  ) => {
    const q = new URLSearchParams();
    if (opts.page) q.set("page", String(opts.page));
    if (opts.pageSize) q.set("pageSize", String(opts.pageSize));
    if (opts.sort) q.set("sort", opts.sort);
    if (opts.order) q.set("order", opts.order);
    const qs = q.toString();
    return request<FolderChildren>(`/folders/${id}/children${qs ? `?${qs}` : ""}`);
  },
  getBreadcrumb: (id: string) => request<BreadcrumbItem[]>(`/folders/${id}/breadcrumb`),
  getImage: (id: string) => request<ImageDetail>(`/images/${id}`),
  search: (q: string) => request<SearchResults>(`/search?q=${encodeURIComponent(q)}`),

  // --- admin folder ---
  createFolder: (name: string, parentId: string | null) =>
    request<FolderCreated>("/folders", jsonBody("POST", { name, parentId })),
  renameFolder: (id: string, name: string) =>
    request<FolderCreated>(`/folders/${id}`, jsonBody("PUT", { name })),
  moveFolder: (id: string, parentId: string | null) =>
    request<FolderCreated>(`/folders/${id}`, jsonBody("PUT", { parentId })),
  deleteFolder: (id: string, cascade = true) =>
    request<{ deletedFolders: number; deletedImages: number }>(
      `/folders/${id}?cascade=${cascade}`,
      { method: "DELETE" },
    ),

  // --- admin image ---
  deleteImage: (id: string) => request<unknown>(`/images/${id}`, { method: "DELETE" }),
  updateImage: (id: string, patch: { name?: string; folderId?: string }) =>
    request<ImageDetail>(`/images/${id}`, jsonBody("PUT", patch)),
  moveImage: (id: string, folderId: string) =>
    request<ImageDetail>(`/images/${id}`, jsonBody("PUT", { folderId })),

  // --- upload (batch, 1 request — SPEC §5.4.5) ---
  uploadBatch: async (folderId: string, files: File[]): Promise<BatchUploadResponse> => {
    const form = new FormData();
    form.append("folderId", folderId);
    for (const f of files) form.append("files", f, f.name);
    const token = getToken();
    const headers = new Headers();
    if (token) headers.set("Authorization", `Bearer ${token}`);
    const res = await fetch(`${BASE_URL}/images/upload-batch`, { method: "POST", headers, body: form });
    if (res.status === 401) clearToken();
    if (!res.ok && res.status !== 207) {
      let detail = `HTTP ${res.status}`;
      try {
        const b = await res.json();
        detail = b.detail || b.title || detail;
      } catch {
        /* ignore */
      }
      throw new ApiError(res.status, detail);
    }
    return (await res.json()) as BatchUploadResponse;
  },
};
