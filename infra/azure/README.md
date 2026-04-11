# Azure Deployment

This repo is set up to deploy as two Azure Container Apps:

- `biostack-web`: Next.js frontend
- `biostack-api`: .NET API

## Why Container Apps

The project currently targets:

- Next.js `16`
- .NET `10`
- SQLite file storage for now

Container Apps is the lowest-friction Azure option because both workloads already have Dockerfiles and the frontend needs a build-time `NEXT_PUBLIC_API_URL`.

## Required secrets

You will need:

- `AUTH_SECRET`
- `Jwt__Secret`
- `Auth__CallbackSecret`

Optional, but required for OAuth sign-in to work:

- `GOOGLE_CLIENT_ID` / `GOOGLE_CLIENT_SECRET`
- `GITHUB_CLIENT_ID` / `GITHUB_CLIENT_SECRET`
- `DISCORD_CLIENT_ID` / `DISCORD_CLIENT_SECRET`
- `APPLE_CLIENT_ID` / `APPLE_CLIENT_SECRET`

## Deploy

Run:

```powershell
pwsh ./infra/azure/deploy-container-apps.ps1 `
  -ResourceGroup biostack-rg `
  -Location eastus `
  -BaseName biostackmissionctrl `
  -AuthSecret '<32+ char random secret>' `
  -JwtSecret '<32+ char random secret>' `
  -CallbackSecret '<32+ char random secret>'
```

Then update your OAuth provider callback URLs to point at:

```text
https://<frontend-fqdn>/api/auth/callback/google
https://<frontend-fqdn>/api/auth/callback/github
https://<frontend-fqdn>/api/auth/callback/discord
https://<frontend-fqdn>/api/auth/callback/apple
```

## Notes

- The API uses SQLite at `/app/data/biostack.db`.
- In the current Azure script, that storage is ephemeral inside the container. It is enough to get the site live, but not enough for durable production data.
- For persistent production data, move the API to PostgreSQL or add an Azure Files mount in a follow-up step.
- The backend CORS policy now reads from `Cors:AllowedOrigins`, so the deployed frontend origin must be present there.
