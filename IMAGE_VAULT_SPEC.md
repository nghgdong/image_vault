# Image Vault — Tài liệu Spec & Thiết kế

> **Mục đích tài liệu:** Đặc tả đầy đủ để AI coding agent (Claude Code / Cursor / v.v.) có thể implement end-to-end mà không cần hỏi lại. Mọi quyết định kiến trúc, schema, API contract, và UI behavior đã được chốt.

---

## 0. Tóm tắt 1 phút

Web app dạng **kho ảnh có cây thư mục lồng nhau**, giao diện giống **File Explorer của Windows**.

- **Public**: duyệt thư mục + xem ảnh (read-only).
- **Admin** (đăng nhập): tạo/đổi tên/xóa/di chuyển thư mục & ảnh, upload ảnh.
- **Binary ảnh** lưu trên **freeimage.host**; **metadata** (cây thư mục, vị trí ảnh) lưu trên **MongoDB**.
- **Backend**: .NET 8 Web API. **Frontend**: Next.js (React) + TypeScript.

---

## 1. Ràng buộc & giả định quan trọng (ĐỌC TRƯỚC KHI CODE)

### 1.1 Giới hạn của freeimage.host (Chevereto guest API)

| Khả năng | Hỗ trợ? | Ghi chú |
|---|---|---|
| Upload ảnh (POST multipart / base64 / URL) | ✅ | Endpoint `POST /api/1/upload`, trả JSON |
| Trả về direct URL + thumbnail URL | ✅ | Trong `image.url`, `image.thumb.url` |
| Auth bằng API key | ✅ | Param `key` hoặc header `X-API-Key` (V1.1) |
| Giới hạn dung lượng | 64 MB / ảnh | |
| **Xóa ảnh qua API** | ❌ **KHÔNG** | Guest API không có endpoint delete |
| Tạo "folder" thật trên freeimage | ❌ | Chỉ có album (không cần dùng) |

> **Hệ quả thiết kế (BẮT BUỘC tuân theo):**
> - Khi admin "xóa ảnh" hoặc "xóa thư mục" → app **chỉ xóa metadata trong MongoDB** (soft delete). Binary trên freeimage.host **vẫn tồn tại vĩnh viễn** và URL vẫn truy cập được nếu ai đó giữ link.
> - **Ảnh là PUBLIC tuyệt đối.** Phân quyền admin chỉ kiểm soát metadata/giao diện, KHÔNG bảo vệ được binary. Không upload ảnh nhạy cảm.
> - Phải có màn admin cảnh báo rõ điều này.

### 1.2 Quyết định kiến trúc đã chốt

- Cây thư mục dùng **materialized path** (không dùng adjacency-list thuần) để query con cháu + breadcrumb nhanh.
- Soft delete (`isDeleted` flag) thay vì hard delete, vì không xóa được binary nên giữ metadata để audit.
- Upload **luôn đi qua backend** — FE không bao giờ gọi freeimage trực tiếp (giấu API key).
- 1 admin duy nhất (mở rộng multi-admin sau). Auth bằng JWT.

---

## 2. Kiến trúc hệ thống

```
┌─────────────┐      HTTPS/JSON      ┌──────────────────┐     Mongo driver    ┌──────────┐
│  Next.js FE │ ───────────────────> │  .NET 8 Web API  │ ──────────────────> │ MongoDB  │
│  (browser)  │ <─────────────────── │                  │ <────────────────── │ metadata │
└─────────────┘                      └────────┬─────────┘                     └──────────┘
                                              │ multipart POST (server-side, có API key)
                                              ▼
                                     ┌──────────────────┐
                                     │  freeimage.host  │  (binary ảnh, public URL)
                                     └──────────────────┘
```

### 2.1 Tech stack

| Layer | Công nghệ |
|---|---|
| Frontend | Next.js 14 (App Router), TypeScript, TailwindCSS, TanStack Query, Zustand (UI state), dnd-kit (drag-drop) |
| Backend | .NET 8 Web API, Clean Architecture (API / Application / Domain / Infrastructure) |
| DB | MongoDB (MongoDB.Driver) |
| Auth | JWT Bearer (admin), public không cần token |
| Image host | freeimage.host API V1.1 |
| Deploy | Docker Compose (BE + FE + Mongo) — Dokploy-friendly |

---

## 3. Mô hình dữ liệu (MongoDB)

### 3.1 Collection `folders`

```jsonc
{
  "_id": "ObjectId",
  "name": "string",              // tên hiển thị, vd "Du lịch 2024"
  "parentId": "ObjectId|null",   // null = thư mục gốc (root)
  "path": "string",              // materialized path, vd "/<rootId>/<id>/" — kết bằng "/"
  "depth": "int",                // 0 = root
  "isDeleted": "bool",           // soft delete
  "createdAt": "DateTime(UTC)",
  "updatedAt": "DateTime(UTC)"
}
```

**Quy ước `path`:**
- Root: `path = "/<ownId>/"`, `depth = 0`, `parentId = null`.
- Con: `path = parent.path + "<ownId>/"`, `depth = parent.depth + 1`.
- Lấy toàn bộ con cháu: `path` regex `^/<targetId>/`.
- Breadcrumb: tách các ObjectId trong `path`, query 1 lần với `$in`.

**Indexes:**
```
{ parentId: 1, isDeleted: 1 }
{ path: 1 }
{ name: "text" }   // cho search (tùy chọn)
```

### 3.2 Collection `images`

```jsonc
{
  "_id": "ObjectId",
  "folderId": "ObjectId",        // thư mục chứa ảnh
  "name": "string",              // tên hiển thị (mặc định = tên file gốc)
  "url": "string",               // image.url từ freeimage (full size)
  "thumbUrl": "string",          // image.thumb.url (fallback = url nếu thiếu)
  "mediumUrl": "string|null",    // image.medium.url nếu có
  "width": "int|null",
  "height": "int|null",
  "sizeBytes": "long|null",
  "mimeType": "string|null",
  "freeimageId": "string|null",  // image.id_encoded — lưu để tham chiếu, KHÔNG xóa được
  "isDeleted": "bool",
  "uploadedAt": "DateTime(UTC)",
  "updatedAt": "DateTime(UTC)"
}
```

**Indexes:**
```
{ folderId: 1, isDeleted: 1 }
{ name: "text" }
```

### 3.3 Collection `users` (admin)

```jsonc
{
  "_id": "ObjectId",
  "username": "string",          // unique
  "passwordHash": "string",      // BCrypt
  "role": "string",              // "Admin"
  "createdAt": "DateTime(UTC)"
}
```
Seed 1 admin lúc khởi động từ config (`Admin:Username`, `Admin:Password`) nếu collection rỗng.

---

## 4. API Contract (.NET 8 Web API)

Base path: `/api`. Tất cả response JSON. Lỗi trả `ProblemDetails` (RFC 7807).

### 4.1 Public (không cần token)

#### `GET /api/folders/root`
Trả thư mục gốc (hoặc danh sách các root nếu cho phép nhiều root). Mặc định: 1 root ảo, trả children của root.

#### `GET /api/folders/{id}/children`
Nội dung 1 thư mục: thư mục con + ảnh.
```jsonc
// 200 OK
{
  "folder": { "id": "...", "name": "...", "parentId": "..." },
  "subFolders": [
    { "id": "...", "name": "...", "imageCount": 12, "subFolderCount": 3 }
  ],
  "images": [
    { "id": "...", "name": "...", "url": "...", "thumbUrl": "...", "width": 1920, "height": 1080 }
  ]
}
```
Query params: `?page=1&pageSize=50` (phân trang ảnh), `?sort=name|date&order=asc|desc`.

#### `GET /api/folders/{id}/breadcrumb`
```jsonc
// 200 OK — từ root tới folder hiện tại
[ { "id": "...", "name": "Root" }, { "id": "...", "name": "Du lịch" } ]
```

#### `GET /api/images/{id}`
Chi tiết 1 ảnh (cho lightbox).

### 4.2 Admin (yêu cầu `Authorization: Bearer <jwt>`)

#### `POST /api/auth/login`
```jsonc
// req
{ "username": "admin", "password": "..." }
// 200 OK
{ "token": "jwt...", "expiresAt": "ISO8601" }
// 401 nếu sai
```

#### `POST /api/folders`
```jsonc
// req
{ "name": "string", "parentId": "ObjectId|null" }
// 201 Created → { "id": "...", "name": "...", "parentId": "...", "path": "..." }
// 409 nếu trùng tên trong cùng parent
```

#### `PUT /api/folders/{id}`
Đổi tên và/hoặc di chuyển.
```jsonc
// req (các field optional)
{ "name": "string?", "parentId": "ObjectId|null?" }
// 200 OK
// QUAN TRỌNG: nếu đổi parentId → phải cập nhật `path` & `depth` của folder NÀY
//             và TẤT CẢ con cháu (bulk update bằng regex trên path).
// 400 nếu cố di chuyển folder vào chính con cháu của nó (vòng lặp).
```

#### `DELETE /api/folders/{id}`
Soft-delete đệ quy.
```jsonc
// query: ?cascade=true (mặc định). Nếu cascade=false và còn con → 409.
// Soft-delete folder + tất cả con cháu (folders & images) qua regex path.
// 200 OK → { "deletedFolders": n, "deletedImages": m }
// Response NHẮC: binary trên freeimage không bị xóa.
```

#### `POST /api/images/upload`
`multipart/form-data`.
```
fields:
  file: <binary>           (bắt buộc, ≤ 64MB)
  folderId: <ObjectId>     (bắt buộc)
  name: <string>           (optional, mặc định = filename)
```
Luồng xử lý server-side:
1. Validate file (mime ảnh, size ≤ 64MB).
2. Gọi freeimage.host (xem §5).
3. Parse JSON → lưu `images` document.
4. Trả về document.
```jsonc
// 201 Created → { image object }
// 502 nếu freeimage lỗi (kèm message)
```
#### `POST /api/images/upload-batch` — Upload NHIỀU file (xem chi tiết §5.4)
`multipart/form-data` với nhiều file cùng `folderId`.
```
fields:
  files: <binary[]>        (bắt buộc, mỗi file ≤ 64MB, tối đa 20 file/request)
  folderId: <ObjectId>     (bắt buộc)
```
Server xử lý **tuần tự** (không song song, tránh rate-limit freeimage). Mỗi file
độc lập: 1 file lỗi KHÔNG chặn các file còn lại.
```jsonc
// 207 Multi-Status → tổng kết kết quả từng file
{
  "total": 10,
  "succeeded": 8,
  "failed": 2,
  "results": [
    { "index": 0, "fileName": "a.jpg", "status": "success", "image": { /* image object */ } },
    { "index": 1, "fileName": "b.png", "status": "error", "error": "File quá 64MB" }
  ]
}
```

#### `DELETE /api/images/{id}`
Soft-delete metadata. Binary vẫn còn trên freeimage.
```jsonc
// 200 OK
```

#### `PUT /api/images/{id}`
Đổi tên / di chuyển ảnh.
```jsonc
{ "name": "string?", "folderId": "ObjectId?" }
// 200 OK
```

### 4.3 Mã lỗi chuẩn
`400` validation · `401` chưa auth · `403` sai quyền · `404` không thấy · `409` xung đột (trùng tên, vòng lặp) · `502` lỗi upstream (freeimage).

---

## 5. Tích hợp freeimage.host

### 5.1 Endpoint upload

```
POST https://freeimage.host/api/1/upload
Content-Type: multipart/form-data

fields:
  key:    <API_KEY>        (hoặc header X-API-Key: <API_KEY>)
  source: <binary file>    (hoặc base64 string, hoặc image URL)
  format: json
```

> **Lấy API key:** đăng ký tài khoản freeimage.host → Settings → API key (dạng `chv_...`).
> Có 1 public demo key `6d207e02198a847aa98d0a2a901485a5` nhưng KHÔNG dùng cho production (chung, không kiểm soát).

### 5.2 Response mẫu (rút gọn) cần parse

```jsonc
{
  "status_code": 200,
  "image": {
    "name": "abc",
    "extension": "jpg",
    "size": 123456,
    "width": 1920,
    "height": 1080,
    "id_encoded": "xxxx",
    "url": "https://iili.io/xxxx.jpg",
    "medium": { "url": "https://iili.io/xxxx.md.jpg" },
    "thumb": { "url": "https://iili.io/xxxx.th.jpg" }
  },
  "status_txt": "OK"
}
```
Mapping → `images`: `url`, `thumb.url`→thumbUrl, `medium.url`→mediumUrl, `width/height/size`, `id_encoded`→freeimageId. Nếu `thumb` thiếu → fallback `url`.

### 5.3 .NET HttpClient (mẫu)

```csharp
public async Task<FreeImageResult> UploadAsync(Stream fileStream, string fileName, CancellationToken ct)
{
    using var content = new MultipartFormDataContent();
    var fileContent = new StreamContent(fileStream);
    content.Add(fileContent, "source", fileName);
    content.Add(new StringContent("json"), "format");

    var req = new HttpRequestMessage(HttpMethod.Post, "https://freeimage.host/api/1/upload");
    req.Headers.Add("X-API-Key", _options.ApiKey);
    req.Content = content;

    var resp = await _httpClient.SendAsync(req, ct);
    var json = await resp.Content.ReadAsStringAsync(ct);
    // parse json, kiểm tra status_code == 200, ném exception nếu lỗi
}
```
Dùng `IHttpClientFactory` + Polly retry (3 lần, exponential backoff) cho lỗi tạm thời.

### 5.4 Upload nhiều file (Multi-file Upload) — ĐẶC TẢ CHI TIẾT

> Hỗ trợ chọn/kéo-thả nhiều ảnh cùng lúc. Đây là tính năng bắt buộc của v1 (làm ở Phase 2).

#### 5.4.1 Cách người dùng kích hoạt (Frontend)
- **Nút "Upload"**: mở file picker chọn nhiều ảnh — `<input type="file" multiple accept="image/*">`.
- **Kéo-thả nhiều file**: kéo nhiều ảnh từ máy thả vào content pane (UploadDropzone). Pane highlight viền khi hover file.
- Cả 2 cách upload vào **thư mục đang mở** (folderId hiện tại).

#### 5.4.2 Hàng đợi upload (Upload Queue UI)
Khi có file được chọn, hiện 1 panel hàng đợi (góc dưới phải hoặc dialog) liệt kê từng file:

| Cột | Nội dung |
|---|---|
| Thumbnail/icon | Preview cục bộ (objectURL) trước khi up |
| Tên file | Tên gốc |
| Tiến trình | Thanh % (đang up) |
| Trạng thái | `Đang chờ` / `Đang tải` / `Xong ✓` / `Lỗi ✗` (kèm lý do) |

- File **xong** → tự thêm thumbnail vào lưới ngay (optimistic, hoặc refetch folder).
- File **lỗi** → hiện lý do + nút **Thử lại** cho riêng file đó.
- Có nút **Đóng** panel khi hoàn tất; nút **Hủy** các file đang chờ.

#### 5.4.3 Validation phía client (trước khi gửi)
- Chỉ nhận mime ảnh (`image/*`).
- Mỗi file ≤ **64MB** → file vượt bị đánh dấu lỗi ngay, KHÔNG gửi lên.
- Tối đa **20 file/lần** → vượt thì cảnh báo, chỉ nhận 20 file đầu (hoặc chặn).

#### 5.4.4 Luồng backend (`POST /api/images/upload-batch`)
1. Nhận `files[]` + `folderId`. Validate folder tồn tại & chưa bị xóa.
2. Lặp **TUẦN TỰ** từng file (không `Task.WhenAll`) — tránh rate-limit freeimage:
   - Validate (magic bytes + size).
   - Gọi `IFreeImageClient.UploadAsync`.
   - Thành công → lưu document `images`, thêm vào `results` với `status: "success"`.
   - Thất bại → bắt exception, thêm `results` với `status: "error"` + message, **tiếp tục file kế tiếp** (không ném ra ngoài).
3. Trả `207 Multi-Status` với tổng kết (xem §4.2 `upload-batch`).

> **QUAN TRỌNG:** 1 file lỗi giữa chừng KHÔNG được rollback hay chặn các file đã/đang thành công. Mỗi file là 1 đơn vị độc lập.

#### 5.4.5 Khuyến nghị triển khai FE
- Có thể gọi `upload-batch` 1 lần (đơn giản) HOẶC gọi từng file qua `POST /api/images/upload` song song có giới hạn (concurrency 2-3) để cập nhật progress mượt hơn. **Chọn cách batch 1 request cho v1** để đơn giản; nếu cần progress per-file realtime thì dùng cách gọi lẻ với `XMLHttpRequest`/`onUploadProgress`.
- Dùng TanStack Query mutation; sau khi xong invalidate query của folder hiện tại.

---

## 6. Thiết kế giao diện — phong cách Windows File Explorer

### 6.1 Layout tổng (3 vùng)

```
┌────────────────────────────────────────────────────────────────────┐
│  Topbar:  [< >]  [Breadcrumb: Root > Du lịch > 2024 ]   [🔍 Search]  │ ← thanh điều hướng + địa chỉ
│           [Đăng nhập admin] hoặc [+ New folder] [⬆ Upload] [⚙]       │
├──────────────┬─────────────────────────────────────────────────────┤
│ Sidebar      │  Main pane (nội dung thư mục)                        │
│ (cây thư mục)│                                                       │
│              │  [Toolbar: ⊞ Icon  ☰ List  | Sort ▾ | Select ]      │
│ ▸ Root       │                                                       │
│   ▾ Du lịch  │   📁 Hạ Long   📁 Đà Lạt   🖼 IMG_01  🖼 IMG_02      │
│      2024    │   🖼 IMG_03    🖼 IMG_04    ...                       │
│   ▸ Công việc│                                                       │
└──────────────┴─────────────────────────────────────────────────────┘
```

### 6.2 Hành vi giống Windows Explorer (BẮT BUỘC)

| Tính năng | Mô tả |
|---|---|
| **Cây thư mục bên trái** | Tree view có nút expand/collapse (▸/▾). Click folder → load nội dung ở pane phải. |
| **Breadcrumb (address bar)** | Hiển thị đường dẫn; click từng cấp để nhảy nhanh. Có nút Back/Forward. |
| **Double-click thư mục** | Mở vào trong (đi xuống cấp con). |
| **Double-click ảnh** | Mở lightbox xem full size. |
| **2 chế độ xem** | "Icon" (lưới thumbnail) và "List" (danh sách + cột: tên, kích thước, ngày). Toggle ở toolbar. |
| **Sort** | Theo tên / ngày / kích thước, tăng/giảm. |
| **Single click** | Chọn (highlight). Ctrl/Shift+click chọn nhiều. |
| **Right-click → context menu** | (admin) Mở / Đổi tên / Xóa / Di chuyển / (ảnh) Copy link. |
| **Drag & drop** | (admin) Kéo ảnh/thư mục thả vào thư mục khác để di chuyển. Kéo file từ máy vào pane để upload. |
| **Folder icon vàng + thumbnail ảnh** | Icon thư mục kiểu Windows (vàng), ảnh hiển thị thumbnail thật. |
| **Empty state** | Thư mục rỗng hiển thị "Thư mục trống". |
| **Loading skeleton** | Khi tải nội dung. |

### 6.3 Phân biệt Public vs Admin trên UI

- **Public**: ẩn toàn bộ nút tạo/sửa/xóa/upload, ẩn context menu chỉnh sửa (chỉ còn "Mở", "Copy link"). Drag-drop tắt.
- **Admin** (sau login): hiện đầy đủ. Có badge "Admin mode" + nút Đăng xuất.
- Khi admin xóa: hiện dialog xác nhận **kèm cảnh báo** "Ảnh trên freeimage.host sẽ KHÔNG bị xóa, chỉ ẩn khỏi kho."

### 6.4 Responsive
- Desktop: 3 vùng như trên.
- Mobile/tablet: sidebar cây thư mục thu vào drawer (hamburger). Main pane full width, chế độ icon mặc định 2-3 cột.

### 6.5 Lightbox (xem ảnh)
- Nền tối, ảnh full-size ở giữa, nút ← → chuyển ảnh trong cùng thư mục, ESC/click nền để đóng, nút tải/copy link, hiển thị tên + kích thước.

---

## 7. Bảo mật & cấu hình

- JWT: HS256, secret từ env, expiry ví dụ 8h. Chỉ endpoint admin gắn `[Authorize(Roles="Admin")]`.
- CORS: chỉ cho phép origin của FE.
- Rate limit endpoint upload (vd 30 req/phút/IP).
- Validate kỹ file upload: kiểm tra magic bytes (không chỉ tin extension), giới hạn mime ảnh, size ≤ 64MB.
- API key freeimage chỉ ở backend (env var), KHÔNG bao giờ trả về FE.
- Secrets qua biến môi trường (xem §8).

---

## 8. Cấu hình & biến môi trường

### Backend (`appsettings` / env)
```
MONGO__CONNECTIONSTRING=mongodb://mongo:27017
MONGO__DATABASE=image_vault
FREEIMAGE__APIKEY=chv_xxx
FREEIMAGE__BASEURL=https://freeimage.host/api/1/upload
JWT__SECRET=<random-32+ chars>
JWT__ISSUER=image-vault
JWT__EXPIRYHOURS=8
ADMIN__USERNAME=admin
ADMIN__PASSWORD=<đổi-ngay>
CORS__ALLOWEDORIGINS=https://your-frontend-domain
```

### Frontend (env)
```
NEXT_PUBLIC_API_BASE_URL=https://your-api-domain/api
```

---

## 9. Docker Compose (deploy)

3 service: `api`, `web`, `mongo` (+ volume cho mongo). FE gọi API qua `NEXT_PUBLIC_API_BASE_URL`. Cho phép chạy sau reverse proxy (Dokploy/Traefik). Healthcheck cho api & mongo. (Agent tự sinh compose theo skill docker-compose, nhớ kèm `.env.example`.)

---

## 10. Cấu trúc thư mục code đề xuất

### Backend (.NET 8, Clean Architecture)
```
src/
  ImageVault.Domain/          # entities: Folder, ImageItem, User
  ImageVault.Application/      # interfaces, DTOs, use-cases (FolderService, ImageService, AuthService)
  ImageVault.Infrastructure/   # Mongo repositories, FreeImageClient, JWT, password hashing
  ImageVault.Api/              # controllers, DI, middleware, Program.cs
tests/
  ImageVault.UnitTests/
```

### Frontend (Next.js App Router)
```
app/
  page.tsx                     # vault explorer (public)
  login/page.tsx
components/
  explorer/
    FolderTree.tsx             # sidebar cây
    Breadcrumb.tsx
    ContentPane.tsx            # icon/list view
    ItemGrid.tsx  ItemList.tsx
    ContextMenu.tsx
    Lightbox.tsx
    UploadDropzone.tsx
    Toolbar.tsx
  ui/                          # nút, dialog, skeleton
lib/
  api.ts                       # client gọi BE (axios/fetch + TanStack Query hooks)
  auth.ts                      # lưu/đọc JWT
store/
  uiStore.ts                   # view mode, selection, navigation history
```

---

## 11. Acceptance Criteria (agent phải đạt)

1. Public mở web → thấy cây thư mục + duyệt được, xem ảnh trong lightbox, KHÔNG thấy nút sửa/xóa/upload.
2. Admin login thành công → nhận JWT, hiện đầy đủ chức năng quản lý.
3. Tạo thư mục con nhiều cấp; breadcrumb & cây cập nhật đúng.
4. Upload ảnh:
   - Chọn nhiều file qua nút Upload VÀ kéo-thả nhiều file đều hoạt động (§5.4).
   - Hàng đợi hiển thị tiến trình + trạng thái từng file.
   - 1 file lỗi (vd quá 64MB) không chặn các file còn lại; có nút thử lại.
   - Ảnh thành công lưu freeimage + metadata Mongo, hiện thumbnail ngay.
5. Đổi tên & di chuyển thư mục → `path`/`depth` của con cháu cập nhật đúng; chặn di chuyển vào con cháu chính nó.
6. Xóa thư mục/ảnh → soft delete, biến mất khỏi UI, hiện cảnh báo binary freeimage không bị xóa.
7. Drag-drop di chuyển ảnh giữa thư mục (admin) hoạt động.
8. 2 chế độ xem icon/list + sort hoạt động.
9. API key freeimage không lộ ra phía client (kiểm tra network tab).
10. Chạy được bằng `docker compose up`.

---

## 12. Roadmap mở rộng (ngoài phạm vi v1, ghi để định hướng)

- Multi-admin / phân quyền chi tiết.
- Tự host binary (S3/MinIO) thay freeimage để có thể xóa thật + kiểm soát quyền riêng tư.
- Tìm kiếm ảnh theo tên/tag, gắn tag.
- Thùng rác (khôi phục soft-deleted).
- Bulk upload qua kéo cả thư mục từ máy.

---

## 13. Lưu ý cuối cho agent

- **Bám sát §1.1**: không viết logic "gọi freeimage để xóa ảnh" — API không hỗ trợ. Mọi delete là soft delete metadata.
- Ưu tiên đúng hành vi UX Explorer ở §6.2 hơn là làm đẹp.
- Đọc kỹ §3.1 về materialized path trước khi viết logic move/delete đệ quy — đây là phần dễ sai nhất.
- Viết file `SKILL.md`/`CLAUDE.md` riêng cho repo nếu cần điều phối nhiều agent.
