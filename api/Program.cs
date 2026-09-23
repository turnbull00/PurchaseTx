using api.Common.Extensions;
using api.Endpoints;
using api.Services.ExchangeRates;
using api.Validators;

using DotNetEnv;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

builder.AddAppTelemetry();
builder.AddAppPostgres();
builder.AddAppExchangeRates();

builder.Services.AddPurchaseValidators();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();

await app.Services.GetRequiredService<ExchangeRateCacheService>().RefreshAsync();

app.MapPurchaseEndpoints();

app.Run();
