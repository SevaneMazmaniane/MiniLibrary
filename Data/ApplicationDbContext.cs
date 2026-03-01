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
    }
}
