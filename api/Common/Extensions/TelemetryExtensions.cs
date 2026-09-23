namespace api.Common.Extensions;

using OpenTelemetry.Logs;
using OpenTelemetry.Trace;

public static class TelemetryExtensions
{
    public static WebApplicationBuilder AddAppTelemetry(this WebApplicationBuilder builder)
    {
        var isDevelopment = builder.Environment.IsDevelopment();

        builder.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation();

                if (!isDevelopment)
                {
                    tracing.AddOtlpExporter();
                }
            });

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;

            if (!isDevelopment)
            {
                logging.AddOtlpExporter();
            }
        });

        return builder;
    }
}
