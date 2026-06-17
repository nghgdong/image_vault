"use client";

import { useMutation, useQuery, useQueryClient, type QueryClient } from "@tanstack/react-query";
import { api } from "./api";
import type { SortField, SortOrder } from "./types";

function invalidateTree(qc: QueryClient) {
  qc.invalidateQueries({ queryKey: ["root"] });
  qc.invalidateQueries({ queryKey: ["children"] });
  qc.invalidateQueries({ queryKey: ["breadcrumb"] });
}

export function useContents(
  folderId: string | null,
  sort: SortField,
  order: SortOrder,
  enabled = true,
) {
  return useQuery({
    queryKey: folderId === null ? ["root"] : ["children", folderId, sort, order],
    queryFn: () =>
      folderId === null ? api.getRoot() : api.getChildren(folderId, { sort, order, pageSize: 200 }),
    enabled,
  });
}

export function useBreadcrumb(folderId: string | null) {
  return useQuery({
    queryKey: ["breadcrumb", folderId],
    queryFn: () => api.getBreadcrumb(folderId as string),
    enabled: folderId !== null,
  });
}

export function useSearch(query: string) {
  const q = query.trim();
  return useQuery({
    queryKey: ["search", q],
    queryFn: () => api.search(q),
    enabled: q.length >= 1,
    staleTime: 10_000,
  });
}

export function useImageDetail(id: string | null) {
  return useQuery({
    queryKey: ["image", id],
    queryFn: () => api.getImage(id as string),
    enabled: id !== null,
  });
}

export function useCreateFolder() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ name, parentId }: { name: string; parentId: string | null }) =>
      api.createFolder(name, parentId),
    onSuccess: () => invalidateTree(qc),
  });
}

export function useRenameFolder() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, name }: { id: string; name: string }) => api.renameFolder(id, name),
    onSuccess: () => invalidateTree(qc),
  });
}

export function useMoveFolder() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, parentId }: { id: string; parentId: string | null }) =>
      api.moveFolder(id, parentId),
    onSuccess: () => invalidateTree(qc),
  });
}

export function useDeleteFolder() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => api.deleteFolder(id, true),
    onSuccess: () => invalidateTree(qc),
  });
}

export function useDeleteImage() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => api.deleteImage(id),
    onSuccess: () => invalidateTree(qc),
  });
}

export function useRenameImage() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, name }: { id: string; name: string }) => api.updateImage(id, { name }),
    onSuccess: () => invalidateTree(qc),
  });
}

export function useMoveImage() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, folderId }: { id: string; folderId: string }) => api.moveImage(id, folderId),
    onSuccess: () => invalidateTree(qc),
  });
}

export function useUploadBatch() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ folderId, files }: { folderId: string; files: File[] }) =>
      api.uploadBatch(folderId, files),
    onSuccess: () => invalidateTree(qc),
  });
}
