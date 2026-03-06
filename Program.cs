using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor;
using MudBlazor.Services;
using PersonalCash;
using System.Globalization;
using Application.Services;
using Infrastructure.Auth;
using Infrastructure.Persistence;
using Infrastructure.Settings;
using Infrastructure.Localization;
using Domain.Ports;
using Infrastructure.Repositories;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        builder.Services.AddLocalization();
        builder.Services.AddScoped<CultureService>();
        builder.Services.AddBlazoredLocalStorage();

        builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

        builder.Services.AddMudServices(config =>
        {
            config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.TopRight;

            config.SnackbarConfiguration.PreventDuplicates = false;
            config.SnackbarConfiguration.NewestOnTop = true;
            config.SnackbarConfiguration.ShowCloseIcon = true;
            config.SnackbarConfiguration.VisibleStateDuration = 4000;
            config.SnackbarConfiguration.HideTransitionDuration = 500;
            config.SnackbarConfiguration.ShowTransitionDuration = 500;
            config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
        });


        // ---------- BLAZOR AUTH
        builder.Services.AddScoped<CustomAuthStateProvider>(provider =>
            new CustomAuthStateProvider(
                provider.GetRequiredService<ILocalStorageService>(),
                provider.GetRequiredService<Supabase.Client>()
            )
        );

        builder.Services.AddScoped<AuthenticationStateProvider>(provider =>
            provider.GetRequiredService<CustomAuthStateProvider>());

        builder.Services.AddAuthorizationCore();

        // ---------- SUPABASE
        var url = builder.Configuration["Supabase:Url"];
        var key = builder.Configuration["Supabase:Key"];

        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("Supabase config missing. Check wwwroot/appsettings.*.json");

        builder.Services.AddScoped(provider =>
        {
            var client = new Supabase.Client(
                url,
                key,
                new Supabase.SupabaseOptions
                {
                    AutoRefreshToken = true,
                    AutoConnectRealtime = false,
                    SessionHandler = new CustomSupabaseSessionHandler(
                        provider.GetRequiredService<ISyncLocalStorageService>(),
                        provider.GetRequiredService<ILogger<CustomSupabaseSessionHandler>>()
                    )
                }
            );

            // IMPORTANT: restore persisted session on page reload
            client.Auth.LoadSession();

            return client;
        });

        // builder.Services.AddScoped<ISupabaseClient<User, Session, Socket, Channel, Bucket, FileObject>>(args => new Supabase.Client(url, key, new Supabase.SupabaseOptions { AutoConnectRealtime = true }));
        builder.Services.AddScoped<AuthService>();
        builder.Services.AddScoped<DatabaseService>();
        builder.Services.AddScoped<IAccountsRepository, AccountsRepository>();
        builder.Services.AddScoped<ICategoriesRepository, CategoriesRepository>();
        builder.Services.AddScoped<ITransactionsLookup, TransactionsLookup>();
        builder.Services.AddScoped<ILoansRepository, LoansRepository>();
        builder.Services.AddScoped<ILoanPaymentsRepository, LoanPaymentsRepository>();
        builder.Services.AddScoped<ITransactionsRepository, TransactionsRepository>();
        builder.Services.AddScoped<IUserSettingsRepository, UserSettingsRepository>();
        builder.Services.AddScoped<TransactionService>();
        builder.Services.AddScoped<AccountsService>();
        builder.Services.AddScoped<CategoriesService>();
        builder.Services.AddScoped<LoansService>();
        builder.Services.AddScoped<UserSettingsStore>();
        builder.Services.AddScoped<UserSettingsService>();
        builder.Services.AddScoped<CurrentUserService>();

        var host = builder.Build();

        // APPLY persisted culture before first render
        var cultureService = host.Services.GetRequiredService<CultureService>();
        var cultureName = await cultureService.GetCultureAsync();   // "en" / "es" / "ru"
        var culture = new CultureInfo(cultureName);

        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        await host.RunAsync();
    }
}