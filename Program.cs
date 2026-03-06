using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PersonalCash;
using PersonalCash.Composition;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);

        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        builder.Services
            .AddPersonalCashUi(builder.HostEnvironment.BaseAddress)
            .AddPersonalCashMud()
            .AddPersonalCashAuth()
            .AddPersonalCashSupabase(builder.Configuration)
            .AddPersonalCashInfrastructure()
            .AddPersonalCashApplication();

        var host = builder.Build();

        await host.ApplyPersistedCultureAsync();
        await host.RunAsync();
    }
}