# DentalAPI

Backend API cho ứng dụng quản lý phòng khám nha khoa: bệnh nhân, nha sĩ, lịch hẹn, điều trị, hóa đơn.

> **Trạng thái:** đã có module `Patient` (CRUD), xác thực JWT cho nhân viên và bệnh nhân, phân quyền động theo permission.
> Các module `Dentist`, `Appointment`, `Service`, `Treatment`, `Invoice` chưa có.
> API **chưa chạy được ngoài môi trường Development** vì chưa tích hợp nhà cung cấp SMS/email thật (xem [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md)).

## Thông số kỹ thuật

| Hạng mục | Công nghệ / giá trị |
|---|---|
| Runtime | .NET 9, ASP.NET Core Web API (Controllers) |
| Cơ sở dữ liệu | SQL Server 2022, EF Core 9 (migration bằng `dotnet ef`, local tool) |
| Validate input | FluentValidation 11 |
| Xác thực | JWT Bearer (access token 15 phút), refresh token 7 ngày lưu dạng hash, xoay vòng mỗi lần refresh |
| Phân quyền | Permission động theo role + override theo tài khoản, cache 60 giây |
| Tài liệu API | OpenAPI (`/openapi/v1.json`, chỉ ở Development) |
| Lỗi trả về | `ProblemDetails` (RFC 9457) qua exception handler tập trung |
| Kiểm thử | xUnit, FluentAssertions 7.x, `WebApplicationFactory`, Testcontainers (SQL Server thật) |
| Đóng gói | Docker nhiều giai đoạn (SDK 9.0 → ASP.NET 9.0), chạy user `app` (không phải root), cổng `8080` |
| Health check | `GET /health` (kiểm tra cả kết nối DB) |
| Build | `Nullable`, `TreatWarningsAsErrors`, kiểm tra code style khi build (`Directory.Build.props`) |

### Quy ước dữ liệu

- **Thời gian:** lưu UTC (`DateTimeOffset`), hiển thị và tính giờ làm việc theo `Asia/Ho_Chi_Minh` (UTC+7).
- **Tiền tệ:** `decimal`, đơn vị VND.
- **Xóa:** soft delete (`IsDeleted`, `DeletedAt`) cho bệnh nhân, hồ sơ điều trị, hóa đơn.
- **Audit:** mọi bản ghi có `CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy`, do interceptor ghi tự động.
- **SĐT:** luôn chuẩn hóa về dạng `0…`.

### Bảo mật

- Mọi endpoint mặc định yêu cầu đăng nhập; endpoint công khai phải khai báo `[AllowAnonymous]`.
- Chống dò mật khẩu: chặn theo (tài khoản, IP) sau 5 lần sai trong 15 phút; khóa tài khoản sau 20 lần sai.
- Rate limit: endpoint auth công khai 10 request/phút/IP; `/me/email/*` 5 request/phút/tài khoản; vượt ngưỡng trả `429`.
- Không log SĐT, email, mật khẩu, OTP, token, tiền sử bệnh.

## Cấu trúc dự án (Clean Architecture)

```
src/
  DentalAPI.Api/             # Controllers, middleware, DI, Program.cs, appsettings
  DentalAPI.Application/     # Use case, DTO, validator, interface service
  DentalAPI.Domain/          # Entity, enum, value object, quy tắc nghiệp vụ
  DentalAPI.Infrastructure/  # DbContext, EF config, migration, repository, dịch vụ ngoài
tests/
  DentalAPI.UnitTests/
  DentalAPI.IntegrationTests/
```

Chiều phụ thuộc: `Api → Application → Domain`, `Infrastructure → Application/Domain`.

## Endpoint hiện có

Tất cả có tiền tố `/api/v1`. Ví dụ request đầy đủ trong [src/DentalAPI.Api/DentalAPI.Api.http](src/DentalAPI.Api/DentalAPI.Api.http).

| Nhóm | Endpoint | Quyền |
|---|---|---|
| Đăng nhập nhân viên | `POST /auth/staff/login`, `POST /auth/staff/change-password` | Công khai / nhân viên |
| Bệnh nhân tự đăng ký | `POST /auth/patients/register`, `verify-phone`, `resend-otp`, `login` | Công khai |
| Token | `POST /auth/refresh`, `POST /auth/logout` | Công khai / đã đăng nhập |
| Dữ liệu của bệnh nhân | `GET /me/profile`, `PUT /me/address`, `POST /me/email/send-code`, `POST /me/email/verify` | Bệnh nhân |
| Bệnh nhân | `GET/POST /patients`, `GET/PUT/DELETE /patients/{id}` | `patients.read` / `patients.write` / `patients.delete` |
| Tài khoản nhân viên | `/staff-accounts`, `/staff-accounts/{id}/permissions`, `/staff-accounts/{id}/permission-overrides` | Admin |
| Quyền theo role | `GET/PUT /staff-roles/{role}/permissions`, `GET /permissions` | Admin |

## Chạy trên máy dev

Yêu cầu: .NET SDK 9, Docker Desktop.

### Cách 1: toàn bộ bằng Docker

```bash
cp .env.example .env          # rồi điền mật khẩu, khóa JWT, tài khoản admin đầu tiên
docker compose up -d --build
curl.exe http://localhost:8080/health
```

API chạy ở `http://localhost:8080`, tự chạy migration và seed dữ liệu giả khi khởi động.
Mã OTP/email giả xem tại `GET /dev/outbox/{destination}`.

### Cách 2: API chạy ngoài Docker

```bash
dotnet tool restore
docker compose up -d db

dotnet user-secrets set "ConnectionStrings:Default" "Server=localhost,1433;Database=DentalDb;User Id=sa;Password=<SA_PASSWORD>;TrustServerCertificate=True" --project src/DentalAPI.Api
dotnet user-secrets set "Jwt:SigningKey" "<chuỗi ngẫu nhiên >= 32 byte>" --project src/DentalAPI.Api
dotnet user-secrets set "BootstrapAdmin:Email" "admin@example.com" --project src/DentalAPI.Api
dotnet user-secrets set "BootstrapAdmin:Password" "<mật khẩu >= 12 ký tự>" --project src/DentalAPI.Api

dotnet run --project src/DentalAPI.Api   # http://localhost:5228
```

### Kiểm thử

```bash
dotnet test        # integration test cần Docker đang chạy
dotnet format      # format code trước khi commit
```

## Cấu hình

| Khóa | Biến môi trường | Mô tả |
|---|---|---|
| `ConnectionStrings:Default` | `ConnectionStrings__Default` | Chuỗi kết nối SQL Server (bắt buộc) |
| `Jwt:SigningKey` | `Jwt__SigningKey` | Khóa ký token, tối thiểu 32 byte (bắt buộc) |
| `Jwt:Issuer`, `Jwt:Audience` | `Jwt__Issuer`, `Jwt__Audience` | Mặc định `DentalAPI` |
| `BootstrapAdmin:Email`, `BootstrapAdmin:Password` | `BootstrapAdmin__Email`, `BootstrapAdmin__Password` | Admin đầu tiên, tạo khi khởi động nếu chưa có Admin |
| `ForwardedHeaders:KnownProxies` | `ForwardedHeaders__KnownProxies__0`, ... | IP của reverse proxy được tin cậy |
| `RateLimiting:PublicAuth`, `RateLimiting:AccountVerification` | `RateLimiting__PublicAuth__PermitLimit`, ... | Ngưỡng rate limit |

Không commit secret: dùng `dotnet user-secrets` khi dev, biến môi trường hoặc secret store khi deploy.

## Tài liệu khác

- [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md): hướng dẫn deploy.
- [.claude/CLAUDE.md](.claude/CLAUDE.md): quy ước code, quy tắc nghiệp vụ và quy trình kiểm tra trước commit.
