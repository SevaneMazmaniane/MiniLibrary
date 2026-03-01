using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniLibrary.Data;
using MiniLibrary.Models;

namespace MiniLibrary.Controllers;

[Authorize]
public class AccountController : Controller
{
    private readonly ApplicationDbContext _context;

    public AccountController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
        {
            return NotFound();
        }

        var model = new AccountDashboardViewModel
        {
            DisplayName = user.DisplayName,
            Email = user.Email ?? string.Empty,
            IsAdmin = User.IsInRole("Admin")
        };

        model.BorrowedBooks = await _context.BookLoans
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Include(x => x.Book)
            .OrderByDescending(x => x.BorrowedAtUtc)
            .Select(x => new AccountBorrowedBookItem
            {
                BookId = x.BookId,
                Title = x.Book.Title,
                Author = x.Book.Author,
                BorrowedAtUtc = x.BorrowedAtUtc,
                ReturnedAtUtc = x.ReturnedAtUtc
            })
            .ToListAsync();

        model.AttendedEvents = await _context.EventAttendances
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Status == EventParticipationStatus.Attending)
            .Include(x => x.EventItem)
            .Where(x => x.EventItem != null && x.EventItem.StartAtUtc <= DateTime.UtcNow)
            .OrderByDescending(x => x.EventItem!.StartAtUtc)
            .Select(x => new AccountAttendedEventItem
            {
                EventId = x.EventItemId,
                Title = x.EventItem!.Title,
                Location = x.EventItem.Location,
                StartAtUtc = x.EventItem.StartAtUtc,
                Category = x.EventItem.Category
            })
            .ToListAsync();

        return View(model);
    }
}
