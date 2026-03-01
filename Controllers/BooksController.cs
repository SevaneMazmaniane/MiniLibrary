using System.Security.Claims;
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
            Books = await query.OrderBy(b => b.Title).ToListAsync()
        };

        ViewBag.Genres = await _context.Books
            .Where(x => x.Genre != null)
            .Select(x => x.Genre!)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();

        return View(model);
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
}
