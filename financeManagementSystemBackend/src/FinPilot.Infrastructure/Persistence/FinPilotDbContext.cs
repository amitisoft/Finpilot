using FinPilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinPilot.Infrastructure.Persistence;

public sealed class FinPilotDbContext(DbContextOptions<FinPilotDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<BudgetItem> BudgetItems => Set<BudgetItem>();
    public DbSet<Goal> Goals => Set<Goal>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<AgentResult> AgentResults => Set<AgentResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.FullName).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(200).IsRequired();
            entity.Property(x => x.PasswordHash).IsRequired();
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.HasIndex(x => x.UserId);
            entity.Property(x => x.Token).IsRequired();
        });

        modelBuilder.Entity<Account>(entity =>
        {
            entity.ToTable("accounts");
            entity.HasIndex(x => x.UserId);
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Currency).HasMaxLength(10).IsRequired();
            entity.Property(x => x.OpeningBalance).HasPrecision(12, 2);
            entity.Property(x => x.CurrentBalance).HasPrecision(12, 2);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("categories");
            entity.HasIndex(x => x.UserId);
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.ToTable("transactions");
            entity.HasIndex(x => new { x.UserId, x.TransactionDate });
            entity.HasIndex(x => x.CategoryId);
            entity.HasIndex(x => x.AccountId);
            entity.Property(x => x.Amount).HasPrecision(12, 2);
            entity.Property(x => x.Description).HasMaxLength(250).IsRequired();
        });

        modelBuilder.Entity<Budget>(entity =>
        {
            entity.ToTable("budgets");
            entity.HasIndex(x => new { x.UserId, x.Month, x.Year }).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.TotalLimit).HasPrecision(12, 2);
        });

        modelBuilder.Entity<BudgetItem>(entity =>
        {
            entity.ToTable("budget_items");
            entity.Property(x => x.LimitAmount).HasPrecision(12, 2);
            entity.Property(x => x.SpentAmount).HasPrecision(12, 2);
        });

        modelBuilder.Entity<Goal>(entity =>
        {
            entity.ToTable("goals");
            entity.HasIndex(x => x.UserId);
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.TargetAmount).HasPrecision(12, 2);
            entity.Property(x => x.CurrentAmount).HasPrecision(12, 2);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("audit_logs");
            entity.HasIndex(x => x.UserId);
            entity.Property(x => x.EntityName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Action).HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<AgentResult>(entity =>
        {
            entity.ToTable("agent_results");
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => new { x.UserId, x.AgentType, x.SourceEntityName, x.SourceEntityId, x.GeneratedAt });
            entity.Property(x => x.SourceEntityName).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Summary).HasMaxLength(500).IsRequired();
            entity.Property(x => x.ResultJson).HasColumnType("text");
            entity.Property(x => x.ErrorMessage).HasColumnType("text");
        });
    }
}
