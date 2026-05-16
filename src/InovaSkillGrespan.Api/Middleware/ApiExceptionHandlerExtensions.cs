using System.Text.Json;
using InovaSkillGrespan.Domain.Exceptions;
using InovaSkillGrespan.Infrastructure.Exceptions;

namespace InovaSkillGrespan.Api.Middleware;

public static class ApiExceptionHandlerExtensions
{
    public static IApplicationBuilder UseApiExceptionHandler(
        this IApplicationBuilder app)
    {
        return app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var exception = context.Features
                    .Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()
                    ?.Error;

                var statusCode = exception switch
                {
                    DomainException domainException
                        when domainException.Message.Contains("excede o limite") =>
                        StatusCodes.Status413PayloadTooLarge,
                    DomainException => StatusCodes.Status400BadRequest,
                    JsonException => StatusCodes.Status400BadRequest,
                    FileNotFoundException => StatusCodes.Status500InternalServerError,
                    TimeoutException => StatusCodes.Status504GatewayTimeout,
                    ExternalImportEngineException => StatusCodes.Status502BadGateway,
                    _ => StatusCodes.Status500InternalServerError
                };

                context.Response.StatusCode = statusCode;
                await context.Response.WriteAsJsonAsync(new
                {
                    message = exception?.Message ?? "Erro inesperado ao processar requisicao."
                });
            });
        });
    }
}
