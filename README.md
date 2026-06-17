# Image Vault

Kho ảnh có cây thư mục lồng nhau, giao diện kiểu Windows File Explorer.
Public duyệt/xem ảnh (read-only); Admin (JWT) quản lý thư mục & ảnh.
Binary ảnh lưu trên **freeimage.host**, metadata trên **MongoDB**.

> Tài liệu: [SPEC](IMAGE_VAULT_SPEC.md) · [PLAN](IMAGE_VAULT_PLAN.md) · [TASKS](IMAGE_VAULT_TASKS.md) · [Quy tắc agent](CLAUDE.md)

## Trạng thái

- ✅ **Phase 1 — Backend core**: Clean Architecture, MongoDB, JWT auth, materialized
  path (tạo/move/soft-delete đệ quy), endpoint public + login, freeimage **stub**.
- ⏳ Phase 2: endpoint admin + upload freeimage thật. Phase 3: frontend. Phase 4: deploy.

## Kiến trúc

```
src/
  ImageVault.Domain          # entities (Folder, ImageItem, User) — không phụ thuộc gì
  ImageVault.Application      # interfaces, DTOs, services (logic path)
  ImageVault.Infrastructure  # Mongo repos, JWT, BCrypt, FreeImage stub, seed, index
  ImageVault.Api             # controllers, DI, ProblemDetails, Program.cs
tests/
  ImageVault.UnitTests       # test materialized path
```

> Lưu ý: dự án **target .NET 8** (`net8.0`). Solution dùng định dạng `ImageVault.slnx`.

## Endpoint Phase 1

| Method | Path | Quyền |
|---|---|---|
| GET | `/api/health` | public |
| GET | `/api/folders/root` | public |
| GET | `/api/folders/{id}/children?page=&pageSize=&sort=name\|date&order=asc\|desc` | public |
| GET | `/api/folders/{id}/breadcrumb` | public |
| GET | `/api/images/{id}` | public |
| POST | `/api/auth/login` | public (trả JWT) |

Lỗi trả `ProblemDetails` (RFC 7807): 400/401/403/404/409/502.

## Chạy local (dev)

Cần .NET SDK 8+ và một MongoDB (hoặc chạy mongo bằng Docker).

```bash
# 1) Mongo bằng Docker (chỉ service mongo)
docker compose up -d mongo

# 2) API (dev — dùng appsettings.Development.json có secret DEV)
dotnet run --project src/ImageVault.Api
```

Mặc định dev: admin `admin` / `dev-admin-123` (đổi trong `appsettings.Development.json`).
Swagger UI: `http://localhost:5xxx/swagger` (chỉ bật ở môi trường Development).

```bash
# Test
dotnet test
```

## Chạy full bằng Docker

```bash
cp .env.example .env      # rồi điền JWT__SECRET, ADMIN__PASSWORD...
docker compose up --build
# Healthcheck:
curl http://localhost:8080/api/health
```

## Cấu hình (biến môi trường)

Xem [.env.example](.env.example). Tất cả secret đọc từ env (`Mongo__*`, `Jwt__*`,
`FreeImage__*`, `Admin__*`, `Cors__*`) — **không hardcode** (SPEC §8).

## Ràng buộc quan trọng (SPEC §1.1)

- freeimage Guest API **không xóa được binary** → mọi "xóa" là **soft delete** metadata
  (`isDeleted=true`). Ảnh trên freeimage là **public vĩnh viễn**.
- Upload **luôn qua backend**; API key freeimage chỉ ở server.
