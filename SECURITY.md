# Security Policy

## Secretos

- **NUNCA** se commitean secretos, connection strings o claves al repositorio.
- `appsettings.Development.json` y `appsettings.Production.json` están en `.gitignore` y **no deben trackearse**.
- En desarrollo se usan **User Secrets** (`dotnet user-secrets set "MongoDbSettings:ConnectionString" ... --project API.Dialitech`).
- En CI/CD y producción, todos los valores sensibles viven en:
  - **GitHub Secrets** (para los workflows).
  - **DigitalOcean App Platform** como `type: SECRET` (cifrados, definidos en `.do/app.yaml` vía `doctl apps update --set`).
- Si un secreto se filtra (ej. se pushea un appsettings con credenciales): **rotar inmediatamente** el valor en el proveedor (Atlas, JWT, etc.), quitarlo del tracking y notificar al equipo.

## Escaneos obligatorios (no desactivar)

| Pipeline | Qué detecta |
|---|---|
| CodeQL (`codeql.yml`) | SAST en código C# |
| Gitleaks (`secret-scan.yml`) | Secretos commiteados |
| `dotnet list package --vulnerable` | Paquetes NuGet vulnerables |
| Trivy | Vulnerabilidades en filesystem/imagen Docker |
| Dependabot | PRs semanales de actualización (NuGet, GitHub Actions, Docker) |

## Reportar un problema de seguridad

No abras un issue público. Contacta a los mantenedores del repositorio (equipo dialitech630-dev) con detalles del problema. El equipo mantiene la regla: **nunca hacer deploy sin tests, nunca saltarse el CI/CD, nunca desactivar los escaneos**.
