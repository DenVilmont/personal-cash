using Application.Common;
using Application.Services;
using Domain.Contracts;
using Infrastructure.Auth;
using Infrastructure.Localization;
using Infrastructure.Models;
using Infrastructure.Settings;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace PersonalCash.Pages.Settings
{
    public partial class SettingsPage
    {
        [Inject] private UserSettingsService UserSettingsService { get; set; } = default!;
        [Inject] private CurrentUserService CurrentUser { get; set; } = default!;
        [Inject] private NavigationManager Nav { get; set; } = default!;
        [Inject] private AuthService AuthService { get; set; } = default!;
        [Inject] private UserSettingsStore UserSettingsStore { get; set; } = default!;
        [Inject] private CultureService CultureService { get; set; } = default!;

        private UserSettingsDto _settings = new();
        private bool _settingsLoaded;

        private string _currentEmail = "";
        private string? _newEmail;

        private const int AvatarMaxWidth = 256;
        private const int AvatarMaxHeight = 256;
        private const long MaxAvatarBytes = 250 * 1024; // 250KB

        protected override async Task OnInitializedAsync()
        {
            if (!CurrentUser.IsAuthenticated)
                return;
            if (!CurrentUser.TryGetUserId(out _))
                return;

            await RunAsync(LoadCoreAsync);
        }

        private async Task LoadCoreAsync()
        {
            if (!CurrentUser.TryGetUserId(out var userId))
                return;

            _settings = await UserSettingsService.LoadAsync(userId);
            UserSettingsStore.Set(_settings);

            _currentEmail = CurrentUser.Email ?? "";
            _settingsLoaded = true;
        }

        private async Task SaveAsync()
        {
            if (!_settingsLoaded)
                return;

            await RunAsync(async () =>
            {
                if (!CurrentUser.TryGetUserId(out var userId))
                    return;

                var languageChangedResult = await UserSettingsService.SaveAsync(userId, _settings);
                _settings = languageChangedResult.Saved;
                UserSettingsStore.Set(_settings);

                if (languageChangedResult.LanguageChanged)
                {
                    await CultureService.SetCultureAsync(_settings.PreferredLanguage);
                    Nav.NavigateTo(Nav.Uri, forceLoad: true);
                }
            }, successMessage: "Saved");
        }

        private Task ChangeEmailAsync()
        {
            return RunAsync(async () =>
            {
                var email = (_newEmail ?? "").Trim();
                if (string.IsNullOrWhiteSpace(email))
                    throw new AppValidationException("Enter new email");

                await AuthService.RequestEmailChangeAsync(email);
            }, successMessage: @L["EmailChangeRequested"]);
        }
        private string AvatarSrc
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_settings.AvatarBase64))
                    return "";

                var mime = string.IsNullOrWhiteSpace(_settings.AvatarMime) ? "image/png" : _settings.AvatarMime;
                return $"data:{mime};base64,{_settings.AvatarBase64}";
            }
        }

        private async Task OnAvatarSelected(InputFileChangeEventArgs e)
        {
            var file = e.File;
            if (file is null)
                return;

            if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                Snackbar.Add(@L["PleaseSelectAnImageFile"], Severity.Warning);
                return;
            }

            await RunAsync(async () =>
            {
                IBrowserFile resized;
                try
                {
                    resized = await file.RequestImageFileAsync("image/png", AvatarMaxWidth, AvatarMaxHeight);
                }
                catch
                {
                    resized = file;
                }

                using var stream = resized.OpenReadStream(MaxAvatarBytes);
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);

                var bytes = ms.ToArray();
                if (bytes.Length == 0)
                {
                    Snackbar.Add(@L["EmptyFile"], Severity.Warning);
                    return;
                }

                if (!CurrentUser.TryGetUserId(out var userId))
                    return;

                _settings = await UserSettingsService.UpdateAvatarAsync(userId, _settings, resized.ContentType, Convert.ToBase64String(bytes));
                UserSettingsStore.Set(_settings);
            }, successMessage: "AvatarUpdated");
        }

        private Task RemoveAvatarAsync()
            => RunAsync(async () =>
            {
                if (!CurrentUser.TryGetUserId(out var userId))
                    return;

                _settings = await UserSettingsService.RemoveAvatarAsync(userId, _settings);
                UserSettingsStore.Set(_settings);
            }, successMessage: @L["AvatarRemoved"]);
    }
}
