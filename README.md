# Mini Library Management System (ASP.NET Core + SQLite + Gemini)

This project is an ASP.NET Core MVC web app that implements a mini library system with:

- **Book management** (Admin): add, edit, delete books.
- **Check-in / Check-out** (authenticated users): borrow and return books.
- **Search & filtering**: by title, author, ISBN, and genre.
- **Authentication + SSO**:
  - Built-in ASP.NET Core Identity (local username/password).
  - Optional Google OAuth login (SSO) if credentials are configured.
- **Custom auth UI**: project now includes explicit Login and Register Razor Pages under `Areas/Identity/Pages/Account` for local auth.
- **Roles and permissions**:
  - `Admin`: full CRUD for books.
  - `Member`: can search, borrow, return, and use AI features.
- **AI feature (Gemini free API)**:
  - “AI Insights” button calls Gemini (`gemini-1.5-flash`) to generate concise summary, ideal audience, and discussion questions.

## Tech Stack

- ASP.NET Core 8 MVC + Identity
- Entity Framework Core + SQLite (free local database)
- Google Gemini API (`gemini-1.5-flash`)

## How to Run

## 1) Prerequisites

- .NET 8 SDK installed

## 2) Configure app settings

Edit `appsettings.json` (or use environment variables):

```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=minilibrary.db"
},
"Authentication": {
  "Google": {
    "ClientId": "<your_google_client_id>",
    "ClientSecret": "<your_google_client_secret>"
  }
},
"Gemini": {
  "ApiKey": "<your_gemini_api_key>"
}
```

You can also set Gemini key via env var:

```bash
export GEMINI_API_KEY="your_key_here"
```

## 3) Run

```bash
dotnet restore
dotnet run
```

App opens on the URLs shown in terminal (`launchSettings.json` default: `https://localhost:7043` / `http://localhost:5043`).

## Default Seeded Account

On first run, the app creates:

- Admin user: `admin@minilibrary.local`
- Password: `Admin123!`

It also seeds 2 sample books.

## Notes

- Database migrations are applied automatically at startup.
- If Google OAuth credentials are empty, local identity login still works.
- If Gemini API key is missing, AI insights returns a friendly configuration message.
