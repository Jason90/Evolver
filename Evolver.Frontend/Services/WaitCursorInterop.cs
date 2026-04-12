using Microsoft.JSInterop;

namespace Evolver.Frontend.Services;

public static class WaitCursorInterop
{
    public static async ValueTask SetAsync(IJSRuntime js, bool wait)
    {
        try
        {
            await js.InvokeVoidAsync("evolverUi.setWaitCursor", wait);
        }
        catch (JSDisconnectedException)
        {
        }
    }
}
