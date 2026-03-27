using FinPilot.Application.Interfaces;
using FinPilot.Application.Interfaces.Accounts;
using FinPilot.Application.Interfaces.Agents;
using FinPilot.Application.Interfaces.Audit;
using FinPilot.Application.Interfaces.Auth;
using FinPilot.Application.Interfaces.Budgets;
using FinPilot.Application.Interfaces.Categories;
using FinPilot.Application.Interfaces.Dashboard;
using FinPilot.Application.Interfaces.Goals;
using FinPilot.Application.Interfaces.Insights;
using FinPilot.Application.Interfaces.Transactions;
using FinPilot.Domain.Entities;
using FinPilot.Infrastructure.Agents;
using FinPilot.Infrastructure.Auth;
using FinPilot.Infrastructure.Finance;
using FinPilot.Infrastructure.Insights;
using FinPilot.Infrastructure.Persistence;
using FinPilot.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinPilot.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = ResolvePostgresConnectionString(configuration);
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services.AddDbContext<FinPilotDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(FinPilotDbContext).Assembly.FullName)));

        services.AddDistributedMemoryCache();

        services.AddScoped<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<IBudgetService, BudgetService>();
        services.AddScoped<IGoalService, GoalService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<InsightContextBuilder>();
        services.AddScoped<IInsightsService, InsightsService>();
        services.AddScoped<AnomalyAgentService>();
        services.AddScoped<BudgetAdvisorAgentService>();
        services.AddScoped<FinancialCoachAgentService>();
        services.AddScoped<InvestmentAdvisorAgentService>();
        services.AddScoped<ReportGeneratorAgentService>();
        services.AddScoped<IAgentExecutionService, AgentExecutionService>();
        services.AddScoped<IAgentOrchestratorService, AgentOrchestratorService>();
        services.AddScoped<IAgentResultService, AgentResultService>();
        services.AddScoped<PasswordHasher<User>>();

        return services;
    }

    private static string ResolvePostgresConnectionString(IConfiguration configuration)
    {
        var host = configuration["POSTGRES_HOST"];
        var port = configuration["POSTGRES_PORT"];
        var database = configuration["POSTGRES_DB"];
        var username = configuration["POSTGRES_USER"];
        var password = configuration["POSTGRES_PASSWORD"];

        if (!string.IsNullOrWhiteSpace(host))
        {
            return $"Host={host};Port={port ?? "5432"};Database={database ?? "finpilot"};Username={username ?? "postgres"};Password={password ?? "postgres"}";
        }

        return configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' was not found.");
    }
}
