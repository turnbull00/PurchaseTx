namespace api.Common.Extensions;

using Amazon;
using Amazon.RDS.Util;

using api.Models;

using Microsoft.EntityFrameworkCore;

using Npgsql;

public static class PostgresExtensions
{
    public static WebApplicationBuilder AddAppPostgres(this WebApplicationBuilder builder)
    {
        var isDevelopment = builder.Environment.IsDevelopment();
        var configuration = builder.Configuration;

        var connectionStringBuilder = new NpgsqlConnectionStringBuilder
        {
            Host = configuration["Postgres:Host"] ?? throw new InvalidOperationException("Postgres:Host is not configured."),
            Port = int.Parse(configuration["Postgres:Port"] ?? "5432"),
            Database = configuration["Postgres:Database"] ?? throw new InvalidOperationException("Postgres:Database is not configured."),
            Username = configuration["Postgres:Username"] ?? throw new InvalidOperationException("Postgres:Username is not configured."),
        };

        if (isDevelopment)
        {
            connectionStringBuilder.Password = configuration["Postgres:Password"]
                ?? throw new InvalidOperationException("Postgres:Password is not configured.");
            connectionStringBuilder.SslMode = SslMode.Disable;

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionStringBuilder.ConnectionString));
        }
        else
        {
            connectionStringBuilder.SslMode = SslMode.Require;

            var region = RegionEndpoint.GetBySystemName(
                configuration["Postgres:Region"] ?? throw new InvalidOperationException("Postgres:Region is not configured."));

            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionStringBuilder.ConnectionString);
            dataSourceBuilder.UsePeriodicPasswordProvider(
                (csBuilder, cancellationToken) => new ValueTask<string>(
                    RDSAuthTokenGenerator.GenerateAuthTokenAsync(region, csBuilder.Host!, csBuilder.Port, csBuilder.Username!)),
                successRefreshInterval: TimeSpan.FromMinutes(13),
                failureRefreshInterval: TimeSpan.FromSeconds(5));

            var dataSource = dataSourceBuilder.Build();
            builder.Services.AddSingleton(dataSource);
            builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(dataSource));
        }

        builder.Services.AddScoped<IPurchaseRepository, PostgresPurchaseRepository>();

        return builder;
    }
}
