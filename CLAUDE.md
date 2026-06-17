# CLAUDE.md — Quy tắc cho AI Agent (Image Vault)

> File này được agent đọc ở MỌI phiên làm việc trong repo. Tuân thủ tuyệt đối.
> Nguồn chân lý kỹ thuật: `IMAGE_VAULT_SPEC.md`. Kế hoạch: `IMAGE_VAULT_PLAN.md`.
> Công việc: `IMAGE_VAULT_TASKS.md`.
> **Quy trình làm việc chi tiết: `.agents/skills/image-vault-workflow/SKILL.md`** — đọc đầu mỗi phiên.

---

## 1. Bối cảnh dự án (1 đoạn)

Image Vault là web app kho ảnh có cây thư mục lồng nhau, giao diện kiểu Windows
File Explorer. Public duyệt + xem ảnh (read-only); Admin (đăng nhập JWT) quản lý
thư mục & ảnh. Binary ảnh lưu trên **freeimage.host**, metadata (cây thư mục, vị
trí ảnh) lưu trên **MongoDB**. Backend: .NET 8 Web API (Clean Architecture).
Frontend: Next.js 14 + TypeScript.

---

## 2. Quy trình làm việc (BẮT BUỘC)

1. **Doc-first**: luôn đọc SPEC trước khi code. Không suy diễn ngoài SPEC.
2. **Bám PLAN theo phase**: làm đúng phase hiện tại, KHÔNG làm task của phase sau.
3. **Cổng review**: hết mỗi phase, DỪNG lại, báo cáo tóm tắt + chờ người duyệt.
4. **Cập nhật TASKS.md**: đánh dấu `[x]` task đã xong sau khi hoàn thành + test.
5. **Trước khi code 1 task lớn**: tóm tắt hiểu biết 3-5 dòng, liệt kê file sẽ tạo,
   nêu điểm mơ hồ (nếu có) → HỎI trước, không đoán.
6. **Build + test phải XANH** trước khi báo xong 1 phase.
7. **Xong mỗi task → commit + push git** (message tham chiếu task ID, vd `P2-10: ...`),
   1 task = 1 commit nhỏ. Chi tiết git ở skill `image-vault-workflow`.

---

## 3. Ràng buộc TUYỆT ĐỐI (không được vi phạm)

> Đây là các lỗi nghiêm trọng nhất. Vi phạm = làm lại.

- ❌ **KHÔNG viết logic gọi freeimage.host để XÓA ảnh.** Guest API không có
  endpoint delete (SPEC §1.1). Mọi thao tác "xóa" là **soft delete metadata**
  trong MongoDB (`isDeleted = true`). Binary trên freeimage tồn tại vĩnh viễn.
- ❌ **KHÔNG để API key freeimage lộ ra frontend.** Upload LUÔN đi qua backend.
  FE không bao giờ gọi freeimage trực tiếp. Key đọc từ biến môi trường, không
  hardcode.
- ❌ **KHÔNG hardcode secret** (JWT secret, mật khẩu admin, connection string).
  Tất cả qua env (SPEC §8).
- ❌ **KHÔNG dùng hard delete.** Chỉ soft delete (`isDeleted`).
- ❌ **KHÔNG giả định ảnh là riêng tư.** Ảnh freeimage public tuyệt đối; phân
  quyền admin chỉ bảo vệ metadata/UI. UI phải cảnh báo rõ khi xóa.

---

## 4. Quy tắc kiến trúc

### Backend (.NET 8, Clean Architecture)
- Phân tầng đúng §10: `Domain` (entities, không phụ thuộc gì) → `Application`
  (interfaces, DTO, use-case) → `Infrastructure` (Mongo, freeimage, JWT) → `Api`
  (controller, DI). Phụ thuộc chỉ hướng vào trong.
- Business logic + transaction/security-critical code: viết cẩn thận, không để
  agent tự "sáng tạo" ngoài SPEC. Boilerplate/scaffolding thì tự do.
- Lỗi trả **ProblemDetails (RFC 7807)** (SPEC §4.3).
- Mã lỗi đúng chuẩn: 400/401/403/404/409/502 (SPEC §4.3).

### Cây thư mục — materialized path (SPEC §3.1) — PHẦN DỄ SAI NHẤT
- `path` dạng `/<rootId>/<childId>/...`, kết thúc bằng `/`.
- Tạo folder: `path = parent.path + ownId + "/"`, `depth = parent.depth + 1`.
- **Move folder**: cập nhật `path` + `depth` của CHÍNH NÓ **và toàn bộ con cháu**
  (bulk update bằng regex `^<oldPath>`). KHÔNG quên con cháu.
- **Chặn move vào con cháu của chính nó** (vòng lặp) → trả 400.
- **Soft delete đệ quy**: xóa folder → soft delete tất cả folders & images có
  `path` khớp regex `^/.../<id>/`.
- Breadcrumb: tách ObjectId từ `path`, query `$in` 1 lần (không loop từng cấp).

### Upload nhiều file (SPEC §5.4)
- Backend `upload-batch` xử lý **TUẦN TỰ** (không `Task.WhenAll`) tránh rate-limit.
- 1 file lỗi KHÔNG chặn file khác; trả **207 Multi-Status** kèm kết quả từng file.
- Validate: magic bytes (không chỉ extension) + mime ảnh + size ≤ 64MB + ≤ 20 file.

### Frontend (Next.js 14)
- **BẮT BUỘC** theo skill `.agents/skills/design-taste-frontend-v1/SKILL.md` cho mọi
  UI/component/CSS (anti-slop, không emoji, không Inter, không "AI purple", đủ trạng thái
  loading/empty/error). Đọc trước khi viết FE.
- App Router + TS + Tailwind + TanStack Query + Zustand + dnd-kit.
- Ưu tiên **đúng hành vi UX Windows Explorer** (SPEC §6.2) hơn là làm đẹp.
- Public: ẩn toàn bộ nút sửa/xóa/upload + tắt drag-drop + context menu rút gọn.
- Sau mutation: invalidate query của folder hiện tại.

---

## 5. Quy ước code

- C#: nullable enabled, async/await, `CancellationToken` cho I/O, DI qua interface.
- Đặt tên: PascalCase (C# public), camelCase (TS), kebab-case (file FE).
- Mongo: dùng `MongoDB.Driver`, ObjectId cho `_id`, UTC cho mọi DateTime.
- TS: strict mode, không `any` trừ khi bất khả kháng.
- Commit nhỏ, theo task; message tham chiếu task ID (vd `P1-9: folder path logic`).

---

## 6. Lệnh thường dùng

```bash
# Backend
dotnet build
dotnet test
dotnet run --project src/ImageVault.Api

# Frontend
cd web && npm install && npm run dev

# Docker (full stack)
docker compose up --build
```

---

## 7. Definition of Done (mỗi task)

- [ ] Code đúng SPEC, không vi phạm §3 (ràng buộc tuyệt đối) ở trên.
- [ ] Build xanh, không warning nghiêm trọng.
- [ ] Có test cho logic quan trọng (path, auth, upload).
- [ ] Đánh dấu `[x]` trong `IMAGE_VAULT_TASKS.md`.
- [ ] Không lộ secret, không hardcode key.

---

## 8. Khi nghi ngờ

Nếu SPEC thiếu/mâu thuẫn, hoặc một quyết định có thể ảnh hưởng kiến trúc/bảo mật:
**DỪNG và HỎI người dùng**, kèm 2-3 phương án + đề xuất. Không tự quyết âm thầm.
