# PurchaseTx api

A minimal ASP.NET Core API for tracking purchases.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Docker or Podman (with `docker compose`/`podman compose`) for local Postgres

## Getting started

1. Create your local `.env` from the template (holds the local Postgres password, update the password):

   ```bash
   cp .env.example .env
   ```

2. Start local Postgres:

   ```bash
   docker compose up -d
   # or, if using Podman:
   podman compose up -d
   ```

3. Apply database migrations (first run only, or after pulling new migrations):

   ```bash
   cd api
   dotnet ef database update
   ```


4. Run the app:

   ```bash
   dotnet run --launch-profile http
   ```

   The API listens on `http://localhost:3000`. Sample requests are in `requests/api.http`.

### Running the app in a container

You can also build and run the API itself as a container, alongside the Postgres container started in step 2. The container listens on port `3000`.

```bash
# Build the image
docker build -t purchasetx-api .
# or, if using Podman:
podman build -t purchasetx-api .

# Run it on the same network as the Postgres container, in Development mode
docker run --rm -d --name purchasetx-api \
  --network purchasetx_default \
  --env-file .env \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e Postgres__Host=postgres \
  -p 3000:3000 \
  purchasetx-api
# or, if using Podman:
podman run --rm -d --name purchasetx-api \
  --network purchasetx_default \
  --env-file .env \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e Postgres__Host=postgres \
  -p 3000:3000 \
  purchasetx-api
```

`Postgres__Host=postgres` overrides `appsettings.Development.json`'s `localhost` default to use that container's service name instead. Outside of `Development`, the image expects `Postgres__*` environment variables and AWS IAM authentication as described below — no `.env` file or password needed.

In `Development` (the default for `dotnet run`), the app connects to the local Postgres container using the non-secret settings in `appsettings.Development.json` plus the password from your local `.env` file (loaded automatically via `DotNetEnv` in `Program.cs`). In other environments, it connects to AWS RDS Postgres using IAM authentication, configured entirely via `Postgres__*` environment variables (no static password) — see `api/Common/Extensions/PostgresExtensions.cs`.

### Configuring AWS IAM authentication for RDS

In any environment other than `Development`, the app authenticates to Postgres with a short-lived AWS IAM token instead of a static password. To set this up on a target RDS instance/cluster:

1. **Enable IAM database authentication** on the RDS instance/cluster (RDS console: *Modify* → enable *IAM database authentication*; or AWS CLI: `--enable-iam-database-authentication`).

2. **Grant the `rds_iam` role** to the Postgres user the app connects as, so RDS accepts IAM auth tokens for it:

   ```sql
   GRANT rds_iam TO api;
   ```

3. **Attach an IAM policy** allowing `rds-db:connect` to the identity the app runs as (e.g. an ECS task role, EC2 instance profile, or Lambda execution role), scoped to that DB user:

   ```json
   {
     "Version": "2012-10-17",
     "Statement": [
       {
         "Effect": "Allow",
         "Action": "rds-db:connect",
         "Resource": "arn:aws:rds-db:<region>:<account-id>:dbuser:<db-resource-id>/api"
       }
     ]
   }
   ```

   `<db-resource-id>` is the RDS instance/cluster's resource ID (RDS console → instance → *Configuration* tab, or `aws rds describe-db-instances`), not its ARN.

4. **Set these environment variables** on the app — no password is needed, since the app generates its own auth token from the credentials granted in step 3:

   ```
   Postgres__Host=<rds-endpoint>
   Postgres__Port=5432
   Postgres__Database=api
   Postgres__Username=api
   Postgres__Region=<region, e.g. us-east-1>
   ```

5. Make sure `ASPNETCORE_ENVIRONMENT` is **not** set to `Development` (leave it unset, or set it to `Production`) — the `Development` branch always expects `Postgres__Password` and never uses IAM auth.

The app refreshes its IAM auth token automatically before the token's 15-minute lifetime expires, and always connects over TLS (`SslMode.Require`), which RDS requires for IAM-authenticated connections.

### Wiping local data

To clear out test data without tearing down the container:

```bash
podman exec -it purchasetx-postgres psql -U api -d purchasetx -c 'TRUNCATE TABLE "Purchases";'
```

