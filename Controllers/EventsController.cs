using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniLibrary.Data;
using MiniLibrary.Models;
using MiniLibrary.Services;

namespace MiniLibrary.Controllers;

[Authorize]
public class EventsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IGeminiService _geminiService;

    public EventsController(ApplicationDbContext context, IGeminiService geminiService)
    {
        _context = context;
        _geminiService = geminiService;
    }

    [AllowAnonymous]
    public async Task<IActionResult> Index(EventSearchViewModel filter)
    {
        var query = _context.EventItems
            .Include(e => e.Organizer)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim().ToLower();
            query = query.Where(e => e.Title.ToLower().Contains(term) || (e.Description != null && e.Description.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(filter.Location))
        {
            var location = filter.Location.Trim().ToLower();
            query = query.Where(e => e.Location.ToLower().Contains(location));
        }

        if (filter.StartDate.HasValue)
        {
            query = query.Where(e => e.StartAtUtc >= filter.StartDate.Value);
        }

        if (filter.EndDate.HasValue)
        {
            var end = filter.EndDate.Value.AddDays(1);
            query = query.Where(e => e.StartAtUtc < end);
        }

        if (filter.Category.HasValue)
        {
            query = query.Where(e => e.Category == filter.Category.Value);
        }

        filter.Events = await query.OrderBy(e => e.StartAtUtc).ToListAsync();

        await PopulateRecommendationsForReaderAsync(filter);

        return View(filter);
    }

    [Authorize(Roles = "Admin")]
    public IActionResult Create()
    {
        var model = new EventFormViewModel();

        if (TempData["AiEventTitle"] is string title) model.Title = title;
        if (TempData["AiEventLocation"] is string location) model.Location = location;
        if (TempData["AiEventCategory"] is string category && Enum.TryParse<EventCategory>(category, out var parsedCategory)) model.Category = parsedCategory;
        if (TempData["AiEventStartAtUtc"] is string startRaw && DateTime.TryParse(startRaw, out var parsedStart)) model.StartAtUtc = parsedStart;
        if (TempData["AiEventEndAtUtc"] is string endRaw && DateTime.TryParse(endRaw, out var parsedEnd)) model.EndAtUtc = parsedEnd;
        if (TempData["AiEventDescription"] is string description)
        {
            TempData.Keep("AiEventDescription");
            model.Description = description;
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(EventFormViewModel model)
    {
        if (model.EndAtUtc.HasValue && model.EndAtUtc < model.StartAtUtc)
        {
            ModelState.AddModelError(nameof(model.EndAtUtc), "End time must be after start time.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var item = new EventItem
        {
            Title = model.Title.Trim(),
            StartAtUtc = model.StartAtUtc,
            EndAtUtc = model.EndAtUtc,
            Location = model.Location.Trim(),
            Description = model.Description,
            Category = model.Category,
            OrganizerId = userId,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.EventItems.Add(item);
        await _context.SaveChangesAsync();

        TempData["Message"] = "Event created.";
        return RedirectToAction(nameof(Details), new { id = item.Id });
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await _context.EventItems.FirstOrDefaultAsync(e => e.Id == id);
        if (item is null)
        {
            return NotFound();
        }


        var model = new EventFormViewModel
        {
            Id = item.Id,
            Title = item.Title,
            StartAtUtc = item.StartAtUtc,
            EndAtUtc = item.EndAtUtc,
            Location = item.Location,
            Description = item.Description,
            Category = item.Category
        };

        if (TempData["AiEventId"] is string idRaw && int.TryParse(idRaw, out var eventId) && eventId == id)
        {
            if (TempData["AiEventTitle"] is string title) model.Title = title;
            if (TempData["AiEventLocation"] is string location) model.Location = location;
            if (TempData["AiEventCategory"] is string category && Enum.TryParse<EventCategory>(category, out var parsedCategory)) model.Category = parsedCategory;
            if (TempData["AiEventStartAtUtc"] is string startRaw && DateTime.TryParse(startRaw, out var parsedStart)) model.StartAtUtc = parsedStart;
            if (TempData["AiEventEndAtUtc"] is string endRaw && DateTime.TryParse(endRaw, out var parsedEnd)) model.EndAtUtc = parsedEnd;
            if (TempData["AiEventDescription"] is string description) model.Description = description;
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id, EventFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        var item = await _context.EventItems.FirstOrDefaultAsync(e => e.Id == id);
        if (item is null)
        {
            return NotFound();
        }


        if (model.EndAtUtc.HasValue && model.EndAtUtc < model.StartAtUtc)
        {
            ModelState.AddModelError(nameof(model.EndAtUtc), "End time must be after start time.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        item.Title = model.Title.Trim();
        item.StartAtUtc = model.StartAtUtc;
        item.EndAtUtc = model.EndAtUtc;
        item.Location = model.Location.Trim();
        item.Description = model.Description;
        item.Category = model.Category;

        await _context.SaveChangesAsync();
        TempData["Message"] = "Event updated.";
        return RedirectToAction(nameof(Details), new { id = item.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.EventItems.FirstOrDefaultAsync(e => e.Id == id);
        if (item is null)
        {
            return NotFound();
        }


        _context.EventItems.Remove(item);
        await _context.SaveChangesAsync();

        TempData["Message"] = "Event deleted.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var item = await _context.EventItems
            .Include(e => e.Organizer)
            .Include(e => e.Attendees)
                .ThenInclude(a => a.User)
            .Include(e => e.Invitations)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (item is null)
        {
            return NotFound();
        }

        ViewBag.CurrentAttendance = userId is null
            ? (EventParticipationStatus?)null
            : item.Attendees.FirstOrDefault(a => a.UserId == userId)?.Status ?? EventParticipationStatus.Upcoming;

        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RSVP(int id, EventParticipationStatus status)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var eventItem = await _context.EventItems.FindAsync(id);
        if (eventItem is null)
        {
            return NotFound();
        }

        var attendance = await _context.EventAttendances
            .FirstOrDefaultAsync(a => a.EventItemId == id && a.UserId == userId);

        if (attendance is null)
        {
            attendance = new EventAttendance
            {
                EventItemId = id,
                UserId = userId,
                Status = status,
                UpdatedAtUtc = DateTime.UtcNow
            };
            _context.EventAttendances.Add(attendance);
        }
        else
        {
            attendance.Status = status;
            attendance.UpdatedAtUtc = DateTime.UtcNow;
        }

        var invitations = await _context.EventInvitations
            .Where(i => i.EventItemId == id && i.InviteeUserId == userId)
            .ToListAsync();

        foreach (var invitation in invitations)
        {
            invitation.Status = status;
            invitation.RespondedAtUtc = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        TempData["Message"] = "RSVP updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Invite(InvitationViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var eventItem = await _context.EventItems.FindAsync(model.EventId);
        if (eventItem is null)
        {
            return NotFound();
        }


        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Please provide a valid email address.";
            return RedirectToAction(nameof(Details), new { id = model.EventId });
        }

        var email = model.InviteeEmail.Trim().ToLower();
        var existing = await _context.EventInvitations
            .AnyAsync(i => i.EventItemId == model.EventId && i.InviteeEmail == email);

        if (existing)
        {
            TempData["Error"] = "An invitation already exists for this email.";
            return RedirectToAction(nameof(Details), new { id = model.EventId });
        }

        var invitee = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        _context.EventInvitations.Add(new EventInvitation
        {
            EventItemId = model.EventId,
            InviterId = userId,
            InviteeEmail = email,
            InviteeUserId = invitee?.Id,
            Status = EventParticipationStatus.Upcoming,
            SentAtUtc = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        TempData["Message"] = "Invitation sent.";
        return RedirectToAction(nameof(Details), new { id = model.EventId });
    }

    public async Task<IActionResult> Invitations()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var email = User.FindFirstValue(ClaimTypes.Email);
        var emailLower = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLower();
        var isAdmin = User.IsInRole("Admin");

        List<EventInvitationListItem> invitations;

        if (isAdmin)
        {
            invitations = await _context.EventInvitations
                .AsNoTracking()
                .Include(i => i.EventItem)
                .Include(i => i.InviteeUser)
                .Where(i => i.InviterId == userId)
                .OrderByDescending(i => i.SentAtUtc)
                .Select(i => new EventInvitationListItem
                {
                    InvitationId = i.Id,
                    EventTitle = i.EventItem!.Title,
                    StartAtUtc = i.EventItem.StartAtUtc,
                    Location = i.EventItem.Location,
                    Status = i.Status,
                    InviteeEmail = i.InviteeUser != null ? i.InviteeUser.Email : i.InviteeEmail,
                    SentAtUtc = i.SentAtUtc,
                    RespondedAtUtc = i.RespondedAtUtc,
                    CanRespond = false
                })
                .ToListAsync();
        }
        else
        {
            invitations = await _context.EventInvitations
                .AsNoTracking()
                .Include(i => i.EventItem)
                .Include(i => i.Inviter)
                .Where(i => i.InviteeUserId == userId || (emailLower != null && i.InviteeEmail.ToLower() == emailLower))
                .OrderBy(i => i.EventItem!.StartAtUtc)
                .Select(i => new EventInvitationListItem
                {
                    InvitationId = i.Id,
                    EventTitle = i.EventItem!.Title,
                    StartAtUtc = i.EventItem.StartAtUtc,
                    Location = i.EventItem.Location,
                    Status = i.Status,
                    InviterEmail = i.Inviter != null ? i.Inviter.Email : null,
                    SentAtUtc = i.SentAtUtc,
                    RespondedAtUtc = i.RespondedAtUtc,
                    CanRespond = true
                })
                .ToListAsync();
        }

        ViewBag.IsAdmin = isAdmin;
        return View(invitations);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RespondInvitation(int invitationId, EventParticipationStatus status)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var email = User.FindFirstValue(ClaimTypes.Email);
        var emailLower = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLower();

        var invitation = await _context.EventInvitations
            .Include(i => i.EventItem)
            .FirstOrDefaultAsync(i => i.Id == invitationId);

        if (invitation is null)
        {
            return NotFound();
        }

        var invitationEmailLower = invitation.InviteeEmail.Trim().ToLower();
        var matchesEmail = emailLower is not null && invitationEmailLower == emailLower;
        if (invitation.InviteeUserId != userId && !matchesEmail)
        {
            return Forbid();
        }

        invitation.InviteeUserId ??= userId;
        invitation.Status = status;
        invitation.RespondedAtUtc = DateTime.UtcNow;

        var attendance = await _context.EventAttendances
            .FirstOrDefaultAsync(a => a.EventItemId == invitation.EventItemId && a.UserId == userId);

        if (attendance is null)
        {
            _context.EventAttendances.Add(new EventAttendance
            {
                EventItemId = invitation.EventItemId,
                UserId = userId,
                Status = status,
                UpdatedAtUtc = DateTime.UtcNow
            });
        }
        else
        {
            attendance.Status = status;
            attendance.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        TempData["Message"] = "Invitation response saved.";
        return RedirectToAction(nameof(Invitations));
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AiEnhanceDescription(EventFormViewModel model, string returnAction)
    {
        if (string.IsNullOrWhiteSpace(model.Description))
        {
            TempData["Error"] = "Please enter a description before using AI enhancement.";
            return RedirectToForm(returnAction, model.Id);
        }

        var prompt = $"""
You are an assistant for a mini library cultural events app.
Rephrase the following event description so it sounds polished, inviting, and clear.
Keep it concise (max 140 words) and preserve original meaning.
Return only plain text.

Description:
{model.Description}
""";

        var aiText = await _geminiService.GenerateBookInsightsAsync(prompt);

        TempData["AiEventDescription"] = aiText;
        TempData["AiEventTitle"] = model.Title;
        TempData["AiEventLocation"] = model.Location;
        TempData["AiEventStartAtUtc"] = model.StartAtUtc.ToString("O");
        TempData["AiEventEndAtUtc"] = model.EndAtUtc?.ToString("O");
        TempData["AiEventCategory"] = model.Category.ToString();
        TempData["AiEventId"] = model.Id?.ToString();

        return RedirectToForm(returnAction, model.Id);
    }

    private IActionResult RedirectToForm(string returnAction, int? id)
    {
        if (string.Equals(returnAction, nameof(Edit), StringComparison.OrdinalIgnoreCase) && id.HasValue)
        {
            return RedirectToAction(nameof(Edit), new { id = id.Value });
        }

        return RedirectToAction(nameof(Create));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AiDraftDescription(EventFormViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Title) || string.IsNullOrWhiteSpace(model.Location))
        {
            TempData["Error"] = "Title and location are required for AI draft.";
            return RedirectToAction(nameof(Create));
        }

        var prompt = $"""
You are helping curate cultural events for a mini library app.
Write a concise event description (max 140 words) for a {(model.Category == EventCategory.Book ? "book" : "art")} event.
Title: {model.Title}
Location: {model.Location}
Start UTC: {model.StartAtUtc:O}
Return only plain text.
""";

        var aiText = await _geminiService.GenerateBookInsightsAsync(prompt);
        TempData["AiEventDescription"] = aiText;
        TempData["AiEventTitle"] = model.Title;
        TempData["AiEventLocation"] = model.Location;
        TempData["AiEventStartAtUtc"] = model.StartAtUtc.ToString("O");
        TempData["AiEventCategory"] = model.Category.ToString();
        TempData["AiEventEndAtUtc"] = model.EndAtUtc?.ToString("O");
        TempData["AiEventId"] = model.Id?.ToString();

        return RedirectToForm(nameof(Create), model.Id);
    }


    private sealed class EventRecommendationAiResult
    {
        public List<int>? RecommendedEventIds { get; set; }
        public string? Reason { get; set; }
    }

    private async Task PopulateRecommendationsForReaderAsync(EventSearchViewModel filter)
    {
        if (User.Identity?.IsAuthenticated != true || User.IsInRole("Admin"))
        {
            return;
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        var borrowedBookIds = await _context.BookLoans
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.BorrowedAtUtc)
            .Select(x => x.BookId)
            .Distinct()
            .Take(20)
            .ToListAsync();

        filter.HasBorrowedBooksForRecommendations = borrowedBookIds.Any();
        if (!filter.HasBorrowedBooksForRecommendations)
        {
            return;
        }

        var borrowedBooks = await _context.Books
            .Where(x => borrowedBookIds.Contains(x.Id))
            .ToListAsync();

        var allEvents = await _context.EventItems
            .OrderBy(x => x.StartAtUtc)
            .Take(30)
            .ToListAsync();

        if (!allEvents.Any())
        {
            return;
        }

        var booksForPrompt = string.Join("\n", borrowedBooks.Select((b, idx) =>
            $"{idx + 1}. Title: {b.Title}; Author: {b.Author}; Genre: {b.Genre ?? "Unknown"}; Description: {b.Description ?? "N/A"}"));

        var eventsForPrompt = string.Join("\n", allEvents.Select(e =>
            $"EventId: {e.Id}; Title: {e.Title}; Category: {e.Category}; Location: {e.Location}; Description: {e.Description ?? "N/A"}"));

        var prompt = $@"
You are recommending mini library events for a user based on books they have borrowed.
Given the borrowed books and available events, select up to 3 event IDs most likely to match the user's interests.
Return ONLY minified JSON with this exact schema:
{{""recommendedEventIds"":[1,2],""reason"":""short reason""}}
Do not include markdown or extra text.

Borrowed books:
{booksForPrompt}

Available events:
{eventsForPrompt}
";

        try
        {
            var aiText = await _geminiService.GenerateBookInsightsAsync(prompt);
            var json = ExtractJsonObject(aiText);
            var parsed = JsonSerializer.Deserialize<EventRecommendationAiResult>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var ids = parsed?.RecommendedEventIds?
                .Where(x => allEvents.Any(e => e.Id == x))
                .Distinct()
                .Take(3)
                .ToList() ?? [];

            filter.RecommendedEvents = allEvents.Where(e => ids.Contains(e.Id)).ToList();
            filter.RecommendationReason = parsed?.Reason;
        }
        catch
        {
            var genreText = string.Join(" ", borrowedBooks
                    .Select(b => $"{b.Genre} {b.Description} {b.Title}")
                    .Where(x => !string.IsNullOrWhiteSpace(x)))
                .ToLowerInvariant();

            var preferredCategory = genreText.Contains("art")
                ? EventCategory.Art
                : EventCategory.Book;

            filter.RecommendedEvents = allEvents
                .Where(e => e.Category == preferredCategory)
                .Take(3)
                .ToList();

            filter.RecommendationReason = filter.RecommendedEvents.Any()
                ? $"Based on your borrowed books, these {preferredCategory.ToString().ToLowerInvariant()} events may interest you."
                : null;
        }
    }

    private static string ExtractJsonObject(string raw)
    {
        var start = raw.IndexOf('{');
        var end = raw.LastIndexOf('}');

        if (start < 0 || end <= start)
        {
            throw new InvalidOperationException("No JSON object found in AI response.");
        }

        return raw[start..(end + 1)];
    }
}
