using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ConferenceRoomBooking.Api.Security;

/// <summary>
/// Allows the request only if the X-Api-Key header matches the admin key from configuration.
/// Protects operations that only the business owner may perform.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AdminApiKeyAttribute : Attribute, IAuthorizationFilter
{
    public const string HeaderName = "X-Api-Key";

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var expectedKey = configuration["Security:AdminApiKey"];

        // Fail closed: without a configured key nobody gets admin access
        if (string.IsNullOrEmpty(expectedKey))
            throw new InvalidOperationException("Admin API key is not configured.");

        var providedKey = context.HttpContext.Request.Headers[HeaderName].ToString();

        if (!KeysMatch(providedKey, expectedKey))
        {
            context.Result = new UnauthorizedObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Detail = $"A valid admin API key is required in the '{HeaderName}' header.",
                Instance = context.HttpContext.Request.Path
            });
        }
    }

    // Constant-time comparison, so the key cannot be guessed by measuring response time
    private static bool KeysMatch(string provided, string expected) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(provided),
            Encoding.UTF8.GetBytes(expected));
}