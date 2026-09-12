using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Web;

/// <summary>
/// Traduit les exceptions du domaine en <see cref="ProblemDetails"/> (RFC 7807).
/// </summary>
/// <remarks>
/// Sans cela, « il te manque du rhum » remonterait en 500. Ce handler ne couvre que
/// le strict nécessaire au fonctionnement de « Mon Bar » ; la reprise complète des
/// controllers (types de retour, 400/404/409 explicites) reste B5, cf. docs/MVP.md §7.
/// </remarks>
public class DomainExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        (int status, string title) = exception switch
        {
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Ressource introuvable"),
            ArgumentException => (StatusCodes.Status400BadRequest, "Requête invalide"),
            InvalidOperationException => (StatusCodes.Status409Conflict, "Opération impossible"),
            _ => (0, string.Empty)
        };

        if (status == 0)
            return false;

        httpContext.Response.StatusCode = status;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = exception.Message
            }
        });
    }
}
