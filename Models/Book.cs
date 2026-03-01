using System.ComponentModel.DataAnnotations;

namespace MiniLibrary.Models;

public class Book
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Author { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Isbn { get; set; }

    [StringLength(100)]
    public string? Genre { get; set; }

    public int? Year { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    [Range(1, 1000)]
    public int TotalCopies { get; set; } = 1;

    [Range(0, 1000)]
    public int AvailableCopies { get; set; } = 1;

    public List<BookLoan> Loans { get; set; } = [];
}
