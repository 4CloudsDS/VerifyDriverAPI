# VerifyDriverAPI

ASP.NET Core API for the Verify Driver marketplace. This service owns profile search, current-user workspace data, vehicles, relationships, verification cases, document-evidence metadata, moderation queues, and trust-signal aggregates.

## Current focus

- Public verification search supports broad profile, vehicle, platform, partner, role, and signal matching.
- Relationship candidate search excludes the signed-in user and applies intent/user-type fit rules.
- `/api/Me`, `/api/Vehicles/mine`, and `/api/Relationships/mine` currently provide the current-user workspace contracts used by `MvcDriverVerify`.
- Relationship and vehicle DTOs still need one backend polish pass so cards can render human-readable labels and vehicle edit forms can use explicit make/model/year fields without parsing summary text.

## Deferred backend design work

1. Endpoint convention cleanup: consolidate mixed current-user routes such as `/api/Profiles/me` and `/api/Vehicles/mine` under a consistent `/api/me` resource tree.
2. API-owned current-user access: keep deriving current-user identity from auth claims or development identity configuration; do not make the frontend pass arbitrary user IDs for private workspace reads or writes.
3. DTO enrichment: add driver, owner, vehicle, platform, and partner display labels to current-user relationship responses.
4. Marketplace data-model normalization: separate users/profiles, roles, vehicle ownership, platform eligibility, relationships, and verification claims from the legacy single-vehicle user model.

These items are tracked in `Requirements\VerifyDriverAPI\specs\backend_requirements.yaml` as `API-FR-057`, `API-TR-014`, and `API-DR-004`.

## Local validation

From the workspace root:

```powershell
dotnet build "projects\VerifyDriverAPI\VerifyDriversAPI.csproj" --nologo
dotnet test "projects\VerifyDriverAPI\Tests\VerifyDriverAPI.Tests.csproj" --nologo
```

Run locally:

```powershell
dotnet run --project "projects\VerifyDriverAPI\VerifyDriversAPI.csproj"
```

The development SQLite database is `Data\VerifyDriver.db`.

## Prompt-pipeline engine flow toward Azure

Run these from the workspace root and resume each workflow until it reaches `WORKFLOW COMPLETED`.

1. Backend polish:

```powershell
python Automation\engine\copilot_runner.py --task "backend refactor" --project VerifyDriverAPI
```

2. Git release candidate:

```powershell
python Automation\engine\copilot_runner.py --task "deploy VerifyDriverAPI to git" --project VerifyDriverAPI
```

3. Azure deployment preparation and execution:

```powershell
python Automation\engine\copilot_runner.py --task "deploy VerifyDriverAPI to azure" --project VerifyDriverAPI
```

The Azure workflow is expected to generate or validate `projects\VerifyDriverAPI\infra\main.bicep`, `projects\VerifyDriverAPI\.github\workflows\azure-deploy.yml`, `Deployment\VerifyDriverAPI\azure_deployment\deployment-plan.md`, `cost-estimate.md`, `approval-gate.md`, deployment logs, and post-deployment validation reports.
