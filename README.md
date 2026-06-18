# Image Vault

Kho ảnh có cây thư mục lồng nhau, giao diện kiểu Windows File Explorer.
Public duyệt/xem ảnh read-only; Admin dùng JWT để quản lý thư mục, ảnh và upload.
Binary ảnh lưu trên **freeimage.host**, metadata lưu trong **MongoDB**.

Tài liệu: [SPEC](IMAGE_VAULT_SPEC.md) · [PLAN](IMAGE_VAULT_PLAN.md) · [TASKS](IMAGE_VAULT_TASKS.md) · [Quy tắc agent](CLAUDE.md)

## Trạng thái

- Backend: .NET 8 Web API, MongoDB, JWT auth, ProblemDetails, rate limit upload, magic-byte validation, FreeImage client/stub.
- Frontend: Next.js 16, TypeScript, Tailwind, TanStack Query, Zustand, explorer UI public/admin.
- Deploy: Docker Compose gồm `api`, `web`, `mongo`; có healthcheck và volume Mongo.

MongoDB trong compose đang pin `mongo:4.4.29` để tương thích các VPS/Dokploy host không hỗ trợ AVX; MongoDB 5+ có thể crash với exit code `132` trên CPU cũ.

## Chạy local để phát triển

Cần .NET SDK 8+, Node.js 20+, npm và Docker nếu muốn chạy Mongo bằng compose.

```bash
# MongoDB local bằng Docker
docker compose up -d mongo

# API dev
dotnet run --project src/ImageVault.Api

# Web dev
cd web
npm ci
npm run dev
```

Mặc định dev:

- Web: `http://localhost:3000`
- API: `http://localhost:8080` hoặc port Kestrel hiện trong log khi chạy `dotnet run`
- Swagger chỉ bật ở `Development`
- Admin dev: `admin` / `dev-admin-123`

## Chạy full stack bằng Docker Compose

```bash
cp .env.example .env
# Sửa .env: JWT__SECRET, ADMIN__PASSWORD, FREEIMAGE__APIKEY nếu upload thật.
docker compose up --build
```

URL mặc định:

- Web: `http://localhost:3000`
- API health: `http://localhost:8080/api/health`

Các biến quan trọng trong `.env`:

| Biến | Ý nghĩa |
|---|---|
| `MONGO__CONNECTIONSTRING` | Connection string MongoDB từ API |
| `MONGO__DATABASE` | Tên database metadata |
| `FREEIMAGE__APIKEY` | API key freeimage.host; rỗng thì backend dùng stub |
| `FREEIMAGE__BASEURL` | Endpoint upload freeimage.host |
| `JWT__SECRET` | Secret HS256, tối thiểu 32 ký tự trong Production |
| `JWT__ISSUER` | JWT issuer |
| `JWT__EXPIRYHOURS` | Thời hạn token admin |
| `ADMIN__USERNAME` | Username admin seed lần đầu |
| `ADMIN__PASSWORD` | Password admin seed lần đầu |
| `CORS__ALLOWEDORIGINS` | Origin frontend được phép gọi API |
| `NEXT_PUBLIC_API_BASE_URL` | URL public của API, kèm `/api`; được bake khi build web |
| `API_PORT` | Port host map vào API container |
| `WEB_PORT` | Port host map vào web container |

Lưu ý: `NEXT_PUBLIC_API_BASE_URL` là biến public của Next.js và được nhúng vào bundle lúc build image web. Khi đổi domain API production, build lại image web với biến này.

## Deploy Dokploy / Traefik

1. Tạo project/app Docker Compose trong Dokploy và trỏ tới repo này.
2. Khai báo các biến trong `.env.example` dưới dạng secret/env của Dokploy.
3. Đặt domain cho web trỏ tới service `web`, port `3000`.
4. Đặt domain cho API trỏ tới service `api`, port `8080`.
5. Đặt `NEXT_PUBLIC_API_BASE_URL=https://<api-domain>/api` trước khi build web.
6. Đặt `CORS__ALLOWEDORIGINS=https://<web-domain>` để API chỉ cho frontend production gọi.
7. Bật HTTPS/TLS ở Traefik/Dokploy cho cả web và API.
8. Deploy, chờ healthcheck `mongo`, `api`, `web` healthy.
9. Smoke test:
   - Mở web domain public và duyệt thư mục.
   - Login admin bằng tài khoản seed.
   - Tạo thư mục, upload ảnh thử, đổi tên/di chuyển/xóa metadata.
   - Kiểm tra browser network không có request trực tiếp tới freeimage.host từ frontend và không lộ API key.

## Kiểm tra chất lượng

```bash
dotnet test

cd web
npm ci
npm run lint
npm run build
```

Docker checks:

```bash
docker compose config
docker compose up --build
```

## Ràng buộc quan trọng

- freeimage Guest API không có delete endpoint. Xóa ảnh/thư mục trong app chỉ soft-delete metadata trong MongoDB; binary trên freeimage.host vẫn tồn tại nếu ai giữ link.
- Upload luôn đi qua backend để giữ `FREEIMAGE__APIKEY` ở server.
- Production yêu cầu `JWT__SECRET` hợp lệ và `CORS__ALLOWEDORIGINS` rõ ràng; API không fallback `AllowAnyOrigin` ngoài Development.
