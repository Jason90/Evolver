namespace Evolver.Shared.Api;

/// <summary>
/// 统一 API 响应信封（成功/失败同一 JSON 结构）。
/// </summary>
public interface IApiEnvelope
{
    bool Success { get; }
}

public sealed class ApiEnvelope : IApiEnvelope
{
    public bool Success { get; init; }

    public object? Data { get; init; }

    /// <summary>业务或协议错误码，例如 <c>validation_error</c>、<c>not_found</c>。</summary>
    public string? Code { get; init; }

    public string? Message { get; init; }

    public string? TraceId { get; init; }

    public static ApiEnvelope Ok(object? data = null, string? traceId = null) =>
        new() { Success = true, Data = data, TraceId = traceId };

    public static ApiEnvelope Fail(string code, string message, object? data = null, string? traceId = null) =>
        new() { Success = false, Code = code, Message = message, Data = data, TraceId = traceId };
}
