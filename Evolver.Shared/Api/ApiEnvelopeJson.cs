namespace Evolver.Shared.Api;

/// <summary>
/// 与 <see cref="ApiEnvelope"/> JSON 形状一致，用于客户端反序列化（可写属性）。
/// </summary>
public sealed class ApiEnvelopeJson<T>
{
    public bool Success { get; set; }

    public T? Data { get; set; }

    public string? Code { get; set; }

    public string? Message { get; set; }

    public string? TraceId { get; set; }
}
