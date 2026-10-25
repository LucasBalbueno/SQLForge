using Microsoft.AspNetCore.Mvc;

namespace SQLForge.Api.Errors;

public static class ApiErrors
{
    // Adiciona o middleware de tratamento de exceções nos controllers
    public static IMvcBuilder AddSafeApiErrors(this IMvcBuilder mvc)
    {
        // Adiciona ApiExceptionHandler como injeção de dependência
        mvc.Services.AddExceptionHandler<ApiExceptionHandler>();
        mvc.Services.AddProblemDetails();

        // Faz validações automáticas nos dados recebidos pelos controllers
        return mvc.ConfigureApiBehaviorOptions(options =>
        {
            options.InvalidModelStateResponseFactory = InvalidRequestResponse;
        });
    }

    // Chamado quando os dados enviados pelo cliente são inválidos
    private static IActionResult InvalidRequestResponse(ActionContext context)
    {
        var problem = InvalidRequest(context.HttpContext);
        var result = new BadRequestObjectResult(problem);
        result.ContentTypes.Add("application/problem+json");
        return result;
    }

    // Cria uma resposta padrão para erros de requisição
    internal static ProblemDetails InvalidRequest(HttpContext context) =>
        CreateProblem(context, StatusCodes.Status400BadRequest, "Requisição inválida.",
            "Verifique o formato e os valores dos campos enviados.");

    // Cria um objeto padronizado de erro para uma exceção
    internal static ProblemDetails CreateProblem(HttpContext context, int status, string title, string detail)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail
        };

        problem.Extensions["traceId"] = context.TraceIdentifier;
        return problem;
    }
}
