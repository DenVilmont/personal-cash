using Application.Services;
using Blazored.LocalStorage;
using Domain.Ports;
using Infrastructure.Auth;
using Infrastructure.Localization;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Infrastructure.Settings;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using MudBlazor.Services;

namespace PersonalCash.Composition;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPersonalCashUi(this IServiceCollection services, string baseAddress)
    {
        services.AddLocalization();
        services.AddScoped<CultureService>();
        services.AddBlazoredLocalStorage();

        services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(baseAddress) });

        return services;
    }

    public static IServiceCollection AddPersonalCashMud(this IServiceCollection services)
    {
        services.AddMudServices(config =>
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

        return services;
    }

    public static IServiceCollection AddPersonalCashAuth(this IServiceCollection services)
    {
        services.AddScoped<CustomAuthStateProvider>();
        services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<CustomAuthStateProvider>());
        services.AddAuthorizationCore();

        return services;
    }

    public static IServiceCollection AddPersonalCashSupabase(this IServiceCollection services, IConfiguration config)
    {
        var url = config["Supabase:Url"];
        var key = config["Supabase:Key"];

        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("Supabase config missing. Check wwwroot/appsettings.*.json");

        services.AddScoped(sp =>
        {
            var client = new Supabase.Client(
                url,
                key,
                new Supabase.SupabaseOptions
                {
                    AutoRefreshToken = true,
                    AutoConnectRealtime = false,
                    SessionHandler = new CustomSupabaseSessionHandler(
                        sp.GetRequiredService<ISyncLocalStorageService>(),
                        sp.GetRequiredService<ILogger<CustomSupabaseSessionHandler>>()
                    )
                }
            );

            client.Auth.LoadSession();
            return client;
        });

        return services;
    }

    public static IServiceCollection AddPersonalCashInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<DatabaseService>();

        services.AddScoped<IAccountsRepository, AccountsRepository>();
        services.AddScoped<ICategoriesRepository, CategoriesRepository>();
        services.AddScoped<ITransactionsLookup, TransactionsLookup>();
        services.AddScoped<ILoansRepository, LoansRepository>();
        services.AddScoped<ILoanPaymentsRepository, LoanPaymentsRepository>();
        services.AddScoped<ITransactionsRepository, TransactionsRepository>();
        services.AddScoped<IUserSettingsRepository, UserSettingsRepository>();

        services.AddScoped<UserSettingsStore>();
        services.AddScoped<CurrentUserService>();

        return services;
    }

    public static IServiceCollection AddPersonalCashApplication(this IServiceCollection services)
    {
        services.AddScoped<TransactionService>();
        services.AddScoped<AccountsService>();
        services.AddScoped<CategoriesService>();
        services.AddScoped<LoansService>();
        services.AddScoped<UserSettingsService>();

        return services;
    }
}