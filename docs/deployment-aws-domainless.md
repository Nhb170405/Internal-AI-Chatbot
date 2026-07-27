# Domainless AWS deployment guide

This guide deploys the project without buying a domain.

Final layout:

```text
Browser
  -> HTTPS https://dxxxxxxxxxxxxx.cloudfront.net
  -> CloudFront
  -> HTTP port 80 on EC2 (restricted to CloudFront)
  -> ASP.NET Core + embedded React
       -> Python over private Docker network
       -> private Amazon RDS SQL Server
       -> private Cloudflare R2
       -> OpenAI and Qdrant Cloud
```

Do not create an App Runner service. AWS stopped accepting new App Runner
customers on 2026-03-31.

## 1. Create and secure the AWS account

1. Open https://aws.amazon.com/free/ and create a Personal Free Plan account.
2. Complete payment-card and phone verification yourself.
3. Select Basic Support.
4. Sign in as the root user once and enable MFA.
5. Do not create a root access key.
6. Open Billing and Cost Management.
7. Enable Free Tier usage alerts.
8. Create monthly AWS Budgets alerts at USD 1, USD 10, and USD 25.
9. Check `Billing -> Credits` and record the balance and expiry date.
10. Use region `ap-southeast-1` (Singapore) for EC2 and RDS.

AWS account passwords, card data, MFA codes, OpenAI keys, R2 secrets, and
database passwords must never be pasted into source code or committed to Git.

## 2. Create the private R2 bucket

1. Create or sign in to a Cloudflare account.
2. Open `R2 Object Storage`.
3. Create bucket `internal-chatbot-files`.
4. Keep public access and the `r2.dev` URL disabled.
5. Open `Manage R2 API Tokens`.
6. Create a token with Object Read & Write access only to this bucket.
7. Save these values once:
   - Account ID
   - Access Key ID
   - Secret Access Key
   - S3 endpoint
8. The service URL has this shape:

```text
https://<ACCOUNT_ID>.r2.cloudflarestorage.com
```

No R2 CORS rule is required because the browser never uploads directly to R2.
ASP.NET Core uploads objects and produces short-lived read URLs.

## 3. Create AWS security groups

In `EC2 -> Security Groups`, create:

### `internal-chatbot-ec2-sg`

Outbound:

- Allow all IPv4 outbound traffic.

Inbound during initial setup:

- SSH TCP 22 from `My IP` only.
- HTTP TCP 80 from `My IP` only.

After CloudFront works, replace the HTTP rule with:

- HTTP TCP 80 from the AWS-managed prefix list
  `com.amazonaws.global.cloudfront.origin-facing`.

Never allow SSH from `0.0.0.0/0`.

### `internal-chatbot-rds-sg`

Inbound:

- MS SQL TCP 1433, source `internal-chatbot-ec2-sg`.

Do not use an IP range and never allow `0.0.0.0/0` to port 1433.

## 4. Create RDS for SQL Server Express

Open `RDS -> Create database`:

- Creation method: Standard create.
- Engine: Microsoft SQL Server.
- Edition: SQL Server Express Edition.
- Template: Free tier, when the console offers it.
- DB instance: `db.t3.micro`.
- Deployment: Single DB instance / Single-AZ.
- Storage: the smallest allowed General Purpose SSD allocation.
- Storage autoscaling: either disable it for the demo or set a conservative maximum.
- VPC: the same/default VPC that EC2 will use.
- Public access: No.
- Security group: `internal-chatbot-rds-sg`.
- Backup retention: 1 day for the initial low-cost deployment.
- Deletion protection: Off during the empty-data setup phase; enable it after
  production data is loaded.
- Enhanced monitoring and paid add-ons: Off.

Create a strong master password and store it in a password manager. Wait for
status `Available`, then copy the RDS endpoint.

The application runs EF Core migrations automatically on first start. The
master user must be allowed to create the `internal_ai_chatbot` database.

## 5. Create the EC2 instance

Open `EC2 -> Launch instance`:

- Name: `internal-chatbot-ec2`.
- AMI: Ubuntu Server 24.04 LTS x86_64.
- Instance type: `t3.small` initially (2 GiB RAM) to conserve credits. Upgrade
  to `t3.medium` if OCR or Python workloads exhaust memory.
- Key pair: create/download one; it cannot be downloaded again.
- VPC: same VPC as RDS.
- Subnet: a public subnet.
- Auto-assign public IP: enabled.
- Security group: `internal-chatbot-ec2-sg`.
- Root disk: 20 GiB gp3.
- Detailed monitoring: Off.

Allocate and associate an Elastic IP so the CloudFront origin stays stable
across instance restarts. AWS charges for public IPv4 addresses, including
ordinary auto-assigned public IPv4 addresses, so monitor this in Billing.

## 6. Install Docker on EC2

SSH to the instance using its Elastic IP. Follow Docker's current official
Ubuntu installation instructions. Verify:

```bash
docker version
docker compose version
```

Before building the images on a `t3.small`, add 4 GiB of persistent swap:

```bash
sudo fallocate -l 4G /swapfile
sudo chmod 600 /swapfile
sudo mkswap /swapfile
sudo swapon /swapfile
echo '/swapfile none swap sw 0 0' | sudo tee -a /etc/fstab
```

Add the Ubuntu user to the Docker group only if you understand the privileges
this grants, then sign out and back in:

```bash
sudo usermod -aG docker ubuntu
```

## 7. Put the repository on EC2

Clone the repository using a GitHub deploy key or another read-only method:

```bash
git clone <repository-url> internal-ai-chatbot
cd internal-ai-chatbot
```

For a private repository, do not put a GitHub personal access token directly in
the clone URL or shell history.

## 8. Create production secrets

Copy the template:

```bash
cp .env.production.example .env.production
chmod 600 .env.production
```

Generate the Python internal-service secret:

```bash
openssl rand -hex 32
```

Edit `.env.production` and supply:

- OpenAI key.
- Qdrant URL and API key.
- R2 endpoint, bucket, Access Key ID, and Secret Access Key.
- RDS endpoint, username, and password.
- The generated Python service secret.

The SQL connection string must remain on one line. If the database password
contains characters interpreted by Docker Compose, wrap the entire value in
double quotes and validate the rendered configuration with:

```bash
docker compose --env-file .env.production \
  -f docker-compose.production.yml config --quiet
```

Do not run `docker compose config` without `--quiet` while sharing the terminal,
because the expanded output contains secrets.

## 9. Build and start

From the repository root:

```bash
docker compose --env-file .env.production \
  -f docker-compose.production.yml build

docker compose --env-file .env.production \
  -f docker-compose.production.yml up -d

docker compose --env-file .env.production \
  -f docker-compose.production.yml ps
```

Inspect logs without printing environment variables:

```bash
docker compose --env-file .env.production \
  -f docker-compose.production.yml logs --tail=200 ai-service-python

docker compose --env-file .env.production \
  -f docker-compose.production.yml logs --tail=200 web
```

Test from the IP allowed by the temporary security-group rule:

```text
http://<ELASTIC_IP>/
```

Verify login, upload, processing, chat, Qdrant search, chart creation, chart
viewing, deletion, and container restart.

## 10. Create the CloudFront HTTPS URL

Open `CloudFront -> Create distribution`:

- Origin domain: EC2 public DNS name, not the raw IP.
- Origin protocol policy: HTTP only.
- HTTP port: 80.
- Viewer protocol policy: Redirect HTTP to HTTPS.
- Allowed HTTP methods: GET, HEAD, OPTIONS, PUT, POST, PATCH, DELETE.
- Cache policy: `CachingDisabled`.
- Origin request policy: `AllViewerAndCloudFrontHeaders-2022-06`.
- Compress objects automatically: On.
- Web Application Firewall: Off initially if it would add cost.
- Alternate domain name: leave empty.
- Custom certificate: leave empty.

The selected origin request policy supplies `CloudFront-Forwarded-Proto`.
ASP.NET Core uses that header so HTTPS redirects and secure authentication
cookies work correctly behind CloudFront.

Wait for deployment and open:

```text
https://dxxxxxxxxxxxxx.cloudfront.net
```

After it works:

1. Remove the EC2 HTTP-from-My-IP rule.
2. Allow port 80 only from
   `com.amazonaws.global.cloudfront.origin-facing`.
3. Keep SSH restricted to your current IP.
4. Use only the CloudFront URL in normal operation.

## 11. Validation checklist

- CloudFront URL loads React on HTTPS.
- Refreshing a React route does not return 404.
- Login cookie is Secure, HttpOnly, and SameSite=Lax.
- Browser requests use relative `/api` URLs.
- Python port 8000 is not present in the EC2 inbound rules.
- RDS is not publicly accessible.
- R2 bucket and `r2.dev` access are private.
- A document upload creates an object under `documents/<id>/`.
- Python can read the short-lived R2 URL.
- Chart creation creates `charts/chart_<id>.png`.
- Authenticated chart requests redirect to a short-lived R2 URL.
- Qdrant indexing/search works.
- `docker compose restart` does not lose documents or charts.
- Billing budget and Free Tier alerts are enabled.

## 12. Deploy updates

```bash
cd ~/internal-ai-chatbot
git pull --ff-only

docker compose --env-file .env.production \
  -f docker-compose.production.yml build

docker compose --env-file .env.production \
  -f docker-compose.production.yml up -d
```

Run the validation checklist after every deployment.

## 13. Remove Azure after cutover

Because this deployment has no data to migrate:

1. Confirm the AWS/CloudFront version passes the validation checklist.
2. Keep the Azure version available only during the first verification period.
3. Stop Azure Container Apps and Azure SQL.
4. Confirm no required file remains in Azure Blob Storage.
5. Delete the Azure resource group.
6. Delete Azure deployment secrets from GitHub.
7. Confirm Azure Cost Management shows no remaining billable resource.

The Azure GitHub Actions deployment workflow and Azure Blob runtime provider
have already been removed from this repository.
