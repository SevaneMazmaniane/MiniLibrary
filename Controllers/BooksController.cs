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
public class BooksController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IGeminiService _geminiService;

    public BooksController(ApplicationDbContext context, IGeminiService geminiService)
    {
        _context = context;
        _geminiService = geminiService;
    }

    [AllowAnonymous]
    public async Task<IActionResult> Index(string? search, string? genre)
    {
        var query = _context.Books.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(b =>
                b.Title.ToLower().Contains(term) ||
                b.Author.ToLower().Contains(term) ||
                (b.Isbn != null && b.Isbn.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(genre))
        {
            query = query.Where(b => b.Genre == genre);
        }

        var model = new BookListViewModel
        {
            Search = search,
            Genre = genre,
            Books = await query.OrderBy(b => b.Title).ToListAsync(),
            AiInput = new AiBookDraftInput()
        };

        var aiCandidateJson = TempData["AiBookCandidate"] as string;
        if (!string.IsNullOrWhiteSpace(aiCandidateJson))
        {
            try
            {
                model.AiCandidate = JsonSerializer.Deserialize<AiBookCandidate>(aiCandidateJson);
            }
            catch
            {
                TempData["Error"] = "AI suggestion data was invalid. Please try Add with AI again.";
            }
        }

        model.AiInput.Title = TempData["AiDraftTitle"] as string;
        model.AiInput.Author = TempData["AiDraftAuthor"] as string;

        ViewBag.Genres = await _context.Books
            .Where(x => x.Genre != null)
            .Select(x => x.Genre!)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();

        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddWithAi(AiBookDraftInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Title))
        {
            TempData["Error"] = "Book title is required for AI suggestion.";
            return RedirectToAction(nameof(Index));
        }

        var prompt = $"""
                      Generate a single library book suggestion from this input.
                      Input title: {input.Title}
                      Input author (optional): {input.Author}

                      Return ONLY valid JSON in this exact shape with no markdown:
                      {
                        \"title\": \"string\",
                        \"author\": \"string\",
                        \"genre\": \"string\",
                        \"isbn\": \"string\"
                      }

                      Rules:
                      - If author is unknown, infer the most likely author or set to \"Unknown\".
                      - Genre should be concise.
                      - ISBN can be empty string if unavailable.
                      """;

        var aiText = await _geminiService.GenerateBookInsightsAsync(prompt);
        var candidate = ParseAiBookCandidate(aiText);
        if (candidate is null)
        {
            TempData["Error"] = "AI response could not be parsed. Please try again.";
            TempData["AiDraftTitle"] = input.Title;
            TempData["AiDraftAuthor"] = input.Author;
            return RedirectToAction(nameof(Index));
        }

        TempData["AiBookCandidate"] = JsonSerializer.Serialize(candidate);
        TempData["AiDraftTitle"] = input.Title;
        TempData["AiDraftAuthor"] = input.Author;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmAddWithAi(AiBookCandidate candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate.Title) || string.IsNullOrWhiteSpace(candidate.Author))
        {
            TempData["Error"] = "AI candidate must include title and author.";
            return RedirectToAction(nameof(Index));
        }

        var book = new Book
        {
            Title = candidate.Title.Trim(),
            Author = candidate.Author.Trim(),
            Genre = string.IsNullOrWhiteSpace(candidate.Genre) ? null : candidate.Genre.Trim(),
            Isbn = string.IsNullOrWhiteSpace(candidate.Isbn) ? null : candidate.Isbn.Trim(),
            TotalCopies = 1,
            AvailableCopies = 1
        };

        _context.Books.Add(book);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    public IActionResult Create() => View(new Book());

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Book book)
    {
        if (!ModelState.IsValid)
        {
            return View(book);
        }

        book.AvailableCopies = book.TotalCopies;
        _context.Books.Add(book);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var book = await _context.Books.FindAsync(id);
        return book is null ? NotFound() : View(book);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Book book)
    {
        if (id != book.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(book);
        }

        var existing = await _context.Books.FindAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        var activeLoans = await _context.BookLoans.CountAsync(x => x.BookId == id && x.ReturnedAtUtc == null);
        existing.Title = book.Title;
        existing.Author = book.Author;
        existing.Isbn = book.Isbn;
        existing.Genre = book.Genre;
        existing.Year = book.Year;
        existing.Description = book.Description;
        existing.TotalCopies = Math.Max(book.TotalCopies, activeLoans);
        existing.AvailableCopies = existing.TotalCopies - activeLoans;

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var book = await _context.Books.FindAsync(id);
        if (book is null)
        {
            return NotFound();
        }

        var hasActiveLoans = await _context.BookLoans.AnyAsync(x => x.BookId == id && x.ReturnedAtUtc == null);
        if (hasActiveLoans)
        {
            TempData["Error"] = "Cannot delete a book with active loans.";
            return RedirectToAction(nameof(Index));
        }

        _context.Books.Remove(book);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(int id)
    {
        var book = await _context.Books.FindAsync(id);
        if (book is null)
        {
            return NotFound();
        }

        if (book.AvailableCopies < 1)
        {
            TempData["Error"] = "No copies available.";
            return RedirectToAction(nameof(Index));
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var activeLoan = await _context.BookLoans
            .FirstOrDefaultAsync(x => x.BookId == id && x.UserId == userId && x.ReturnedAtUtc == null);
        if (activeLoan is not null)
        {
            TempData["Error"] = "You already borrowed this book.";
            return RedirectToAction(nameof(Index));
        }

        _context.BookLoans.Add(new BookLoan
        {
            BookId = id,
            UserId = userId,
            BorrowedAtUtc = DateTime.UtcNow
        });
        book.AvailableCopies -= 1;
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkin(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var loan = await _context.BookLoans
            .Include(x => x.Book)
            .FirstOrDefaultAsync(x => x.BookId == id && x.UserId == userId && x.ReturnedAtUtc == null);

        if (loan is null)
        {
            TempData["Error"] = "No active loan found for this user.";
            return RedirectToAction(nameof(Index));
        }

        loan.ReturnedAtUtc = DateTime.UtcNow;
        loan.Book.AvailableCopies += 1;
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AiInsights(int id)
    {
        var book = await _context.Books.FindAsync(id);
        if (book is null)
        {
            return NotFound();
        }

        var prompt = $"Provide concise reading insights for this library catalog item. Include: summary, ideal audience, and 3 discussion questions.\nTitle: {book.Title}\nAuthor: {book.Author}\nGenre: {book.Genre}\nDescription: {book.Description}";
        var insights = await _geminiService.GenerateBookInsightsAsync(prompt);

        TempData["AiInsights"] = insights;
        TempData["AiInsightsBook"] = book.Title;
        return RedirectToAction(nameof(Index));
    }

    private static AiBookCandidate? ParseAiBookCandidate(string aiText)
    {
        if (string.IsNullOrWhiteSpace(aiText))
        {
            return null;
        }

        var json = ExtractJsonObject(aiText);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var title = root.TryGetProperty("title", out var titleEl) ? titleEl.GetString() : null;
            var author = root.TryGetProperty("author", out var authorEl) ? authorEl.GetString() : null;
            var genre = root.TryGetProperty("genre", out var genreEl) ? genreEl.GetString() : null;
            var isbn = root.TryGetProperty("isbn", out var isbnEl) ? isbnEl.GetString() : null;

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(author))
            {
                return null;
            }

            return new AiBookCandidate
            {
                Title = title.Trim(),
                Author = author.Trim(),
                Genre = string.IsNullOrWhiteSpace(genre) ? null : genre.Trim(),
                Isbn = string.IsNullOrWhiteSpace(isbn) ? null : isbn.Trim()
            };
        }
        catch
        {
            return null;
        }
    }

    private static string? ExtractJsonObject(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            return null;
        }

        return text[start..(end + 1)];
    }
}
