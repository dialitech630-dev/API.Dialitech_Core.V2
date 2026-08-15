# DevOps Guide — API.Dialitech

Infraestructura de CI/CD, seguridad y despliegue. Stack: **GitHub Actions**, **GitHub Rulesets**, **DigitalOcean App Platform** + **DigitalOcean Container Registry (DOCR)**.

---

## 1. Flujo completo

```
Developer → Push a feature/* → PR hacia develop → CI (build + tests + security) → Merge → develop
→ PR hacia main → CI de nuevo → Merge → main → CD (imagen Docker + deploy) → DigitalOcean → Health check /health
```

- `main` → **producción** (deploy automático vía `cd.yml`).
- `develop` → **integración** (CI obligatorio).
- `feature/*` → desarrollo.

Reglas críticas (ver `SECURITY.md`):
- NUNCA deploy sin tests.
- NUNCA subir secretos al repositorio.
- NUNCA saltarse el CI/CD.
- NUNCA desactivar escaneos de seguridad.
- SIEMPRE validar inputs en APIs (ya implementado en controllers/repositorios).
- SIEMPRE mantener compatibilidad de endpoints.

---

## 2. Workflows de GitHub Actions (`.github/workflows/`)

| Workflow | Trigger | Jobs |
|---|---|---|
| `ci.yml` | push a `main`/`develop` y PR hacia `main`/`develop` | `build`, `unit-tests`, `integration-tests`, `security-tests`, `vulnerability-scan` |
| `codeql.yml` | push/PR + `schedule` semanal (lunes 03:37) | `analyze` (CodeQL C#, security-and-quality) |
| `secret-scan.yml` | push/PR | `gitleaks` (escaneo de secretos en todo el historial) |
| `cd.yml` | push a `main` + `workflow_dispatch` | build imagen, push a DOCR, `doctl apps update` |

Los nombres de job (`build`, `unit-tests`, `integration-tests`, `security-tests`, `vulnerability-scan`, `analyze`, `gitleaks`) son los **status checks** que exigen los Rulesets.

---

## 3. Secretos y variables

### GitHub Secrets (Settings → Secrets and variables → Actions)

| Secret | Para qué | Notas |
|---|---|---|
| `MONGODB_CONNECTION_STRING` | Conectarse a MongoDB Atlas | Sustituye el placeholder `${MONGODB_CONNECTION_STRING}` |
| `JWT_SECRET_KEY` | Firmar/validar JWTs (HS256) | Mínimo 32 caracteres; sustituye `${JWT_SECRET_KEY}` |
| `DIGITALOCEAN_ACCESS_TOKEN` | `doctl` (registry + App Platform) | Token con scope `read/write` en Container Registry y App Platform |
| `DOCKER_REGISTRY_USERNAME` / `DOCKER_REGISTRY_PASSWORD` | (Opcional) si algún día se cambia a Docker Hub | No usados con DOCR |

### GitHub Variables (Settings → Secrets and variables → Actions)

| Variable | Valor |
|---|---|
| `DIGITALOCEAN_APP_ID` | ID de la app en DigitalOcean App Platform (para `doctl apps update`) |

Configurar con la CLI (requiere `gh` autenticado):

```powershell
gh secret set MONGODB_CONNECTION_STRING
gh secret set JWT_SECRET_KEY
gh secret set DIGITALOCEAN_ACCESS_TOKEN
gh variable set DIGITALOCEAN_APP_ID
```

### Consumo en ASP.NET Core

Los workflows inyectan las variables de entorno con la convención `Section__Key` de ASP.NET Core (doble guion bajo = `:`):

- `MongoDbSettings__ConnectionString` → `MongoDbSettings:ConnectionString`
- `JwtSettings__SecretKey` → `JwtSettings:SecretKey`

En `.do/app.yaml` los secretos se marcan `type: SECRET` y su valor se inyecta desde GitHub Secrets en el paso de deploy del CD:

```yaml
- key: MongoDbSettings__ConnectionString
  type: SECRET
  value: ${MONGODB_CONNECTION_STRING}
```

En desarrollo local se usan User Secrets:

```powershell
dotnet user-secrets set "MongoDbSettings:ConnectionString" "mongodb+srv://..." --project API.Dialitech
dotnet user-secrets set "JwtSettings:SecretKey" "..." --project API.Dialitech
```

---

## 4. DigitalOcean — setup inicial (una sola vez)

### 4.1 Crear el Container Registry

```powershell
doctl registry create dialitech --region sfo3
doctl auth init
```

La imagen resultante será `registry.digitalocean.com/dialitech/api-dialitech` (definida en `cd.yml`).

### 4.2 Crear la app en App Platform

Usando la spec del repositorio (`.do/app.yaml`). El tag `__IMAGE_TAG__` es un placeholder que el CD reemplaza por `sha-<commit>`:

```powershell
# 1. Sustituir el tag placeholder con un valor inicial
$env:MONGODB_CONNECTION_STRING = "mongodb+srv://..."
$env:JWT_SECRET_KEY = "..."
doctl apps create --spec .do/app.yaml --set MONGODB_CONNECTION_STRING="$env:MONGODB_CONNECTION_STRING" --set JWT_SECRET_KEY="$env:JWT_SECRET_KEY"
# 2. Copiar el ID de la app del output y guardarlo como variable de GitHub:
gh variable set DIGITALOCEAN_APP_ID
```

> Nota: si se prefiere crear la app desde el dashboard, los valores requeridos son: tipo **Container Registry**, imagen `api-dialitech`, puerto HTTP **8080**, health check path **`/health`**, y las variables de entorno de la tabla siguiente.

### 4.3 Variables de entorno de la app

| Variable | Valor |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `MongoDbSettings__DatabaseName` | `DialitechDB` |
| `MongoDbSettings__ConnectionString` | (secret) |
| `JwtSettings__SecretKey` | (secret) |
| `JwtSettings__Issuer` | `API.Dialitech` |
| `JwtSettings__Audience` | `API.Dialitech` |
| `JwtSettings__ExpirationInMinutes` | `60` |
| `Cors__Origins` | (lista separada por `;` si hay frontend) |
| `OpenApi__Enabled` | `true` |

### 4.4 Redeploy manual

```powershell
doctl apps update $env:DIGITALOCEAN_APP_ID --spec .do/app.yaml --wait
```

---

## 5. Rulesets / protección de ramas

Los Rulesets no se versionan en el repo; se crean con la API. El script `scripts/create-rulesets.ps1` los crea para `main` y `develop`:

```powershell
powershell -File scripts/create-rulesets.ps1          # real
powershell -File scripts/create-rulesets.ps1 -DryRun  # previsualizar JSON
```

Reglas aplicadas en ambas ramas:

- PR obligatorio (prohibido push directo).
- 1 approval requerida.
- Status checks requeridos: `build`, `unit-tests`, `integration-tests`, `security-tests`, `vulnerability-scan`, `analyze` (CodeQL), `gitleaks`.
- Requiere rama actualizada con la base (`strict`).
- Prohibido `force-push` y borrado de rama.
- Aplica también a administradores.

> Estado actual del repo: ya existe un ruleset **"Main Protection"** activo en la rama default (PR obligatorio, `deletion`, `non_fast_forward`). El script detecta rulesets existentes por su patrón de ramas y los **actualiza** (PUT) en vez de duplicarlos; si no existe, los crea (POST). Los checks requeridos se añaden al ejecutarlo.

Alternativa por UI: Settings → Rules → New ruleset → "Branch" → elegir rama → activar los mismos controles (Pull request, Required status checks, Deletion, Non-fast-forward).

> Si el script falla con "already exists", eliminar el ruleset previo por la UI o con `gh api --method DELETE repos/OWNER/REPO/rulesets/<id>`.

---

## 6. Docker

- `API.Dialitech/Dockerfile`: multi-stage (sdk 10.0 → aspnet 10.0), usuario no root (`USER $APP_UID`), puerto `8080`, runtime .NET 10.
- Health check del orquestador: `GET /health` (mapeado en `Program.cs`).
- El CD usa `docker/build-push-action` con cache GHA y tags `latest` + `sha-<commit>`.

---

## 7. Dependabot

`.github/dependabot.yml` ya configura actualizaciones semanales para:

- `nuget` (todos los csproj)
- `github-actions` (workflows)
- `docker` (Dockerfile)

Los PRs de dependabot pasan por el mismo CI que el resto; un cambio que rompa tests no se mergea.

---

## 8. Verificación rápida

```powershell
dotnet build API.Dialitech.slnx -c Release
dotnet test API.Dialitech.slnx -c Release --no-build
dotnet list API.Dialitech.slnx package --vulnerable --include-transitive
```

Después del primer push: revisar en GitHub las runs de CI, CodeQL y Secret Scan, y en DigitalOcean el deploy + `https://<app-url>/health`.

---

## 9. Endurecimiento de endpoints públicos (rate limiting)

Los endpoints públicos (sin JWT) están protegidos con rate limiting por IP (ventana fija). Políticas:

| Política | Endpoints | Límite por IP | Ajustable con (env) |
|---|---|---|---|
| `login` | `auth/login` | 5/min | — (fijo) |
| `batch` | `health-data/batch` | 30/min + cola 10 | — (fijo) |
| `sensitive` | `patients/validate-code`, `devices/link`, `health-data/patient-info/{code}`, `health-data/device-token` | 30/min + cola 2 | `RateLimiting__SensitivePermitLimit` |
| `register` | `auth/register` | 15/hora | `RateLimiting__RegisterPermitLimit` |
| `auth-restore` | `auth/forgot-password`, `auth/reset-password` | 5/min | `RateLimiting__AuthRestorePermitLimit` |

Los valores por defecto viven en la sección `RateLimiting` de `appsettings.json` / `appsettings.Production.json` y se sobreescriben con variables de entorno (convención `Section__Key`, ej. `RateLimiting__SensitivePermitLimit=60` en DigitalOcean App Platform).

> Si un cliente legítimo (app móvil/web) comienza a recibir `429 Too Many Requests`, subir el límite correspondiente **sin deploy**: las variables `RateLimiting__*` se leen en cada arranque.

### OpenAPI / Scalar en producción

Scalar está habilitado en producción (`OpenApi__Enabled=true`) para descubrimiento de endpoints. Opción de endurecimiento **opcional** (desactivada por defecto, no cambia el comportamiento actual):

- Configurar `OpenApi__AccessToken` (env). Si tiene valor, `/openapi/*` y `/scalar/*` exigen el token vía header `X-API-Access` o query `?x-api-access=`.
- Dejarla vacía = acceso abierto como hoy.

### Notas de operación del servicio ML

- El arranque registra un **warning** si `MlService:ApiKey` está vacía o usa el valor por defecto `test-key`, y otro si `MlService:BaseUrl` apunta a `localhost` en producción. El API **no** se detiene (el ML es fall-soft), pero esos warnings indican que el análisis ML no está conectado.

---

## 10. Notificaciones push Firebase (FCM)

### 9.1 Qué necesita el backend

El backend usa el **service account** de Firebase (`firebase-admin.json`, JSON de la cuenta de servicio del proyecto Firebase). **No** usa el `google-services.json` (ese archivo es solo para apps Android).

Obtener el service account: Firebase Console → ⚙️ Project settings → Service accounts → Generate new private key → se descarga `firebase-admin.json`.

### 9.2 Proporcionar las credenciales

El secreto se lee de la variable de entorno `FIREBASE_ADMIN_CREDENTIALS` (contenido JSON completo del service account) o, si no existe, del archivo `firebase-admin.json` en el directorio base de la app. Ambos archivos (`firebase-admin.json`, `google-services.json`) están en `.gitignore`.

- **Local:** copiar el JSON como `firebase-admin.json` junto al .csproj de `API.Dialitech` — el csproj lo copia automáticamente al output (`CopyToOutputDirectory="PreserveNewest"`) y la DI lo detecta. Alternativa: definir la env var `FIREBASE_ADMIN_CREDENTIALS` (ej. vía User Secrets).
- **Render:** `render.yaml` ya define `FIREBASE_ADMIN_CREDENTIALS` con `sync: false` (valor a pegar en el dashboard de Render).
- **DigitalOcean App Platform:** añadir env var `FIREBASE_ADMIN_CREDENTIALS` como secret en `.do/app.yaml` o desde el dashboard (ver tabla 4.3).

> **Fall-soft:** si las credenciales no están presentes, el API arranca normal y las notificaciones son no-op (los batches y alertas siguen funcionando). El envío de push **nunca** rompe el `200` de un batch: si Firebase falla, se registra un warning y el batch se procesa igual.

### 9.3 Flujo de las notificaciones

1. La app del paciente registra su token FCM: `POST /api/v1/health-data/device-token` con `{ "patientCode": "...", "deviceToken": "..." }` → `204 No Content`.
2. Por cada batch con alertas críticas (HR < 50, HR > 120, SpO₂ < 90) y con token registrado, se envía **una** notificación con la alerta más severa del lote.
3. `GET /api/v1/health-data/patient-info/{code}` devuelve `hasDeviceToken` para que la app sepa si el paciente ya está registrado.

### 9.4 Códigos de vinculación de un solo uso

- Un código generado (`generate-code` / `generate-wearable-code`) es **válido para una sola vinculación**: `POST /api/v1/devices/link` lo marca como usado (`CodeUsedAt` / `WearableCodeUsedAt` en el paciente) y ya no puede vincular otro dispositivo (`400 Code has already been used`).
- `POST /api/v1/patients/validate-code` devuelve `isValid: false` para códigos ya usados o expirados.
- **Idempotencia**: si el mismo serial que ya está vinculado reintenta con el código usado (retry por timeout), el link responde `200` sin crear duplicados.
- Generar un código nuevo resetea su marca de uso, permitiendo vincular otro dispositivo (ej. reemplazo de wearable). El código se conserva en el paciente tras el uso porque el wearable lo sigue usando como `patientCode` en el batch.

### 9.5 Inicialización automática

`DependencyInjection.AddInfrastructure` registra el servicio real (`FirebaseNotificationService`, paquete `FirebaseAdmin`) solo si las credenciales existen **y son válidas** (`TryInitialize` valida el JSON del service account); si no, registra `NoopNotificationService`. No requiere configuración adicional en `Program.cs`.
