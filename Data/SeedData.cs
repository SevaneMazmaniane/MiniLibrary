using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MiniLibrary.Models;

namespace MiniLibrary.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();

        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        string[] roles = ["Admin", "Member"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var adminEmail = "admin@minilibrary.local";
        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                DisplayName = "Default Admin"
            };
            await userManager.CreateAsync(admin, "Admin123!");
            await userManager.AddToRoleAsync(admin, "Admin");
        }

        if (!context.Books.Any())
        {
            context.Books.AddRange(
                new Book
                {
                    Title = "Clean Code",
                    Author = "Robert C. Martin",
                    Genre = "Software Engineering",
                    Isbn = "9780132350884",
                    Year = 2008,
                    Description = "Classic book on maintainable software craftsmanship.",
                    TotalCopies = 3,
                    AvailableCopies = 3
                },
                new Book
                {
                    Title = "The Pragmatic Programmer",
                    Author = "Andy Hunt, Dave Thomas",
                    Genre = "Software Engineering",
                    Isbn = "9780135957059",
                    Year = 2019,
                    Description = "A practical handbook for programmers.",
                    TotalCopies = 2,
                    AvailableCopies = 2
                });
            await context.SaveChangesAsync();
        }
    }
}
