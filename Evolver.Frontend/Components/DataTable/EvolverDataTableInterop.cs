using Microsoft.JSInterop;

namespace Evolver.Frontend.Components.DataTable;

/// <summary>
/// 与 wwwroot/js/evolverDataTable.js 交互的统一入口，列表页只换数据源 / schema 时复用同一套调用。
/// </summary>
public static class EvolverDataTableInterop
{
    /// <param name="dotNetRef">一般为 <see cref="DotNetObjectReference{T}"/>，传 null 时不回调 .NET。</param>
    public static async Task SyncAsync(
        IJSRuntime js,
        string hostId,
        string json,
        object? dotNetRef,
        string schema,
        long? currentUserId)
    {
        _ = await TrySyncAsync(js, hostId, json, dotNetRef, schema, currentUserId);
    }

    public static async Task<bool> TrySyncAsync(
        IJSRuntime js,
        string hostId,
        string json,
        object? dotNetRef,
        string schema,
        long? currentUserId)
    {
        try
        {
            return await js.InvokeAsync<bool>("evolverDataTable.sync", hostId, json, dotNetRef, schema, currentUserId);
        }
        catch (JSDisconnectedException)
        {
            return false;
        }
    }

    public static async Task DestroyAsync(IJSRuntime js, string hostId)
    {
        try
        {
            await js.InvokeVoidAsync("evolverDataTable.destroy", hostId);
        }
        catch (JSDisconnectedException)
        {
        }
        catch (InvalidOperationException)
        {
        }
    }

    public static async Task SetGlobalFilterAsync(IJSRuntime js, string hostId, string? text)
    {
        try
        {
            await js.InvokeVoidAsync("evolverDataTable.setGlobalFilter", hostId, text ?? "");
        }
        catch (JSDisconnectedException)
        {
        }
    }

    public static async Task DownloadAsync(IJSRuntime js, string hostId, string format)
    {
        try
        {
            await js.InvokeVoidAsync("evolverDataTable.download", hostId, format);
        }
        catch (JSDisconnectedException)
        {
        }
    }

    public static async Task ExpandTreeAllAsync(IJSRuntime js, string hostId)
    {
        try
        {
            await js.InvokeVoidAsync("evolverDataTable.expandTreeAll", hostId);
        }
        catch (JSDisconnectedException)
        {
        }
    }

    public static async Task CollapseTreeAllAsync(IJSRuntime js, string hostId)
    {
        try
        {
            await js.InvokeVoidAsync("evolverDataTable.collapseTreeAll", hostId);
        }
        catch (JSDisconnectedException)
        {
        }
    }
}
