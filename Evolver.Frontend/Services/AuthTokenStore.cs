using System.Net.Http.Headers;
using Microsoft.JSInterop;

namespace Evolver.Frontend.Services;

public sealed class AuthTokenStore(HttpClient http)
{
    public event Action? AuthenticationChanged;

    public string? AccessToken { get; private set; }

    public void Set(string accessToken)
    {
        AccessToken = accessToken;
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        AuthenticationChanged?.Invoke();
    }

    public void Clear()
    {
        AccessToken = null;
        http.DefaultRequestHeaders.Authorization = null;
        AuthenticationChanged?.Invoke();
    }

    /// <summary>从浏览器 localStorage 恢复令牌（需在首帧渲染后调用）。</summary>
    public async Task RestoreFromBrowserAsync(IJSRuntime js)
    {
        if (!string.IsNullOrEmpty(AccessToken))
            return;

        try
        {
            var t = await js.InvokeAsync<string?>("evolverAuthGetToken");
            if (!string.IsNullOrWhiteSpace(t))
                Set(t);
        }
        catch
        {
            // JS 未就绪时忽略
        }
    }

    public async Task PersistToBrowserAsync(IJSRuntime js)
    {
        try
        {
            if (!string.IsNullOrEmpty(AccessToken))
                await js.InvokeVoidAsync("evolverAuthSetToken", AccessToken);
            else
                await js.InvokeVoidAsync("evolverAuthClearToken");
        }
        catch
        {
            // ignore
        }
    }

    public async Task ClearBrowserAsync(IJSRuntime js)
    {
        try
        {
            await js.InvokeVoidAsync("evolverAuthClearToken");
        }
        catch
        {
            // ignore
        }
    }
}
