# Image Vault — TASKS (Bảng công việc)

> Theo dõi tiến độ chi tiết. Mỗi task có ID, đánh dấu `[ ]` chưa làm / `[x]` xong.
> Tham chiếu §x trỏ tới `IMAGE_VAULT_SPEC.md`. Phase xem `IMAGE_VAULT_PLAN.md`.

Quy ước ID: `P{phase}-{số}`.

---

## Phase 0 — Khởi tạo

- [x] **P0-1** Tạo repo + cấu trúc thư mục `src/`, `tests/`, `web/`
- [x] **P0-2** Thêm `IMAGE_VAULT_SPEC.md`, `IMAGE_VAULT_PLAN.md`, `IMAGE_VAULT_TASKS.md` vào repo
- [x] **P0-3** Tạo `CLAUDE.md` (quy tắc agent: doc-first, ràng buộc §1.1, không xóa freeimage)
- [x] **P0-4** Tạo .NET 8 solution + 4 project theo §10 (Domain/Application/Infrastructure/Api)
- [x] **P0-5** Tạo project test `ImageVault.UnitTests`
- [x] **P0-6** `docker-compose.yml` (api + mongo) + `.env.example` (mở rộng hơn yêu cầu Phase 0)
- [x] **P0-7** `.gitignore`, `.editorconfig`, `README.md` khung
- [x] **P0-8** ✅ Cổng review 0: solution build OK

---

## Phase 1 — Backend core

### Domain & DB
- [x] **P1-1** Entity `Folder` (name, parentId, path, depth, isDeleted, timestamps) §3.1
- [x] **P1-2** Entity `ImageItem` §3.2
- [x] **P1-3** Entity `User` §3.3
- [x] **P1-4** Mongo context + đăng ký collection
- [x] **P1-5** Đăng ký index lúc khởi động (folders, images, users) §3
- [x] **P1-6** Seed admin từ config nếu users rỗng (BCrypt) §3.3

### Repositories & logic path
- [x] **P1-7** `IFolderRepository` + impl Mongo
- [x] **P1-8** `IImageRepository` + impl Mongo
- [x] **P1-9** Logic tạo folder: set `path`/`depth` đúng §3.1
- [x] **P1-10** Logic move folder: cập nhật path+depth CHÍNH NÓ + toàn bộ con cháu (regex path)
- [x] **P1-11** Chặn move folder vào con cháu của chính nó (vòng lặp) → 400
- [x] **P1-12** Soft delete đệ quy folder + con cháu (folders & images)
- [x] **P1-13** Breadcrumb: tách ObjectId từ path, query `$in` 1 lần

### Auth
- [x] **P1-14** JWT service (issue token HS256, expiry config) §7
- [x] **P1-15** Password hashing (BCrypt verify)
- [x] **P1-16** `POST /api/auth/login` §4.2
- [x] **P1-17** Middleware `[Authorize(Roles="Admin")]` (hạ tầng auth + role claim sẵn sàng; endpoint admin gắn ở Phase 2)

### Endpoint public
- [x] **P1-18** `GET /api/folders/root` §4.1
- [x] **P1-19** `GET /api/folders/{id}/children` + phân trang + sort §4.1
- [x] **P1-20** `GET /api/folders/{id}/breadcrumb` §4.1
- [x] **P1-21** `GET /api/images/{id}` §4.1

### Hạ tầng & chất lượng
- [x] **P1-22** `IFreeImageClient` interface + STUB (dữ liệu giả) §5
- [x] **P1-23** ProblemDetails (RFC 7807) cho toàn bộ lỗi §4.3
- [x] **P1-24** Unit test: tạo folder set path đúng
- [x] **P1-25** Unit test: move folder cập nhật con cháu + chặn vòng lặp
- [x] **P1-26** ✅ Cổng review 1: build+test xanh, gọi được public API + login

---

## Phase 2 — Admin + freeimage thật

### CRUD folder
- [x] **P2-1** `POST /api/folders` (tạo, chặn trùng tên trong parent → 409) §4.2
- [x] **P2-2** `PUT /api/folders/{id}` (đổi tên + move) §4.2
- [x] **P2-3** `DELETE /api/folders/{id}` (soft delete đệ quy, cascade flag, trả số lượng) §4.2

### CRUD image
- [x] **P2-4** `DELETE /api/images/{id}` (soft delete metadata, KHÔNG gọi freeimage) §4.2
- [x] **P2-5** `PUT /api/images/{id}` (đổi tên + move) §4.2

### freeimage thật
- [x] **P2-6** `FreeImageClient` thật: HttpClient + multipart + X-API-Key §5.3
- [x] **P2-7** Parse response + mapping sang `images` document §5.2
- [x] **P2-8** Polly retry (3 lần, backoff) cho lỗi tạm thời §5.3
- [x] **P2-9** Đọc API key từ env, không hardcode §7/§8 (rỗng → fallback stub)

### Upload
- [x] **P2-10** `POST /api/images/upload` (1 file) §4.2
- [x] **P2-11** Validate file: magic bytes + mime ảnh + size ≤ 64MB
- [x] **P2-12** `POST /api/images/upload-batch` (nhiều file, TUẦN TỰ) §5.4
- [x] **P2-13** 1 file lỗi không chặn file khác; trả 207 Multi-Status §5.4.4
- [x] **P2-14** Giới hạn tối đa 20 file/batch
- [x] **P2-15** Rate limit endpoint upload §7

### Test
- [x] **P2-16** Integration test: CRUD folder + move cập nhật con cháu
- [x] **P2-17** Integration test: upload (mock freeimage) đơn & batch
- [x] **P2-18** ✅ Cổng review 2: upload thật chạy, CRUD đúng, soft delete không xóa freeimage

---

## Phase 3 — Frontend Explorer

### Nền tảng
- [x] **P3-1** Khởi tạo Next.js 14 + TS + Tailwind + TanStack Query + Zustand + dnd-kit
- [x] **P3-2** API client (`lib/api.ts`) + hooks TanStack Query
- [x] **P3-3** Auth: login page, lưu/đọc JWT, gắn token vào request §6.3

### Khung Explorer
- [x] **P3-4** Layout 3 vùng: topbar + sidebar + main pane §6.1
- [x] **P3-5** Sidebar cây thư mục: expand/collapse, click load nội dung §6.2
- [x] **P3-6** Breadcrumb (address bar) + nút Back/Forward §6.2
- [x] **P3-7** Content pane: chế độ Icon (lưới thumbnail) §6.2
- [x] **P3-8** Content pane: chế độ List (cột tên/kích thước/ngày) §6.2
- [x] **P3-9** Toggle Icon/List + Sort (tên/ngày/kích thước) §6.2
- [x] **P3-10** Chọn 1 / nhiều (Ctrl/Shift+click) §6.2
- [x] **P3-11** Double-click folder mở vào; double-click ảnh mở lightbox §6.2

### Lightbox
- [x] **P3-12** Lightbox: full-size, ← →, ESC, copy/tải link, tên+kích thước §6.5

### Chức năng admin
- [x] **P3-13** Context menu (right-click): mở/đổi tên/xóa/move/copy link §6.2
- [x] **P3-14** Dialog xác nhận xóa + cảnh báo binary freeimage không bị xóa §6.3
- [x] **P3-15** Tạo folder mới (nút + dialog)
- [x] **P3-16** Drag-drop di chuyển ảnh/folder giữa thư mục §6.2

### Upload UI (§5.4)
- [x] **P3-17** Nút Upload chọn nhiều file (`multiple accept=image/*`) §5.4.1
- [x] **P3-18** Kéo-thả nhiều file vào pane (UploadDropzone) §5.4.1
- [x] **P3-19** Hàng đợi upload: thumbnail + tên + trạng thái §5.4.2
- [x] **P3-20** Validate client (≤64MB, ≤20 file) + nút thử lại file lỗi §5.4.3
- [x] **P3-21** Sau upload xong → cập nhật lưới (invalidate query) §5.4.5

### Phân quyền & responsive
- [x] **P3-22** Public: ẩn nút sửa/xóa/upload, tắt context menu chỉnh sửa + drag-drop §6.3
- [x] **P3-23** Admin: badge "Admin mode" + nút đăng xuất §6.3
- [x] **P3-24** Responsive: sidebar thu drawer trên mobile §6.4
- [x] **P3-25** Empty state + loading skeleton §6.2
- [x] **P3-26** ✅ Cổng review 3: public/admin đúng, upload nhiều file chạy, API key không lộ

---

## Phase 4 — Hoàn thiện & deploy

- [ ] **P4-1** `docker-compose.yml` đủ 3 service (api+web+mongo) + healthcheck + volume §9
- [ ] **P4-2** `.env.example` đầy đủ biến §8
- [ ] **P4-3** CORS chỉ cho origin FE §7
- [ ] **P4-4** Security headers + rà soát validate/rate limit §7
- [ ] **P4-5** README: hướng dẫn chạy local + deploy Dokploy/Traefik
- [ ] **P4-6** Chạy checklist Acceptance Criteria §11 (10 mục)
- [ ] **P4-7** Smoke test trên môi trường deploy
- [ ] **P4-8** ✅ Cổng review 4: full stack chạy, §11 đạt, deploy OK

---

## Tổng tiến độ

| Phase | Task | Xong |
|---|---|---|
| 0 | 8 | 8 |
| 1 | 26 | 26 |
| 2 | 18 | 18 |
| 3 | 26 | 26 |
| 4 | 8 | 0 |
| **Tổng** | **86** | **78** |
