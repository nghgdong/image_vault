# Image Vault — PLAN (Kế hoạch triển khai)

> Đọc cùng `IMAGE_VAULT_SPEC.md`. File này định nghĩa thứ tự phase, milestone, và
> cổng review giữa các phase (human review gate). Task chi tiết ở `IMAGE_VAULT_TASKS.md`.

---

## Nguyên tắc thực thi

- **Doc-first**: SPEC đã chốt → PLAN → TASKS → code. Không nhảy cóc.
- **Mỗi phase có cổng review**: agent dừng, báo cáo, chờ người duyệt mới sang phase sau.
- **Build + test phải xanh** trước khi đóng 1 phase.
- Bám ràng buộc §1.1 SPEC: freeimage KHÔNG xóa được binary → mọi delete là soft delete.

---

## Tổng quan các Phase

| Phase | Tên | Mục tiêu | Đầu ra | Trạng thái |
|---|---|---|---|---|
| 0 | Khởi tạo | Repo, solution, docs, CI cơ bản | Skeleton chạy được | ☐ |
| 1 | Backend core | Domain + Mongo + auth + endpoint public | API public + login chạy | ☐ |
| 2 | Admin + freeimage | Endpoint admin + upload (đơn & nhiều file) thật | CRUD đầy đủ, upload thật | ☐ |
| 3 | Frontend Explorer | UI kiểu Windows Explorer, public + admin | Web hoàn chỉnh | ☐ |
| 4 | Hoàn thiện & deploy | Test, hardening, Docker, deploy Dokploy | Production-ready | ☐ |

---

## Phase 0 — Khởi tạo (½ ngày)

**Mục tiêu:** Nền móng repo + tài liệu + môi trường chạy.

- Tạo repo, cấu trúc thư mục `src/`, `tests/`, `web/`.
- Copy `IMAGE_VAULT_SPEC.md`, `PLAN.md`, `TASKS.md`, tạo `CLAUDE.md` (quy tắc cho agent).
- Tạo .NET 8 solution rỗng theo §10 (4 project) + project test.
- `docker-compose.yml` chỉ với mongo (để dev local).
- `.gitignore`, `.editorconfig`, `README.md`.

**Cổng review 0:** Solution build trống thành công, `docker compose up mongo` chạy.

---

## Phase 1 — Backend core (2-3 ngày)

**Mục tiêu:** API public + auth, materialized path, Mongo, freeimage STUB.

- Entities: Folder, ImageItem, User (§3).
- Mongo repositories + đăng ký index lúc khởi động.
- Seed admin từ config (BCrypt).
- JWT auth + `[Authorize(Roles="Admin")]`.
- Logic materialized path: tạo/move folder (cập nhật path+depth con cháu), soft delete đệ quy, chặn move vòng lặp.
- Endpoint PUBLIC (§4.1): root, children (phân trang+sort), breadcrumb, image detail.
- Endpoint `POST /api/auth/login`.
- `IFreeImageClient` + stub (chưa gọi thật).
- Unit test cho logic path.
- ProblemDetails cho lỗi.

**Cổng review 1:** build+test xanh; gọi được endpoint public + login bằng REST client; index tạo đúng trong Mongo.

---

## Phase 2 — Admin + freeimage thật (2-3 ngày)

**Mục tiêu:** CRUD đầy đủ + upload thật (đơn & nhiều file).

- Endpoint admin (§4.2): tạo/đổi tên/move/xóa folder; xóa/đổi tên/move image.
- Nối `FreeImageClient` thật (§5): HttpClient + Polly retry, đọc API key từ env.
- `POST /api/images/upload` (1 file).
- `POST /api/images/upload-batch` (nhiều file, tuần tự, 207 Multi-Status — §5.4).
- Validate file: magic bytes + size ≤ 64MB + tối đa 20 file/batch.
- Rate limit endpoint upload.
- Integration test cho upload (mock freeimage) + CRUD folder.

**Cổng review 2:** upload 1 và nhiều file chạy thật lên freeimage; CRUD folder/image đúng; con cháu cập nhật path khi move; soft delete không gọi xóa freeimage.

---

## Phase 3 — Frontend Explorer (3-4 ngày)

**Mục tiêu:** Web UI kiểu Windows File Explorer, phân biệt public/admin.

- Next.js 14 App Router + TS + Tailwind + TanStack Query + Zustand + dnd-kit.
- API client + auth (lưu JWT).
- Sidebar cây thư mục (expand/collapse), breadcrumb + back/forward.
- Content pane: chế độ Icon & List, sort, chọn nhiều (Ctrl/Shift).
- Double-click folder mở vào, double-click ảnh mở lightbox.
- Lightbox (← →, ESC, copy/tải link).
- Admin: context menu (đổi tên/xóa/move/copy link), dialog xác nhận xóa kèm cảnh báo freeimage.
- Upload: nút chọn nhiều file + kéo-thả nhiều file + hàng đợi tiến trình (§5.4).
- Drag-drop di chuyển ảnh/thư mục giữa folder.
- Responsive (sidebar thu drawer trên mobile).
- Empty state + loading skeleton.

**Cổng review 3:** public duyệt+xem được, không thấy nút sửa; admin làm đủ chức năng; upload nhiều file + hàng đợi chạy; API key không lộ ở network tab.

---

## Phase 4 — Hoàn thiện & deploy (1-2 ngày)

**Mục tiêu:** Production-ready trên Dokploy.

- `docker-compose.yml` đủ 3 service (api+web+mongo) + healthcheck + volume.
- `.env.example` đầy đủ.
- CORS chỉ cho origin FE; secrets qua env.
- Hardening: rate limit, validate, security headers.
- README hướng dẫn chạy + deploy Dokploy/Traefik.
- Kiểm tra toàn bộ Acceptance Criteria (§11 SPEC).
- Smoke test trên môi trường deploy.

**Cổng review 4:** `docker compose up` chạy full stack; toàn bộ §11 đạt; deploy thử thành công.

---

## Phụ thuộc & rủi ro

| Rủi ro | Ảnh hưởng | Giảm thiểu |
|---|---|---|
| freeimage không xóa được binary | Ảnh tồn tại vĩnh viễn | Soft delete + cảnh báo UI; roadmap chuyển S3/MinIO |
| freeimage rate-limit / downtime | Upload fail | Polly retry; upload-batch tuần tự; thông báo lỗi rõ |
| Logic materialized path sai khi move | Hỏng cây thư mục | Unit test kỹ Phase 1; chặn move vòng lặp |
| Ảnh public hoàn toàn | Rò rỉ ảnh nhạy cảm | Cảnh báo rõ; không dùng cho dữ liệu riêng tư |

---

## Ước lượng tổng

~10-15 ngày công 1 dev (nhanh hơn nhiều nếu dùng coding agent scaffold). Có thể làm BE (Phase 1-2) và FE (Phase 3) song song nếu chốt API contract sớm.
