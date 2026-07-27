# Hướng dẫn triển khai AWS không cần domain

Ngày cập nhật: 27/07/2026.

Kiến trúc chốt:

```text
Người dùng
  -> HTTPS https://dxxxx.cloudfront.net
  -> Amazon CloudFront
  -> EC2 cổng 80
  -> container ASP.NET Core + React
       -> container Python qua mạng Docker nội bộ
       -> Amazon RDS SQL Server Express private
       -> Cloudflare R2 private
       -> OpenAI và Qdrant Cloud
```

Không tạo App Runner. AWS đã ngừng nhận khách hàng App Runner mới từ
31/03/2026, trong khi tài khoản AWS của bạn sẽ được tạo sau thời điểm đó.

## Bước 1 — Tạo tài khoản AWS

1. Truy cập https://aws.amazon.com/free/.
2. Chọn tạo tài khoản mới và chọn `Personal`.
3. Chọn `Free Plan`.
4. Tự nhập thẻ thanh toán và OTP; không gửi các thông tin này cho bất kỳ ai.
5. Chọn `Basic Support`, không chọn gói hỗ trợ trả phí.
6. Sau khi đăng nhập:
   - bật MFA cho root user;
   - không tạo access key cho root;
   - vào `Billing and Cost Management`;
   - bật cảnh báo Free Tier;
   - tạo Budget tại các mốc 1 USD, 10 USD và 25 USD;
   - kiểm tra `Billing -> Credits` và ghi lại ngày credit hết hạn.
7. Chọn region Singapore `ap-southeast-1` cho toàn bộ EC2 và RDS.

Không tham gia AWS Organization hoặc Control Tower trong giai đoạn Free Plan.

## Bước 2 — Tạo Cloudflare R2

1. Đăng nhập Cloudflare.
2. Mở `Storage & databases -> R2 Object Storage`.
3. Hoàn thành bước kích hoạt R2 nếu Cloudflare yêu cầu phương thức thanh toán.
4. Tạo bucket:

```text
internal-chatbot-files
```

5. Không bật public access và không bật URL `r2.dev`.
6. Mở `Manage R2 API Tokens`.
7. Tạo token:
   - quyền `Object Read & Write`;
   - chỉ áp dụng cho bucket `internal-chatbot-files`.
8. Lưu ngay bốn giá trị:
   - Account ID;
   - Access Key ID;
   - Secret Access Key;
   - S3 API endpoint.

Endpoint có dạng:

```text
https://<ACCOUNT_ID>.r2.cloudflarestorage.com
```

Secret Access Key chỉ hiển thị một lần. Không cần cấu hình R2 CORS vì trình
duyệt không truy cập trực tiếp bucket.

## Bước 3 — Tạo Security Group

Trong `EC2 -> Security Groups`, tạo hai group.

### `internal-chatbot-ec2-sg`

Inbound ban đầu:

| Type | Port | Source |
|---|---:|---|
| SSH | 22 | My IP |
| HTTP | 80 | My IP |

Outbound: cho phép IPv4 outbound mặc định.

Không mở SSH `0.0.0.0/0`.

### `internal-chatbot-rds-sg`

Inbound:

| Type | Port | Source |
|---|---:|---|
| MS SQL | 1433 | `internal-chatbot-ec2-sg` |

Không mở port 1433 ra Internet.

## Bước 4 — Tạo RDS SQL Server Express

Vào `RDS -> Create database`:

- Creation method: `Standard create`.
- Engine: `Microsoft SQL Server`.
- Edition: `SQL Server Express Edition`.
- Template: `Free tier` nếu giao diện cung cấp.
- Instance: `db.t3.micro`.
- Deployment: một instance, `Single-AZ`.
- Storage: mức General Purpose SSD nhỏ nhất được phép.
- Storage autoscaling: tắt hoặc đặt maximum thấp.
- VPC: default VPC.
- Public access: `No`.
- Security group: `internal-chatbot-rds-sg`.
- Backup retention: 1 ngày trong giai đoạn triển khai tiết kiệm ban đầu.
- Deletion protection: tắt khi chưa có dữ liệu; bật lại sau khi đưa dữ liệu
  production vào hệ thống.
- Enhanced Monitoring và add-on trả phí: tắt.

Tạo master username/password mạnh và lưu trong password manager. Đợi trạng thái
`Available`, sau đó copy endpoint RDS.

Ứng dụng sẽ tự chạy EF Core migrations và tạo database
`internal_ai_chatbot` khi khởi động lần đầu.

## Bước 5 — Tạo EC2

Vào `EC2 -> Launch instance`:

- Name: `internal-chatbot-ec2`.
- AMI: Ubuntu Server 24.04 LTS x86_64.
- Instance type: `t3.small` (2 GiB RAM) để tiết kiệm credit; nâng lên
  `t3.medium` nếu OCR/Python thiếu bộ nhớ.
- Key pair: tạo và tải file key; AWS không cho tải lại lần hai.
- VPC: cùng default VPC với RDS.
- Subnet: public subnet.
- Auto-assign public IP: bật.
- Security group: `internal-chatbot-ec2-sg`.
- Disk: 20 GiB gp3.
- Detailed monitoring: tắt.

Sau khi instance chạy:

1. Vào `Elastic IPs`.
2. Allocate một Elastic IP.
3. Associate Elastic IP với EC2.

Elastic IP giữ origin CloudFront ổn định khi EC2 restart. AWS tính phí public
IPv4, vì vậy phải theo dõi Budget.

## Bước 6 — Cài Docker trên EC2

SSH vào Elastic IP bằng key pair. Cài Docker theo tài liệu Ubuntu chính thức,
sau đó kiểm tra:

```bash
docker version
docker compose version
```

Trước khi build trên `t3.small`, tạo 4 GiB swap và cho phép tự mount sau reboot:

```bash
sudo fallocate -l 4G /swapfile
sudo chmod 600 /swapfile
sudo mkswap /swapfile
sudo swapon /swapfile
echo '/swapfile none swap sw 0 0' | sudo tee -a /etc/fstab
```

Nếu thêm user Ubuntu vào group Docker:

```bash
sudo usermod -aG docker ubuntu
```

hãy logout và đăng nhập lại. Thành viên group Docker có quyền gần tương đương
root trên máy.

## Bước 7 — Đưa source code lên EC2

Clone bằng deploy key/read-only credential:

```bash
git clone <repository-url> internal-ai-chatbot
cd internal-ai-chatbot
```

Không đặt GitHub token trực tiếp trong URL clone hoặc lịch sử terminal.

## Bước 8 — Tạo file secret production

Trong thư mục repository:

```bash
cp .env.production.example .env.production
chmod 600 .env.production
openssl rand -hex 32
```

Copy kết quả lệnh cuối vào `PYTHON_SERVICE_API_KEY`.

Mở `.env.production` và điền:

- OpenAI API key;
- Qdrant URL/API key;
- R2 endpoint, bucket, Access Key ID và Secret Access Key;
- RDS endpoint, username và password;
- Python internal API key vừa tạo.

Connection string mẫu:

```text
Server=<rds-endpoint>,1433;Database=internal_ai_chatbot;User Id=<user>;Password=<password>;Encrypt=True;TrustServerCertificate=True;Connection Timeout=30;
```

Kiểm tra cấu hình mà không in secrets:

```bash
docker compose --env-file .env.production \
  -f docker-compose.production.yml config --quiet
```

Không chia sẻ output của `docker compose config` khi không có `--quiet`, vì
output đầy đủ có thể chứa secrets đã được expand.

## Bước 9 — Build và chạy ứng dụng

```bash
docker compose --env-file .env.production \
  -f docker-compose.production.yml build

docker compose --env-file .env.production \
  -f docker-compose.production.yml up -d

docker compose --env-file .env.production \
  -f docker-compose.production.yml ps
```

Xem log:

```bash
docker compose --env-file .env.production \
  -f docker-compose.production.yml logs --tail=200 ai-service-python

docker compose --env-file .env.production \
  -f docker-compose.production.yml logs --tail=200 web
```

Từ IP đã được cho phép trong Security Group, mở:

```text
http://<ELASTIC_IP>/
```

Nếu web container fail ngay lần đầu, kiểm tra:

- endpoint/password RDS;
- `internal-chatbot-rds-sg` có nhận source từ EC2 SG;
- OpenAI key không rỗng;
- Python container có trạng thái healthy.

## Bước 10 — Tạo URL HTTPS CloudFront

Vào `CloudFront -> Create distribution`:

- Origin domain: public DNS của EC2, không nhập raw IP.
- Origin protocol policy: `HTTP only`.
- HTTP port: 80.
- Viewer protocol policy: `Redirect HTTP to HTTPS`.
- Allowed HTTP methods: cho phép đủ 7 methods.
- Cache policy: `CachingDisabled`.
- Origin request policy: `AllViewerAndCloudFrontHeaders-2022-06`.
- Compress objects automatically: bật.
- WAF: tắt ban đầu nếu phát sinh phí.
- Alternate domain name: để trống.
- Custom certificate: để trống.

Đợi distribution deploy xong rồi mở:

```text
https://dxxxxxxxxxxxxx.cloudfront.net
```

Đây là URL chính thức của ứng dụng, không cần domain và không cần mua SSL.

Sau khi CloudFront chạy:

1. Xóa inbound HTTP port 80 từ `My IP`.
2. Thêm inbound HTTP port 80 từ AWS-managed prefix list:

```text
com.amazonaws.global.cloudfront.origin-facing
```

3. Giữ SSH port 22 chỉ từ IP của bạn.
4. Không truy cập ứng dụng bằng Elastic IP nữa.

## Bước 11 — Checklist kiểm thử

- CloudFront URL tải được React qua HTTPS.
- Refresh trực tiếp `/login`, `/documents` không trả 404.
- Login/logout hoạt động.
- Cookie có `Secure`, `HttpOnly`, `SameSite=Lax`.
- Browser gọi `/api/...` cùng origin, không gọi hostname Azure.
- Python port 8000 không được mở trong EC2 Security Group.
- RDS có `Public access: No`.
- R2 bucket và `r2.dev` đều private.
- Upload tạo object dưới `documents/<documentId>/`.
- Python đọc được presigned URL.
- Tạo chart sinh object `charts/chart_<id>.png`.
- Chart endpoint yêu cầu đăng nhập và redirect sang URL R2 ngắn hạn.
- Qdrant indexing/search hoạt động.
- Restart Docker không làm mất document/chart.
- Billing Budget và Free Tier alerts đã bật.

## Bước 12 — Cập nhật phiên bản sau này

```bash
cd ~/internal-ai-chatbot
git pull --ff-only

docker compose --env-file .env.production \
  -f docker-compose.production.yml build

docker compose --env-file .env.production \
  -f docker-compose.production.yml up -d
```

Chạy lại checklist sau mỗi lần deploy.

## Bước 13 — Xóa Azure

Vì dự án chưa có dữ liệu cần migrate:

1. Chỉ xóa sau khi bản CloudFront vượt qua checklist.
2. Stop Azure Container Apps và Azure SQL trước.
3. Kiểm tra lần cuối Azure Blob không còn file cần giữ.
4. Xóa Azure resource group.
5. Xóa Azure deployment secrets trong GitHub.
6. Kiểm tra Azure Cost Management không còn resource tính phí.

Workflow Azure và Azure Blob provider đã được xóa khỏi source code.
