# Mini Library + Event Scheduler (ASP.NET Core + SQLite + Gemini)

This project is an ASP.NET Core MVC web app that combines:

- **Mini library management** for books and loans.
- **Book & art event scheduling** with invitations and RSVP tracking.

## Features

### Library
- **Book management** (Admin): add, edit, delete books.
- **Check-in / Check-out** (authenticated users): borrow and return books.
- **Search & filtering**: by title, author, ISBN, and genre.

### Event Scheduler (Book & Art Focus)
- **Event management**: create, edit, delete events with title, date/time, location, description, and category.
- **Status tracking**: RSVP as `Upcoming`, `Attending`, `Maybe`, or `Declined`.
- **Invitations**: invite users by email, view your invitations, and respond in one click.
- **Search**: filter by title/description, location, date range, and category.
- **AI feature**: generate an event description draft via Gemini.

### Auth, Roles, and AI
- **Authentication + SSO**:
  - Built-in ASP.NET Core Identity (local username/password).
  - Optional Google OAuth login (SSO) if credentials are configured.
- **Roles and permissions**:
  - `Admin`: full CRUD for books/events.
  - `Member`: can search, borrow/return, RSVP, and use AI features.
- **AI features (Gemini free API)**:
  - Book insights.
  - Event description drafting.

## Tech Stack

- ASP.NET Core 8 MVC + Identity
- Entity Framework Core + SQLite
- Google Gemini API (`gemini-1.5-flash`)

## How to Run

### 1) Prerequisites
- .NET 8 SDK installed

### 2) Configure app settings
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

### 3) Run

```bash
dotnet restore
dotnet run
```

App URLs are shown in terminal (`launchSettings.json` default: `https://localhost:7043` / `http://localhost:5043`).

## Default Seeded Account

On first run, the app creates:

- Admin user: `admin@minilibrary.local`
- Password: `Admin123!`

It also seeds two sample books.

## Deployment

You can deploy this app to any ASP.NET Core host (Azure App Service, Render, Railway, Fly.io, etc.) using a standard `dotnet publish` pipeline.

Example publish command:

```bash
dotnet publish -c Release -o ./publish
```

Then run the published output on your chosen provider with the same environment variables (`ConnectionStrings__DefaultConnection`, `Gemini__ApiKey`, optional Google OAuth keys).
