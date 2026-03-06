using System.Globalization;
using Infrastructure.Localization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace PersonalCash.Composition;

internal static class WebAssemblyHostExtensions
{
    public static async Task ApplyPersistedCultureAsync(this WebAssemblyHost host)
    {
        var cultureService = host.Services.GetRequiredService<CultureService>();
        var cultureName = await cultureService.GetCultureAsync();
        var culture = new CultureInfo(cultureName);

        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }
}