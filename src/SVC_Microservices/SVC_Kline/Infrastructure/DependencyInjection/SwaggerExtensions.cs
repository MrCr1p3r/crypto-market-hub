namespace SVC_Kline.Infrastructure.DependencyInjection;

/// <summary>
/// Extension methods for configuring Swagger services.
/// </summary>
public static class SwaggerExtensions
{
    /// <summary>
    /// Configures the Swagger pipeline for the application.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The web application for chaining.</returns>
    public static WebApplication UseSwaggerPipeline(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        return app;
    }
}
