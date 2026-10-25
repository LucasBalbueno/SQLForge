using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SQLForge.Api.Models;

namespace SQLForge.Api.Errors;

internal sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    // Método chamado quando uma exceção não tratada acontece durante uma requisição
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception,
        CancellationToken cancellationToken)
    {
        var problem = CreateProblem(context, exception);
        context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(problem,
            options: null,
            contentType: "application/problem+json",
            cancellationToken: cancellationToken);
        return true;
    }

    // Analisa o tipo da exceção e cria um objeto ProblemDetails
    private ProblemDetails CreateProblem(HttpContext context, Exception exception)
    {
        if (exception is QueryValidationException validation)
            return ApiErrors.CreateProblem(context, StatusCodes.Status400BadRequest,
                "Consulta inválida.", validation.Message);

        if (exception is BadHttpRequestException)
            return ApiErrors.InvalidRequest(context);

        logger.LogError(exception, "Falha ao processar requisição. TraceId: {TraceId}", context.TraceIdentifier);
        return ApiErrors.CreateProblem(context, StatusCodes.Status500InternalServerError,
            "Erro interno do servidor.",
            "Não foi possível processar a requisição. Tente novamente mais tarde.");
    }
}
