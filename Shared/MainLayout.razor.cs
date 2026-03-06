using Domain.Contracts;
using Infrastructure.Auth;
using Infrastructure.Localization;
using Infrastructure.Models;
using Infrastructure.Settings;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using MudBlazor;


namespace PersonalCash.Shared;

public partial class MainLayout : LayoutComponentBase, IDisposable
{
    private bool _drawerOpen = false;

    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] private UserSettingsStore UserSettingsStore { get; set; } = default!;
    [Inject] private AuthService AuthService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private Supabase.Client SupabaseClient { get; set; } = default!;
    [Inject] private CultureService CultureService { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        var culture = await CultureService.GetCultureAsync();
        await CultureService.SetCultureAsync(culture);

        AuthStateProvider.AuthenticationStateChanged += OnAuthStateChanged;

        await UserSettingsStore.GetAsync();
        UserSettingsStore.Changed += OnSettingsChanged;

        NavigationManager.LocationChanged += OnLocationChanged;
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        if (_drawerOpen)
        {
            _drawerOpen = false;
            InvokeAsync(StateHasChanged);
        }
    }

    private void OnAuthStateChanged(Task<AuthenticationState> task)
    {
        _ = HandleAuthStateChangedAsync(task);
    }

    private async Task HandleAuthStateChangedAsync(Task<AuthenticationState> task)
    {
        try
        {
            var state = await task;
            var isAuthed = state.User?.Identity?.IsAuthenticated == true;

            if (!isAuthed)
            {
                UserSettingsStore.Clear();
            }
            else
            {
                await UserSettingsStore.GetAsync(forceRefresh: true);
            }

            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            Snackbar.Add(ex.Message, Severity.Error);
        }
    }

    private void OnSettingsChanged()
        => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        AuthStateProvider.AuthenticationStateChanged -= OnAuthStateChanged;
        UserSettingsStore.Changed -= OnSettingsChanged;
        NavigationManager.LocationChanged -= OnLocationChanged;
    }

    private void DrawerToggle() => _drawerOpen = !_drawerOpen;

    private async Task OnClickLogout()
    {
        try
        {
            await AuthService.Logout();
            UserSettingsStore.Clear();
            NavigationManager.NavigateTo("login", forceLoad: true);
        }
        catch (Exception ex)
        {
            Snackbar.Add(ex.Message, Severity.Error);
        }
    }

    private UserSettingsDto? currentUser => UserSettingsStore.Current;

    private string DisplayName
    {
        get
        {
            var first = currentUser?.FirstName?.Trim();
            var last = currentUser?.LastName?.Trim();
            var full = $"{first} {last}".Trim();
            if (!string.IsNullOrWhiteSpace(full)) return full;
            return SupabaseClient.Auth.CurrentUser?.Email ?? "User";
        }
    }

    private string AvatarSrc
    {
        get
        {
            if (string.IsNullOrWhiteSpace(currentUser?.AvatarBase64)) return "";
            var mime = string.IsNullOrWhiteSpace(currentUser!.AvatarMime) ? "image/png" : currentUser.AvatarMime!;
            return $"data:{mime};base64,{currentUser.AvatarBase64}";
        }
    }

}