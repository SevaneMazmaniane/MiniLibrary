using System.ComponentModel.DataAnnotations;

namespace MiniLibrary.Models;

public enum EventCategory
{
    Book = 0,
    Art = 1
}

public enum EventParticipationStatus
{
    Upcoming = 0,
    Attending = 1,
    Maybe = 2,
    Declined = 3
}

public class EventItem
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public DateTime StartAtUtc { get; set; }

    public DateTime? EndAtUtc { get; set; }

    [Required]
    [MaxLength(200)]
    public string Location { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string? Description { get; set; }

    public EventCategory Category { get; set; } = EventCategory.Book;

    [Required]
    public string OrganizerId { get; set; } = string.Empty;

    public ApplicationUser? Organizer { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<EventAttendance> Attendees { get; set; } = new List<EventAttendance>();
    public ICollection<EventInvitation> Invitations { get; set; } = new List<EventInvitation>();
}

public class EventAttendance
{
    public int Id { get; set; }
    public int EventItemId { get; set; }
    public EventItem? EventItem { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }

    public EventParticipationStatus Status { get; set; } = EventParticipationStatus.Upcoming;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class EventInvitation
{
    public int Id { get; set; }

    public int EventItemId { get; set; }
    public EventItem? EventItem { get; set; }

    [Required]
    public string InviterId { get; set; } = string.Empty;

    public ApplicationUser? Inviter { get; set; }

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string InviteeEmail { get; set; } = string.Empty;

    public string? InviteeUserId { get; set; }
    public ApplicationUser? InviteeUser { get; set; }

    public EventParticipationStatus Status { get; set; } = EventParticipationStatus.Upcoming;

    public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAtUtc { get; set; }
}
