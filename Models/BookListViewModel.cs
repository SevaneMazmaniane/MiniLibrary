namespace MiniLibrary.Models;

public class BookListViewModel
{
    public string? Search { get; set; }
    public string? Genre { get; set; }
    public List<Book> Books { get; set; } = [];

    public AiBookDraftInput AiInput { get; set; } = new();
    public AiBookCandidate? AiCandidate { get; set; }
}

public class AiBookDraftInput
{
    public string? Title { get; set; }
    public string? Author { get; set; }
}

public class AiBookCandidate
{
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string? Genre { get; set; }
    public string? Isbn { get; set; }
}
