# MiniLibrary

MiniLibrary is an ASP.NET Core MVC application for managing books and cultural events in one place.

It includes:
- A **library module** (catalog + borrow/return).
- An **events module** (create events, invite by email, RSVP, invitations inbox/sent list).
- **Authentication and roles** using ASP.NET Core Identity.
- **AI-assisted content** (Gemini) for book insights and event description drafting.

---

## Tech Stack

- .NET 8
- ASP.NET Core MVC + Razor Pages (Identity)
- Entity Framework Core
- SQLite
- Optional Google OAuth
- Optional Gemini API

---

## Features

## 1) Books

- Browse all books.
- Search by title, author, ISBN.
- Filter by genre.
- Borrow and return books (authenticated users).
- Admin can create, edit, and delete books.
- AI-generated book insights.

## 2) Events

- Browse and search book/art events.
- Filter by date/location/category.
- RSVP status tracking: `Upcoming`, `Attending`, `Maybe`, `Declined`.
- Admin can create/edit/delete events.
- Admin can invite users by email.
- Invitations page:
  - Admin sees invitations sent (who + event + status).
  - Non-admin sees invitations received and can respond.
- AI draft/enhance event descriptions.

## 3) Account Dashboard

- Personal account page with:
  - Borrowed books history.
  - Attended events history.

## 4) Authentication & Authorization

- Local Identity login/register.
- Role-based authorization (`Admin`, `Member`).
- Optional Google SSO if configured.

---

## Prerequisites

- .NET 8 SDK installed.

Check:

```bash
dotnet --version
```

---

## Configuration

Update `appsettings.json` (or use environment variables):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=minilibrary.db"
  },
  "Authentication": {
    "Google": {
      "ClientId": "",
      "ClientSecret": ""
    }
  },
  "Gemini": {
    "ApiKey": ""
  }
}
```

### Environment variable alternative for Gemini

```bash
export GEMINI_API_KEY="your_key_here"
```

---

## How to Run (Local)

1. Restore packages:

```bash
dotnet restore
```

2. Run the app:

```bash
dotnet run
```

3. Open one of the URLs shown in terminal (typically from `launchSettings.json`):
- `https://localhost:7043`
- `http://localhost:5043`

On startup, the app automatically applies migrations and seeds initial data.

---

## Seeded Data & Credentials

The app seeds roles, one admin user, sample books, and sample events.

### Roles seeded
- `Admin`
- `Member`

### Default admin credentials
- **Email:** `admin@minilibrary.local`
- **Password:** `Admin123!`

> Security note: change this password immediately in real environments.

### Member credentials
- There is **no default member password** seeded.
- Create a member account from the Register page.
- New users can register normally and use all non-admin features (admin-only actions still require the `Admin` role).

---

## Key User Flows

### Admin
- Login with admin account.
- Manage books (create/edit/delete).
- Manage events (create/edit/delete).
- Send invitations by email from Event Details.
- View sent invitations in `Events > Invitations`.

### Member / Reader
- Register and login.
- Borrow/return books.
- View personalized account dashboard.
- Open `Events > Invitations` to see received invites and respond.
- RSVP to events directly from Event Details.

---

## Troubleshooting

### Invitation not visible for user
- Ensure invite was sent to the same email as the user account.
- Confirm user is logged in with that account.
- Admin can verify sent invitations in `Events > Invitations`.

### AI features not working
- Verify `Gemini:ApiKey` (or `GEMINI_API_KEY`) is configured.
- If key is missing, non-AI core features still work.

### Google login not visible
- Configure `Authentication:Google:ClientId` and `ClientSecret`.

---

## Project Structure (high level)

- `Controllers/` → MVC controllers (`Books`, `Events`, `Account`, etc.)
- `Views/` → Razor MVC views
- `Areas/Identity/Pages/` → Identity UI pages (login/register)
- `Data/` → EF Core DbContext + seed
- `Models/` → Domain entities + view models
- `wwwroot/` → static assets (CSS)

---

## License

This project is for educational/demo purposes.
