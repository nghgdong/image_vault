---
name: image-vault-workflow
description: Quy trình làm việc bắt buộc cho dự án Image Vault. Đọc ở đầu MỌI phiên code. Bọc quanh CLAUDE.md/SPEC/PLAN/TASKS, thêm 2 luật cứng — frontend phải theo skill design-taste-frontend-v1, và sau mỗi task phải đánh dấu [x] trong TASKS.md rồi commit+push git.
---

# Image Vault — Workflow Skill

> Skill này KHÔNG thay thế `CLAUDE.md` / `IMAGE_VAULT_SPEC.md` / `IMAGE_VAULT_PLAN.md` /
> `IMAGE_VAULT_TASKS.md` — nó điều phối cách áp dụng chúng và bổ sung 2 luật cứng.
> Thứ tự chân lý: SPEC (kỹ thuật) → PLAN (phase) → TASKS (việc) → CLAUDE.md (quy tắc).

## 0. Nạp ngữ cảnh trước khi làm (BẮT BUỘC)
Đầu mỗi phiên / trước mỗi task, đọc theo thứ tự:
1. `CLAUDE.md` — ràng buộc tuyệt đối (§3) + quy ước.
2. `IMAGE_VAULT_PLAN.md` — xác định **phase hiện tại**. CHỈ làm task trong phase đó.
3. `IMAGE_VAULT_TASKS.md` — chọn task `[ ]` kế tiếp theo thứ tự ID `P{phase}-{n}`.
4. Phần SPEC liên quan tới task (vd §3.1 cho path, §5.4 cho upload).

## 1. Hai luật cứng của skill này

### Luật A — Frontend phải theo design-taste-frontend-v1
Mọi việc động tới UI/Next.js (Phase 3 trở đi, hoặc bất kỳ file trong `web/`):
- **BẮT BUỘC** đọc và tuân thủ `.agents/skills/design-taste-frontend-v1/SKILL.md`
  TRƯỚC khi viết component/markup/CSS.
- Áp các directive của skill đó: anti-slop, không emoji, không font Inter, không "AI purple",
  kiểm tra `package.json` trước khi import lib, trạng thái loading/empty/error đầy đủ,
  `min-h-[100dvh]` thay `h-screen`, animate bằng `transform`/`opacity`.
- KHÔNG mâu thuẫn với SPEC §6: ưu tiên **đúng hành vi UX Windows Explorer** hơn là làm đẹp;
  Public phải ẩn nút sửa/xóa/upload + tắt drag-drop (SPEC §6.3).

### Luật B — Xong task → đánh dấu + commit + push
Ngay khi 1 task đạt Definition of Done (CLAUDE.md §7):
1. Sửa `IMAGE_VAULT_TASKS.md`: đổi `[ ]` → `[x]` cho đúng task ID, cập nhật bảng "Tổng tiến độ".
2. `git add -A` các file của task.
3. `git commit` với message tham chiếu task ID (xem mẫu §3).
4. `git push` lên remote của nhánh hiện tại.
> 1 task = 1 commit nhỏ. KHÔNG gộp nhiều task chưa liên quan vào 1 commit.
> Nếu push fail (chưa có remote/chưa init) → xem §4, báo người dùng, KHÔNG bịa remote.

## 2. Vòng lặp 1 task
```
[ Đọc SPEC của task ]
        ↓
[ Task lớn? → tóm tắt 3-5 dòng + liệt kê file + nêu điểm mơ hồ → HỎI nếu cần ]  (CLAUDE.md §2.5)
        ↓
[ Code đúng SPEC, không vi phạm ràng buộc §3 ]
        ↓
[ Frontend? → áp Luật A (design-taste-frontend-v1) ]
        ↓
[ Build + test XANH ]  (dotnet build/test; web: lint/build)
        ↓
[ Luật B: [x] TASKS.md → commit → push ]
        ↓
[ Hết phase? → DỪNG ở Cổng review, báo cáo, CHỜ DUYỆT ]  (CLAUDE.md §2.3)
```

## 3. Mẫu commit message
Một dòng tiêu đề tham chiếu task ID, kèm trailer đồng tác giả:
```
P2-10: upload 1 file qua backend (multipart, validate size)

- Thêm POST /api/images/upload
- Validate mime + magic bytes + size ≤ 64MB

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
```
- Tiền tố `P{phase}-{n}` đúng ID trong TASKS.md.
- Tiếng Việt, ngắn gọn, mô tả thay đổi thực tế.

## 4. Git — khởi tạo lần đầu (nếu repo chưa có git)
Repo có thể chưa được `git init`. Trước khi áp Luật B lần đầu:
1. Kiểm tra: `git rev-parse --is-inside-work-tree`. Nếu lỗi → chưa có git.
2. `git init` (đã có sẵn `.gitignore` loại trừ `bin/`, `obj/`, `.env`, `node_modules/`).
3. **HỎI người dùng URL remote** (vd GitHub) — KHÔNG tự bịa. `git remote add origin <url>`.
4. Commit nền + `git push -u origin <branch>`.
> KHÔNG bao giờ commit `.env` hay secret thật. Đã có `.env.example` làm mẫu.
> KHÔNG dùng `--no-verify` / bỏ qua hook trừ khi người dùng yêu cầu.

## 5. Nhắc lại ràng buộc TUYỆT ĐỐI (CLAUDE.md §3 — vi phạm = làm lại)
- KHÔNG gọi freeimage để XÓA ảnh → mọi xóa là **soft delete** (`isDeleted=true`).
- KHÔNG lộ API key freeimage ra FE; upload luôn qua backend; key đọc từ env.
- KHÔNG hardcode secret (JWT/admin/connstring) → tất cả qua env (SPEC §8).
- KHÔNG hard delete. KHÔNG giả định ảnh riêng tư (ảnh public vĩnh viễn) → UI cảnh báo khi xóa.

## 6. Khi nghi ngờ
SPEC thiếu/mâu thuẫn, hoặc quyết định ảnh hưởng kiến trúc/bảo mật → **DỪNG và HỎI** kèm
2-3 phương án + đề xuất (CLAUDE.md §8). Không tự quyết âm thầm.
