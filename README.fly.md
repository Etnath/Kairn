# Fly.io Deployment

Both apps run in the `cdg` (Paris) region. Staging auto-stops when idle (free-tier friendly); production keeps one machine alive.

## First-time setup

Install flyctl and log in:
```bash
brew install flyctl   # or: curl -L https://fly.io/install.sh | sh
fly auth login
```

### 1. Create the apps

```bash
fly apps create kairn-staging
fly apps create kairn-prod
```

### 2. Create managed Postgres clusters

```bash
# Staging — smallest free plan
fly postgres create --name kairn-staging-db --region cdg --initial-cluster-size 1 --vm-size shared-cpu-1x --volume-size 1

# Production — same size to start, scale later
fly postgres create --name kairn-prod-db --region cdg --initial-cluster-size 1 --vm-size shared-cpu-1x --volume-size 3
```

### 3. Attach Postgres to each app

Fly automatically injects `DATABASE_URL` into the app as a secret.

```bash
fly postgres attach kairn-staging-db --app kairn-staging
fly postgres attach kairn-prod-db    --app kairn-prod
```

The injected `DATABASE_URL` uses the format:
`postgres://user:password@kairn-staging-db.flycast:5432/kairn_staging`

Your app reads `ConnectionStrings__Default` — wire it up:

```bash
fly secrets set \
  "ConnectionStrings__Default=<value of DATABASE_URL>" \
  --app kairn-staging

fly secrets set \
  "ConnectionStrings__Default=<value of DATABASE_URL>" \
  --app kairn-prod
```

### 4. Set remaining secrets

```bash
# Repeat for both apps (staging / prod values will differ)
fly secrets set \
  ASPNETCORE_ENVIRONMENT=Production \
  Email__SmtpHost="smtp.example.com" \
  Email__SmtpUsername="..." \
  Email__SmtpPassword="..." \
  Email__FromAddress="noreply@yourdomain.com" \
  --app kairn-staging
```

### 5. GitHub Actions secrets

In **GitHub → Settings → Secrets and variables → Actions** add:

| Secret | Value |
|--------|-------|
| `FLY_STAGING_TOKEN` | `fly tokens create deploy -a kairn-staging` |
| `FLY_PROD_TOKEN` | `fly tokens create deploy -a kairn-prod` |
| `PROD_DATABASE_URL` | the Postgres connection string for prod (with `localhost:5432` as host since it goes through the proxy) |
| `B2_BUCKET` | your Backblaze B2 bucket name |
| `B2_ENDPOINT` | e.g. `https://s3.eu-central-003.backblazeb2.com` |
| `B2_KEY_ID` | B2 application key ID |
| `B2_APP_KEY` | B2 application key |

For the `PROD_DATABASE_URL` used in the backup job, replace the host with `localhost`:
```
postgres://kairn_prod:PASSWORD@localhost:5432/kairn_prod
```

### 6. Enable production approval gate (recommended)

GitHub → Settings → Environments → **production** → enable "Required reviewers" and add yourself. This means pushes to `main` pause for your manual approval before deploying.

## Deploy workflow

| Event | Result |
|-------|--------|
| Push to `staging` branch | Auto-deploys to `kairn-staging` |
| Push to `main` | Deploys to `kairn-prod` (waits for approval if gate is enabled) |

## Database backup strategy

**Layer 1 — Fly snapshots (automatic)**
Fly Postgres takes daily volume snapshots, retained for 7 days.
```bash
fly postgres backup list --app kairn-prod-db
fly postgres backup restore <backup-id> --app kairn-prod-db
```

**Layer 2 — Off-platform pg_dump (`.github/workflows/backup.yml`)**
Runs nightly at 04:00 Paris time. Dumps the production database and uploads a `.dump` file to Backblaze B2 (free 10 GB). Set a 30-day lifecycle rule on the B2 bucket so old backups are pruned automatically.

To restore from a B2 backup locally:
```bash
# 1. Download the dump
b2 download-file-by-name YOUR_BUCKET backups/kairn_prod_20260101_020000.dump restore.dump

# 2. Restore into a local or target database
pg_restore --clean --no-owner -d "postgres://..." restore.dump
```

## Useful commands

```bash
# View live logs
fly logs -a kairn-prod

# SSH into the running machine
fly ssh console -a kairn-prod

# Connect to the Postgres cluster
fly postgres connect -a kairn-prod-db

# Scale up if needed
fly scale memory 1024 -a kairn-prod
fly scale count 2 -a kairn-prod
```
