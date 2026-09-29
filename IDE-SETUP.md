# IDE-SETUP

Recommended setup for working on this .NET 10 Authorization project.

## Preferred IDEs

1. **JetBrains Rider** (best experience for .NET)
2. **Visual Studio 2022 / 2026** (Windows)
3. **VS Code** + C# Dev Kit (cross-platform light option)

## Essential Extensions (VS Code)

- C# Dev Kit (Microsoft)
- .NET Install Tool
- Docker
- REST Client or Thunder Client (for .http files)
- GitHub Copilot / Continue / your preferred AI coding assistant

## Rider / Visual Studio Tips

- Enable “Run migrations on startup” awareness (already coded).
- Use the built-in HTTP client with `Authorization.API.http`.
- Set breakpoint in `TokenService` and `RbacController` to inspect claims.

## Recommended Settings

```json
// .vscode/settings.json example
{
  "dotnet.defaultSolution": "src/Authorization.slnx",
  "editor.formatOnSave": true,
  "omnisharp.enableEditorConfigSupport": true
}
```

## Debugging with Docker

- Use Docker Compose debugging support in Rider / VS.
- Or attach to the running `authorization-api` container.

## Database Inspection

- Any Postgres client (DBeaver, DataGrip, pgAdmin, Azure Data Studio).
- Connection: `localhost:5432`, db=`authorization_db`, user=`auth_user`, password=`auth_password`.

## Solution File

There is a minimal `src/Authorization.slnx`. You can open the `.csproj` directly if preferred.
