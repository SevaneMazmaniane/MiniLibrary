namespace MiniLibrary.Models;

public class BookLoan
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public Book Book { get; set; } = default!;
    public string UserId { get; set; } = string.Empty;
    public DateTime BorrowedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReturnedAtUtc { get; set; }

    public bool IsActive => ReturnedAtUtc is null;
}
