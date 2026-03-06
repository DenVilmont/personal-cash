using Domain.Contracts;
using Infrastructure.Mapping;
using Infrastructure.Models;
using Infrastructure.Persistence;

namespace Infrastructure.Settings;

public sealed class UserSettingsStore(DatabaseService db, Supabase.Client supabase)
{
    private readonly DatabaseService _db = db;
    private readonly Supabase.Client _supabase = supabase;

    private readonly SemaphoreSlim _gate = new(1, 1);

    private UserSettingsDto? _current;
    private string? _currentUserId;

    public event Action? Changed;

    public UserSettingsDto? Current => _current;

    public async Task<UserSettingsDto?> GetAsync(bool forceRefresh = false)
    {
        var userId = _supabase.Auth.CurrentUser?.Id;

        if (string.IsNullOrWhiteSpace(userId))
        {
            ClearInternal();
            return null;
        }

        if (!forceRefresh && _current is not null && _currentUserId == userId)
            return _current;

        await _gate.WaitAsync();
        try
        {
            userId = _supabase.Auth.CurrentUser?.Id;

            if (string.IsNullOrWhiteSpace(userId))
            {
                ClearInternal();
                return null;
            }

            if (!forceRefresh && _current is not null && _currentUserId == userId)
                return _current;

            // RLS вернет только строку текущего пользователя
            var list = await _db.From<UserSettings>();
            _current = list.FirstOrDefault()?.ToDto();
            _currentUserId = userId;

            Changed?.Invoke();
            return _current;
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Set(UserSettingsDto settings)
    {
        _current = settings;
        _currentUserId = _supabase.Auth.CurrentUser?.Id;
        Changed?.Invoke();
    }

    public void Clear()
    {
        ClearInternal();
        Changed?.Invoke();
    }

    private void ClearInternal()
    {
        _current = null;
        _currentUserId = null;
    }
}
