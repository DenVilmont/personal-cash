using System.Text.Json;
using Microsoft.JSInterop;

namespace PersonalCash.Pages.Debts.State
{
    public sealed class DebtsUiRestoreService(IJSRuntime jsRuntime)
    {
        private const string StorageKey = "debts-ui-restore";
        private readonly IJSRuntime _jsRuntime = jsRuntime;

        public async ValueTask SaveAsync(DebtsUiRestoreState state)
        {
            var json = JsonSerializer.Serialize(state);
            await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", StorageKey, json);
        }

        public async ValueTask<DebtsUiRestoreState?> GetAsync()
        {
            var json = await _jsRuntime.InvokeAsync<string?>("sessionStorage.getItem", StorageKey);

            if(string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                return JsonSerializer.Deserialize<DebtsUiRestoreState>(json);
            }
            catch
            {
                await ClearAsync();
                return null;
            }
        }

        public ValueTask ClearAsync() => _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", StorageKey);
    }
}
