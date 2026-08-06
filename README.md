# API.Dialitech

Backend de monitoreo de salud multipaciente (.NET 10, Clean Architecture) con soporte para cuidador (web), app móvil y dispositivos wearable.

## Stack

- **.NET 10** / ASP.NET Core Web API (Clean Architecture / Onion)
- **MongoDB Atlas** (colecciones: Caregivers, Patients, Devices, Alerts, Readings)
- **JWT Bearer** (web) + códigos de vinculación de 6 dígitos (app móvil y wearable)
- **GitHub Actions** (CI + CodeQL + Secret Scan + CD) y **DigitalOcean** (App Platform + Container Registry)

## Estructura

| Proyecto | Rol |
|---|---|
| `API.Dialitech` | Entry point (Program.cs, controllers, middleware) |
| `API.Dialitech.Application` | Reglas de negocio, servicios, DTOs |
| `API.Dialitech.Domain` | Entidades, enums, interfaces |
| `API.Dialitech.Infrastructure` | MongoDB, repositorios, JWT, password hasher |
| `API.Dialitech.UnitTest` / `IntegrationTest` / `SecurityTest` | xUnit + Moq + WebApplicationFactory |

## Endpoints principales (`/api/v1`)

- **Auth**: `auth/register`, `auth/login`, `auth/me`, `auth/profile`, `auth/account`, `auth/change-password`, `auth/forgot-password`, `auth/reset-password`, `auth/plan`
- **Pacientes** (JWT): `patients` CRUD, `patients/{id}/generate-code`, `patients/{id}/generate-wearable-code`
- **Público (wearable/app)**: `patients/validate-code`, `devices/link`, `health-data/batch`, `health-data/patient-info/{code}`
- **Dashboard** (JWT): `dashboard`, `dashboard/{patientId}`, `dashboard/{patientId}/readings`
- **Alertas** (JWT): `alerts`, `alerts/{patientId}`, `alerts/{alertId}`

Documentación interactiva (dev): Scalar en `/scalar/v1`.

## Desarrollo local

```powershell
dotnet user-secrets set "MongoDbSettings:ConnectionString" "mongodb+srv://..." --project API.Dialitech
dotnet user-secrets set "JwtSettings:SecretKey" "<clave-mínimo-32-caracteres>" --project API.Dialitech
dotnet run --project API.Dialitech
```

Los `appsettings.*.json` con secretos están en `.gitignore` — nunca commitearlos (ver `SECURITY.md`).

## DevOps

- CI/CD, secretos y despliegue: [`docs/DEVOPS.md`](docs/DEVOPS.md)
- Política de seguridad: [`SECURITY.md`](SECURITY.md)
- Rulesets: `scripts/create-rulesets.ps1`
- Deploy DigitalOcean: `.do/app.yaml` (spec App Platform) — Render sigue disponible en `render.yaml` como target secundario.
