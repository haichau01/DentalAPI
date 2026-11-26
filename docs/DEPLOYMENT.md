# Hướng dẫn deploy DentalAPI

## 0. Điều kiện trước khi deploy ra ngoài máy dev

Hiện tại **chưa deploy được lên Production**:

- `Program.cs` cố ý dừng app khi khởi động nếu môi trường không phải `Development`, vì chưa có nhà cung cấp SMS/email thật. Nếu bỏ qua, bệnh nhân đăng ký sẽ không bao giờ nhận được OTP.
- Môi trường `Development` có endpoint `/dev/outbox/{destination}` đọc được mã OTP, nên **không được** mở ra mạng.

Trước khi deploy thật cần:

1. Tích hợp nhà cung cấp SMS và email thật, thay `InMemoryMessageOutbox` ngoài Development.
2. Bỏ đoạn `throw new InvalidOperationException(...)` trong `Program.cs` sau khi bước 1 xong.
3. Kiểm tra `/dev/outbox` và `/openapi/v1.json` không tồn tại ở Production (`404`).

Các bước dưới đây mô tả quy trình deploy khi các điều kiện trên đã xong. Trước đó chỉ dùng phần 6 (chạy trên máy dev).

## 1. Kiến trúc triển khai

```
Client ──HTTPS──> Reverse proxy (Nginx / Caddy / IIS ARR)
                        │  HTTP, mạng nội bộ
                        ▼
                  Container api (cổng 8080, user app)
                        │
                        ▼
                  SQL Server 2022 (hoặc Azure SQL)
```

- App chỉ phục vụ HTTP; HTTPS kết thúc ở reverse proxy.
- Cổng `8080` của container chỉ mở cho reverse proxy, không mở ra Internet.
- SQL Server không mở ra Internet.

## 2. Yêu cầu máy chủ

- Docker Engine 24+ và Docker Compose v2 (hoặc nền tảng chạy container: Azure App Service, AWS ECS, Kubernetes...).
- SQL Server 2022 (tự quản lý hoặc dịch vụ cloud), có backup định kỳ.
- Chứng chỉ TLS cho domain (ví dụ Let's Encrypt qua Caddy/Certbot).
- Múi giờ máy chủ không quan trọng: app lưu UTC và tự quy đổi sang `Asia/Ho_Chi_Minh`.

## 3. Cấu hình bí mật

Đặt bằng biến môi trường hoặc secret store của nền tảng. **Không** ghi vào `appsettings.json` hay commit lên git.

| Biến môi trường | Bắt buộc | Ghi chú |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | Có | `Production` |
| `ConnectionStrings__Default` | Có | Dùng tài khoản SQL riêng cho app (không dùng `sa`), bật mã hóa: `Encrypt=True` và chứng chỉ hợp lệ thay cho `TrustServerCertificate=True` |
| `Jwt__SigningKey` | Có | Tối thiểu 32 byte ngẫu nhiên, khác nhau cho mỗi môi trường: `openssl rand -base64 48` |
| `Jwt__Issuer`, `Jwt__Audience` | Không | Mặc định `DentalAPI` |
| `BootstrapAdmin__Email`, `BootstrapAdmin__Password` | Lần đầu | Tạo Admin đầu tiên khi chưa có Admin. Mật khẩu >= 12 ký tự. Đổi mật khẩu ngay sau lần đăng nhập đầu |
| `ForwardedHeaders__KnownProxies__0` | Có nếu sau proxy | IP của reverse proxy, nhìn từ container. Thiếu thì mọi client chung một bucket rate limit và chặn đăng nhập |
| `RateLimiting__PublicAuth__PermitLimit` | Không | Mặc định 10/phút/IP |

Đổi `Jwt__SigningKey` sẽ làm mọi access token hiện có mất hiệu lực (người dùng phải đăng nhập lại).

## 4. Build image

```bash
docker build -t dentalapi:<version> .
# đẩy lên registry
docker tag dentalapi:<version> <registry>/dentalapi:<version>
docker push <registry>/dentalapi:<version>
```

Dùng tag theo phiên bản hoặc commit SHA, không deploy bằng `latest` để rollback được.

Trước khi build, chạy đủ bước kiểm tra trong `.claude/CLAUDE.md` (format, build Release, test).

## 5. Migration cơ sở dữ liệu

Ở Production app **không** tự chạy migration. Migration là một bước riêng, chạy **trước** khi khởi động phiên bản mới.

Cách khuyến nghị: tạo script SQL idempotent, xem lại rồi chạy bằng `sqlcmd` hoặc SSMS.

```bash
dotnet tool restore
dotnet ef migrations script --idempotent \
  -p src/DentalAPI.Infrastructure -s src/DentalAPI.Api \
  -o artifacts/migrate.sql
```

Hoặc tạo migration bundle (file chạy độc lập) để chạy trong pipeline:

```bash
dotnet ef migrations bundle --self-contained -r linux-x64 \
  -p src/DentalAPI.Infrastructure -s src/DentalAPI.Api \
  -o artifacts/efbundle
./artifacts/efbundle --connection "<connection string>"
```

Lưu ý:

- Backup DB trước khi chạy migration.
- Migration seed bộ quyền mặc định theo role (`DefaultStaffRolePermissions`); không cần seed tay.
- Dữ liệu giả (`DevelopmentDataSeeder`) chỉ chạy ở Development, không bao giờ có trên Production.

## 6. Chạy container

### Ví dụ với Docker Compose trên máy chủ

Tạo `docker-compose.prod.yml` trên máy chủ (không dùng file `docker-compose.yml` của repo, vì file đó đặt `ASPNETCORE_ENVIRONMENT=Development`):

```yaml
services:
  api:
    image: <registry>/dentalapi:<version>
    restart: unless-stopped
    env_file: /etc/dentalapi/api.env   # chmod 600, chỉ root đọc được
    environment:
      ASPNETCORE_ENVIRONMENT: Production
    ports:
      - "127.0.0.1:8080:8080"          # chỉ reverse proxy trên cùng máy truy cập
    healthcheck:
      test: ["CMD-SHELL", "bash -c 'exec 3<>/dev/tcp/localhost/8080 && printf \"GET /health HTTP/1.0\\r\\n\\r\\n\" >&3 && grep -q \"200 OK\" <&3'"]
      interval: 30s
      timeout: 5s
      retries: 3
```

```bash
docker compose -f docker-compose.prod.yml up -d
docker compose -f docker-compose.prod.yml ps
```

Image ASP.NET không có `curl`, nên healthcheck trên dùng `/dev/tcp` của bash. Có thể bỏ healthcheck trong container nếu nền tảng đã kiểm tra `GET /health` từ bên ngoài.

### Chạy thử trên máy dev (trước khi đủ điều kiện ở mục 0)

```bash
cp .env.example .env     # điền giá trị
docker compose up -d --build
```

Cổng chỉ mở trên `127.0.0.1`. Không đổi sang `0.0.0.0` khi đang ở Development.

## 7. Reverse proxy

Ví dụ Nginx:

```nginx
server {
    listen 443 ssl http2;
    server_name api.example.com;

    ssl_certificate     /etc/letsencrypt/live/api.example.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/api.example.com/privkey.pem;

    location / {
        proxy_pass         http://127.0.0.1:8080;
        proxy_set_header   Host              $host;
        proxy_set_header   X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
    }
}

server {
    listen 80;
    server_name api.example.com;
    return 301 https://$host$request_uri;
}
```

App chỉ tin `X-Forwarded-For` từ IP nằm trong `ForwardedHeaders:KnownProxies` (mặc định chỉ loopback). Nếu proxy ở máy hoặc container khác, khai báo IP của proxy đúng như container nhìn thấy, ví dụ `ForwardedHeaders__KnownProxies__0=172.18.0.1`.

## 8. Kiểm tra sau deploy

```bash
curl -f https://api.example.com/health                         # 200
curl -s -o /dev/null -w "%{http_code}\n" https://api.example.com/openapi/v1.json   # 404 ở Production
curl -s -o /dev/null -w "%{http_code}\n" https://api.example.com/dev/outbox/test   # 404 ở Production
curl -s -o /dev/null -w "%{http_code}\n" https://api.example.com/api/v1/patients   # 401 khi chưa đăng nhập
```

Sau đó:

1. Đăng nhập Admin bằng `POST /api/v1/auth/staff/login`, đổi mật khẩu ban đầu qua `/auth/staff/change-password`.
2. Xóa `BootstrapAdmin__Password` khỏi cấu hình (Admin đã tồn tại, giá trị không còn cần).
3. Xem log: `docker compose -f docker-compose.prod.yml logs api`. Không có exception lạ, không có SĐT, email, OTP, token trong log.

## 9. Cập nhật phiên bản và rollback

Cập nhật:

1. Backup DB.
2. Chạy migration của phiên bản mới (mục 5).
3. Đổi tag image trong `docker-compose.prod.yml`, `docker compose -f docker-compose.prod.yml up -d`.
4. Chạy lại mục 8.

Rollback:

- Nếu phiên bản mới **không** có migration: đổi lại tag image cũ và `up -d`.
- Nếu có migration: migration phải tương thích ngược (chỉ thêm cột/bảng) thì image cũ vẫn chạy được. Nếu không, khôi phục DB từ backup ở bước 1 rồi chạy image cũ.

## 10. Vận hành

- **Backup:** backup SQL Server hằng ngày, giữ bản sao ở nơi khác máy chủ; thử khôi phục định kỳ. Đây là dữ liệu bệnh nhân nhạy cảm, backup phải được mã hóa.
- **Log:** gom log stdout của container (journald, Loki, CloudWatch...). Không bật log level `Debug` hay log câu lệnh SQL kèm tham số ở Production.
- **Tác vụ nền:** `ExpiredRegistrationCleanupWorker` chạy mỗi giờ trong chính process API, xóa đăng ký chưa xác minh quá 24 giờ. Không cần cron riêng.
- **Mở rộng nhiều instance:** rate limit, chặn đăng nhập và cache quyền hiện lưu trong bộ nhớ từng instance. Khi chạy nhiều instance, mỗi instance đếm riêng và thay đổi quyền có thể trễ tới 60 giây ở instance khác. Nên chạy một instance cho tới khi chuyển các phần này sang store dùng chung (ví dụ Redis).
