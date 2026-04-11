using System.Diagnostics;
using Evolver.Shared.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Evolver.Web.Filters;

/// <summary>
/// 将控制器返回的原始对象/错误体包装为 <see cref="ApiEnvelope"/>，便于前端统一解析。
/// 已是 <see cref="IApiEnvelope"/> 的返回值不再嵌套。
/// </summary>
public sealed class UnifiedApiResponseFilter : IAsyncAlwaysRunResultFilter
{
    public Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        var traceId = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;

        if (context.Result is ObjectResult obj)
            UnifyObjectResult(context, obj, traceId);

        return next();
    }

    private static void UnifyObjectResult(ResultExecutingContext context, ObjectResult obj, string traceId)
    {
        if (obj.Value is IApiEnvelope)
            return;

        var status = InferStatusCode(context, obj);

        if (status == StatusCodes.Status204NoContent)
            return;

        if (status is >= StatusCodes.Status200OK and < StatusCodes.Status300MultipleChoices)
        {
            obj.Value = ApiEnvelope.Ok(obj.Value, traceId);
            obj.DeclaredType = typeof(ApiEnvelope);
            return;
        }

        switch (status)
        {
            case StatusCodes.Status400BadRequest:
                obj.Value = WrapBadRequest(obj.Value, traceId);
                obj.DeclaredType = typeof(ApiEnvelope);
                break;

            case StatusCodes.Status401Unauthorized:
                obj.Value = ApiEnvelope.Fail("unauthorized", "未认证或令牌无效。", traceId: traceId);
                obj.DeclaredType = typeof(ApiEnvelope);
                break;

            case StatusCodes.Status403Forbidden:
                obj.Value = ApiEnvelope.Fail("forbidden", "无权访问该资源。", traceId: traceId);
                obj.DeclaredType = typeof(ApiEnvelope);
                break;

            case StatusCodes.Status404NotFound:
                obj.Value = WrapNotFound(obj.Value, traceId);
                obj.DeclaredType = typeof(ApiEnvelope);
                break;

            case StatusCodes.Status409Conflict:
                obj.Value = WrapConflict(obj.Value, traceId);
                obj.DeclaredType = typeof(ApiEnvelope);
                break;

            case >= StatusCodes.Status400BadRequest:
                obj.Value = WrapGenericClientError(status, obj.Value, traceId);
                obj.DeclaredType = typeof(ApiEnvelope);
                break;
        }
    }

    private static int InferStatusCode(ResultExecutingContext context, ObjectResult obj)
    {
        if (obj.StatusCode is { } explicitCode)
            return explicitCode;

        if (obj is IStatusCodeActionResult sc && sc.StatusCode is { } inferred)
            return inferred;

        return obj switch
        {
            OkObjectResult => StatusCodes.Status200OK,
            CreatedResult => StatusCodes.Status201Created,
            CreatedAtActionResult => StatusCodes.Status201Created,
            CreatedAtRouteResult => StatusCodes.Status201Created,
            AcceptedResult => StatusCodes.Status202Accepted,
            BadRequestObjectResult => StatusCodes.Status400BadRequest,
            UnauthorizedObjectResult => StatusCodes.Status401Unauthorized,
            UnprocessableEntityObjectResult => StatusCodes.Status422UnprocessableEntity,
            NotFoundObjectResult => StatusCodes.Status404NotFound,
            ConflictObjectResult => StatusCodes.Status409Conflict,
            _ => context.HttpContext.Response.StatusCode > 0
                ? context.HttpContext.Response.StatusCode
                : StatusCodes.Status200OK
        };
    }

    private static ApiEnvelope WrapBadRequest(object? value, string traceId)
    {
        if (value is ValidationProblemDetails vpd)
            return ApiEnvelope.Fail("validation_error", "请求参数无效。", vpd.Errors, traceId);

        if (value is ProblemDetails pd)
            return ApiEnvelope.Fail("bad_request", pd.Title ?? "请求无效。", pd, traceId);

        if (value is string s)
            return ApiEnvelope.Fail("bad_request", s, traceId: traceId);

        return ApiEnvelope.Fail("bad_request", "请求无效。", value, traceId);
    }

    private static ApiEnvelope WrapNotFound(object? value, string traceId)
    {
        if (value is ProblemDetails pd)
            return ApiEnvelope.Fail("not_found", pd.Title ?? "资源不存在。", pd, traceId);
        if (value is string s)
            return ApiEnvelope.Fail("not_found", s, traceId: traceId);
        return ApiEnvelope.Fail("not_found", "资源不存在。", traceId: traceId);
    }

    private static ApiEnvelope WrapConflict(object? value, string traceId)
    {
        if (value is string s)
            return ApiEnvelope.Fail("conflict", s, traceId: traceId);
        return ApiEnvelope.Fail("conflict", "与当前状态冲突。", value, traceId);
    }

    private static ApiEnvelope WrapGenericClientError(int status, object? value, string traceId)
    {
        var msg = value switch
        {
            ProblemDetails pd => pd.Title ?? "请求无法处理。",
            string s => s,
            _ => "请求无法处理。"
        };
        return ApiEnvelope.Fail($"http_{status}", msg, value, traceId);
    }
}
