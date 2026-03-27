using FinPilot.Domain.Entities;
using FinPilot.Domain.Enums;
using FinPilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

var email = args.FirstOrDefault() ?? "abhishek.shukla@amiti.in";
const string marker = "coach-setup-seed";
const string connectionString = "Host=localhost;Port=5432;Database=finpilot;Username=postgres;Password=postgres";
const string redisConnectionString = "localhost:6379";

var dbOptions = new DbContextOptionsBuilder<FinPilotDbContext>()
    .UseNpgsql(connectionString)
    .Options;

await using var dbContext = new FinPilotDbContext(dbOptions);
var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Email == email);
if (user is null)
{
    Console.Error.WriteLine($"User '{email}' was not found.");
    return 1;
}

var now = DateTimeOffset.UtcNow;
var currentMonth = now.Month;
var currentYear = now.Year;

var categories = await dbContext.Categories
    .Where(x => x.UserId == null || x.UserId == user.Id)
    .ToListAsync();

Category RequireCategory(string name, TransactionType type)
    => categories.FirstOrDefault(x => x.Type == type && string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase))
       ?? categories.FirstOrDefault(x => x.Type == type)
       ?? throw new InvalidOperationException($"Category '{name}' ({type}) not found.");

var salaryCategory = RequireCategory("Salary", TransactionType.Income);
var foodCategory = RequireCategory("Food", TransactionType.Expense);
var transportCategory = RequireCategory("Transport", TransactionType.Expense);
var entertainmentCategory = RequireCategory("Entertainment", TransactionType.Expense);

var account = await dbContext.Accounts.FirstOrDefaultAsync(x => x.UserId == user.Id && x.Name == "Primary Bank");
var accountCreated = false;
if (account is null)
{
    account = new Account
    {
        UserId = user.Id,
        Name = "Primary Bank",
        Type = AccountType.Bank,
        Currency = "INR",
        OpeningBalance = 20000m,
        CurrentBalance = 20000m,
        CreatedAt = now,
        UpdatedAt = now
    };
    dbContext.Accounts.Add(account);
    accountCreated = true;
}

await dbContext.SaveChangesAsync();

var monthStart = new DateTimeOffset(currentYear, currentMonth, 1, 0, 0, 0, TimeSpan.Zero);

async Task EnsureTransactionAsync(Guid categoryId, TransactionType type, decimal amount, string description, int day, string merchant)
{
    var existing = await dbContext.Transactions.AnyAsync(x =>
        x.UserId == user.Id &&
        x.AccountId == account.Id &&
        x.CategoryId == categoryId &&
        x.Type == type &&
        x.Amount == amount &&
        x.Description == description &&
        x.TransactionDate.Year == currentYear &&
        x.TransactionDate.Month == currentMonth &&
        x.Notes == marker);

    if (existing)
    {
        return;
    }

    dbContext.Transactions.Add(new Transaction
    {
        UserId = user.Id,
        AccountId = account.Id,
        CategoryId = categoryId,
        Type = type,
        Amount = amount,
        Description = description,
        TransactionDate = new DateTimeOffset(currentYear, currentMonth, Math.Min(day, DateTime.DaysInMonth(currentYear, currentMonth)), 10, 0, 0, TimeSpan.Zero),
        Merchant = merchant,
        Notes = marker,
        CreatedAt = now,
        UpdatedAt = now
    });
}

await EnsureTransactionAsync(salaryCategory.Id, TransactionType.Income, 50000m, "Monthly Salary", 2, "Employer");
await EnsureTransactionAsync(foodCategory.Id, TransactionType.Expense, 4200m, "Groceries", 5, "Local Market");
await EnsureTransactionAsync(transportCategory.Id, TransactionType.Expense, 1800m, "Metro and cab travel", 11, "Transit" );
await EnsureTransactionAsync(entertainmentCategory.Id, TransactionType.Expense, 2500m, "Weekend entertainment", 18, "Movies");

var goal = await dbContext.Goals.FirstOrDefaultAsync(x => x.UserId == user.Id && x.Status == GoalStatus.Active);
var goalCreated = false;
if (goal is null)
{
    goal = new Goal
    {
        UserId = user.Id,
        Name = "Emergency Fund",
        CurrentAmount = 30000m,
        TargetAmount = 120000m,
        TargetDate = new DateTimeOffset(currentYear, 12, 31, 0, 0, 0, TimeSpan.Zero),
        Status = GoalStatus.Active,
        CreatedAt = now,
        UpdatedAt = now
    };
    dbContext.Goals.Add(goal);
    goalCreated = true;
}

var budget = await dbContext.Budgets
    .Include(x => x.BudgetItems)
    .FirstOrDefaultAsync(x => x.UserId == user.Id && x.Month == currentMonth && x.Year == currentYear);
var budgetCreated = false;
if (budget is null)
{
    budget = new Budget
    {
        UserId = user.Id,
        Name = monthStart.ToString("MMMM") + " Budget",
        Month = currentMonth,
        Year = currentYear,
        TotalLimit = 15000m,
        AlertThresholdPercent = 80,
        CreatedAt = now,
        UpdatedAt = now
    };
    dbContext.Budgets.Add(budget);
    budgetCreated = true;
    await dbContext.SaveChangesAsync();
}
else
{
    budget.TotalLimit = Math.Max(budget.TotalLimit, 15000m);
    budget.AlertThresholdPercent = budget.AlertThresholdPercent <= 0 ? 80 : budget.AlertThresholdPercent;
    budget.UpdatedAt = now;
}

var existingBudgetItems = await dbContext.BudgetItems.Where(x => x.BudgetId == budget.Id).ToListAsync();

void EnsureBudgetItem(Guid categoryId, decimal limitAmount)
{
    var item = existingBudgetItems.FirstOrDefault(x => x.CategoryId == categoryId);
    if (item is null)
    {
        dbContext.BudgetItems.Add(new BudgetItem
        {
            BudgetId = budget.Id,
            CategoryId = categoryId,
            LimitAmount = limitAmount,
            SpentAmount = 0m,
            CreatedAt = now,
            UpdatedAt = now
        });
    }
    else
    {
        item.LimitAmount = Math.Max(item.LimitAmount, limitAmount);
        item.UpdatedAt = now;
    }
}

EnsureBudgetItem(foodCategory.Id, 6000m);
EnsureBudgetItem(transportCategory.Id, 3000m);
EnsureBudgetItem(entertainmentCategory.Id, 4000m);

await dbContext.SaveChangesAsync();

var userTransactions = await dbContext.Transactions
    .Where(x => x.UserId == user.Id)
    .ToListAsync();

var accountTransactions = userTransactions.Where(x => x.AccountId == account.Id).ToList();
var net = accountTransactions.Sum(x => x.Type == TransactionType.Income ? x.Amount : -x.Amount);
account.CurrentBalance = account.OpeningBalance + net;
account.UpdatedAt = now;

var budgetTransactions = userTransactions
    .Where(x => x.Type == TransactionType.Expense && x.TransactionDate.Year == currentYear && x.TransactionDate.Month == currentMonth)
    .ToList();

var budgetItems = await dbContext.BudgetItems.Where(x => x.BudgetId == budget.Id).ToListAsync();
foreach (var item in budgetItems)
{
    item.SpentAmount = budgetTransactions.Where(x => x.CategoryId == item.CategoryId).Sum(x => x.Amount);
    item.UpdatedAt = now;
}

await dbContext.SaveChangesAsync();

try
{
    await using var redis = await ConnectionMultiplexer.ConnectAsync(redisConnectionString);
    var database = redis.GetDatabase();
    var server = redis.GetServer(redis.GetEndPoints().First());
    foreach (var pattern in new[]
    {
        $"dashboard:*:{user.Id}",
        $"dashboard:*:{user.Id}:*",
        $"insights:*:{user.Id}",
        $"insights:*:{user.Id}:*"
    })
    {
        foreach (var key in server.Keys(pattern: pattern))
        {
            await database.KeyDeleteAsync(key);
        }
    }
}
catch (Exception exception)
{
    Console.WriteLine($"Warning: cache invalidation skipped: {exception.Message}");
}

Console.WriteLine($"Seeded coach setup data for {email}");
Console.WriteLine($"UserId: {user.Id}");
Console.WriteLine($"Account: {account.Name} (created: {accountCreated})");
Console.WriteLine($"Budget: {budget.Name} {currentMonth}/{currentYear} (created: {budgetCreated})");
Console.WriteLine($"Goal created: {goalCreated}");
Console.WriteLine("Transactions ensured: Monthly Salary, Groceries, Metro and cab travel, Weekend entertainment");
Console.WriteLine($"Current balance: {account.CurrentBalance}");
return 0;
