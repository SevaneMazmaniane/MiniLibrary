namespace MiniLibrary.Models;

public class AccountDashboardViewModel
{
    public string? DisplayName { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public List<AccountBorrowedBookItem> BorrowedBooks { get; set; } = [];
    public List<AccountAttendedEventItem> AttendedEvents { get; set; } = [];
}

public class AccountBorrowedBookItem
{
    public int BookId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public DateTime BorrowedAtUtc { get; set; }
    public DateTime? ReturnedAtUtc { get; set; }
}

public class AccountAttendedEventItem
{
    public int EventId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime StartAtUtc { get; set; }
    public EventCategory Category { get; set; }
}
