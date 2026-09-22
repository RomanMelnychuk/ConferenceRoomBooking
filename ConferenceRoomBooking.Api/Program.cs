using System.Threading.RateLimiting;
using ConferenceRoomBooking.Api.Data;
using ConferenceRoomBooking.Api.Middleware;
using ConferenceRoomBooking.Api.Security;
using ConferenceRoomBooking.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddControllers();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<ApiKeySecuritySchemeTransformer>();
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        // Retry automatically on short database failures: dropped connection, timeout, deadlock
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null)));

builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddSingleton<PriceCalculator>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IReportService, ReportService>();

// Reports whether the API is alive and can reach the database, for monitoring tools
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");

// Each client IP can make up to 100 requests per minute, so one client cannot overload the API
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Return the same ProblemDetails format as all other errors
    options.OnRejected = async (context, cancellationToken) =>
    {
        await context.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Too many requests",
            Detail = "Request limit exceeded. Please try again in a minute.",
            Instance = context.HttpContext.Request.Path
        }, cancellationToken);
    };

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));
});

var app = builder.Build();

// Create the database if needed, apply migrations and fill it with initial data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.Migrate();
    DbSeeder.Seed(context);
}

// HTTP pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Conference Room Booking API v1");
    });
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseRateLimiter();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");

app.Run();