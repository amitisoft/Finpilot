using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FinPilot.Infrastructure.Persistence;

public sealed class FinPilotDbContextFactory : IDesignTimeDbContextFactory<FinPilotDbContext>
{
    public FinPilotDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("FINPILOT_POSTGRES_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=finpilot;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<FinPilotDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsql =>
            npgsql.MigrationsAssembly(typeof(FinPilotDbContext).Assembly.FullName));

        return new FinPilotDbContext(optionsBuilder.Options);
    }
}
