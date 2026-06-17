// Types khớp DTO backend (SPEC §4).

export interface FolderSummary {
  id: string | null; // null = root ảo
  name: string;
  parentId: string | null;
}

export interface SubFolder {
  id: string;
  name: string;
  imageCount: number;
  subFolderCount: number;
}

export interface ImageDto {
  id: string;
  name: string;
  url: string;
  thumbUrl: string;
  width: number | null;
  height: number | null;
}

export interface FolderChildren {
  folder: FolderSummary;
  subFolders: SubFolder[];
  images: ImageDto[];
  page: number;
  pageSize: number;
  totalImages: number;
}

export interface BreadcrumbItem {
  id: string;
  name: string;
}

export interface ImageDetail {
  id: string;
  folderId: string;
  name: string;
  url: string;
  thumbUrl: string;
  mediumUrl: string | null;
  width: number | null;
  height: number | null;
  sizeBytes: number | null;
  mimeType: string | null;
  uploadedAt: string;
}

export interface FolderCreated {
  id: string;
  name: string;
  parentId: string | null;
  path: string;
}

export interface BatchUploadResultItem {
  index: number;
  fileName: string;
  status: "success" | "error";
  image: ImageDetail | null;
  error: string | null;
}

export interface BatchUploadResponse {
  total: number;
  succeeded: number;
  failed: number;
  results: BatchUploadResultItem[];
}

export interface FolderHit {
  id: string;
  name: string;
  parentId: string | null;
}

export interface ImageHit {
  id: string;
  name: string;
  folderId: string;
  url: string;
  thumbUrl: string;
  width: number | null;
  height: number | null;
}

export interface SearchResults {
  query: string;
  folders: FolderHit[];
  images: ImageHit[];
}

export type SortField = "name" | "date";
export type SortOrder = "asc" | "desc";
export type ViewMode = "icon" | "list";
