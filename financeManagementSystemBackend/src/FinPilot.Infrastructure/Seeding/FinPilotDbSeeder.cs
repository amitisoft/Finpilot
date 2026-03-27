using FinPilot.Domain.Entities;
using FinPilot.Domain.Enums;
using FinPilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinPilot.Infrastructure.Seeding;

public static class FinPilotDbSeeder
{
    public static async Task SeedAsync(FinPilotDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await dbContext.Database.MigrateAsync(cancellationToken);

        var hasDefaults = await dbContext.Categories.AnyAsync(x => x.UserId == null && x.IsDefault, cancellationToken);
        if (hasDefaults)
        {
            return;
        }

        var categories = new[]
        {
            new Category { Name = "Salary", Type = TransactionType.Income, IsDefault = true, Color = "#16a34a", Icon = "wallet" },
            new Category { Name = "Freelance", Type = TransactionType.Income, IsDefault = true, Color = "#22c55e", Icon = "briefcase" },
            new Category { Name = "Investment", Type = TransactionType.Income, IsDefault = true, Color = "#15803d", Icon = "chart-line" },
            new Category { Name = "Food", Type = TransactionType.Expense, IsDefault = true, Color = "#ef4444", Icon = "utensils" },
            new Category { Name = "Transport", Type = TransactionType.Expense, IsDefault = true, Color = "#f97316", Icon = "car" },
            new Category { Name = "Rent", Type = TransactionType.Expense, IsDefault = true, Color = "#dc2626", Icon = "house" },
            new Category { Name = "Utilities", Type = TransactionType.Expense, IsDefault = true, Color = "#eab308", Icon = "bolt" },
            new Category { Name = "Shopping", Type = TransactionType.Expense, IsDefault = true, Color = "#8b5cf6", Icon = "shopping-bag" },
            new Category { Name = "Health", Type = TransactionType.Expense, IsDefault = true, Color = "#06b6d4", Icon = "heart" },
            new Category { Name = "Entertainment", Type = TransactionType.Expense, IsDefault = true, Color = "#ec4899", Icon = "film" }
        };

        dbContext.Categories.AddRange(categories);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
