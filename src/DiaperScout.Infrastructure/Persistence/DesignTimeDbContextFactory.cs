using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DiaperScout.Infrastructure.Persistence;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DiaperScoutDbContext>
{
    public DiaperScoutDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__diaperscout")
            ?? "Host=localhost;Port=5432;Database=diaperscout;Username=postgres;Password=postgres";
        return new DiaperScoutDbContext(new DbContextOptionsBuilder<DiaperScoutDbContext>().UseNpgsql(connectionString).Options);
    }
}
