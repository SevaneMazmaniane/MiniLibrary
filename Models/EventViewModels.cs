using System.ComponentModel.DataAnnotations;

namespace MiniLibrary.Models;

public class EventSearchViewModel
{
    public string? SearchTerm { get; set; }
    public string? Location { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public EventCategory? Category { get; set; }
    public List<EventItem> Events { get; set; } = [];

    public List<EventItem> RecommendedEvents { get; set; } = [];
    public string? RecommendationReason { get; set; }
    public bool HasBorrowedBooksForRecommendations { get; set; }
}

public class EventFormViewModel
{
    public int? Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.DateTime)]
    public DateTime StartAtUtc { get; set; } = DateTime.UtcNow.AddDays(1);

    [DataType(DataType.DateTime)]
    public DateTime? EndAtUtc { get; set; }

    [Required]
    [MaxLength(200)]
    public string Location { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string? Description { get; set; }

    public EventCategory Category { get; set; } = EventCategory.Book;
}

public class InvitationViewModel
{
    public int EventId { get; set; }

    [Required]
    [EmailAddress]
    public string InviteeEmail { get; set; } = string.Empty;
}

public class EventInvitationListItem
{
    public int InvitationId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public DateTime StartAtUtc { get; set; }
    public string Location { get; set; } = string.Empty;
    public EventParticipationStatus Status { get; set; }
}
