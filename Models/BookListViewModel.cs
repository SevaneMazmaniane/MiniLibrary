namespace MiniLibrary.Models;

public class BookListViewModel
{
    public string? Search { get; set; }
    public string? Genre { get; set; }
    public List<Book> Books { get; set; } = [];
}
