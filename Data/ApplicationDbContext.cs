using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MiniLibrary.Models;

namespace MiniLibrary.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Book> Books => Set<Book>();
    public DbSet<BookLoan> BookLoans => Set<BookLoan>();
    public DbSet<EventItem> EventItems => Set<EventItem>();
    public DbSet<EventAttendance> EventAttendances => Set<EventAttendance>();
    public DbSet<EventInvitation> EventInvitations => Set<EventInvitation>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Book>()
            .Property(x => x.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Entity<Book>()
            .Property(x => x.Author)
            .HasMaxLength(200)
            .IsRequired();

        builder.Entity<BookLoan>()
            .HasOne(x => x.Book)
            .WithMany(x => x.Loans)
            .HasForeignKey(x => x.BookId);

        builder.Entity<EventItem>()
            .Property(x => x.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Entity<EventItem>()
            .Property(x => x.Location)
            .HasMaxLength(200)
            .IsRequired();

        builder.Entity<EventItem>()
            .HasOne(x => x.Organizer)
            .WithMany()
            .HasForeignKey(x => x.OrganizerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<EventAttendance>()
            .HasIndex(x => new { x.EventItemId, x.UserId })
            .IsUnique();

        builder.Entity<EventAttendance>()
            .HasOne(x => x.EventItem)
            .WithMany(x => x.Attendees)
            .HasForeignKey(x => x.EventItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<EventAttendance>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<EventInvitation>()
            .HasIndex(x => new { x.EventItemId, x.InviteeEmail })
            .IsUnique();

        builder.Entity<EventInvitation>()
            .Property(x => x.InviteeEmail)
            .HasMaxLength(256)
            .IsRequired();

        builder.Entity<EventInvitation>()
            .HasOne(x => x.EventItem)
            .WithMany(x => x.Invitations)
            .HasForeignKey(x => x.EventItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<EventInvitation>()
            .HasOne(x => x.Inviter)
            .WithMany()
            .HasForeignKey(x => x.InviterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<EventInvitation>()
            .HasOne(x => x.InviteeUser)
            .WithMany()
            .HasForeignKey(x => x.InviteeUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
