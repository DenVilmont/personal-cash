using Microsoft.JSInterop;

namespace Infrastructure.Auth;

public sealed class BrowserSessionStorage
{
    private readonly IJSRuntime _jsRuntime;
    private readonly IJSInProcessRuntime _jsInProcessRuntime;

    public BrowserSessionStorage(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
        _jsInProcessRuntime = jsRuntime as IJSInProcessRuntime
            ?? throw new InvalidOperationException("Synchronous JS interop requires Blazor WebAssembly.");
    }

    public void SetItem(string key, string value)
        => _jsInProcessRuntime.InvokeVoid("sessionStorage.setItem", key, value);

    public string? GetItem(string key)
        => _jsInProcessRuntime.Invoke<string?>("sessionStorage.getItem", key);

    public void RemoveItem(string key)
        => _jsInProcessRuntime.InvokeVoid("sessionStorage.removeItem", key);

    public ValueTask RemoveItemAsync(string key)
        => _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", key);
}