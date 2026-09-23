namespace api.Models;

using DotNetEnv;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

using Npgsql;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        Env.TraversePath().Load();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionStringBuilder = new NpgsqlConnectionStringBuilder
        {
            Host = configuration["Postgres:Host"] ?? throw new InvalidOperationException("Postgres:Host is not configured."),
            Port = int.Parse(configuration["Postgres:Port"] ?? "5432"),
            Database = configuration["Postgres:Database"] ?? throw new InvalidOperationException("Postgres:Database is not configured."),
            Username = configuration["Postgres:Username"] ?? throw new InvalidOperationException("Postgres:Username is not configured."),
            Password = configuration["Postgres:Password"]
                ?? throw new InvalidOperationException("Postgres:Password is not configured (expected in .env)."),
            SslMode = SslMode.Disable,
        };

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(connectionStringBuilder.ConnectionString);

        return new AppDbContext(optionsBuilder.Options);
    }
}
