namespace PersonalCash.Shared;

public sealed class AppPageTitleState
{
    private string _title = string.Empty;

    public string Title => _title;

    public event Action? Changed;

    public void Set(string title)
    {
        title ??= string.Empty;

        if (_title == title)
            return;

        _title = title;
        Changed?.Invoke();
    }

    public void Clear()
    {
        if (string.IsNullOrEmpty(_title))
            return;

        _title = string.Empty;
        Changed?.Invoke();
    }
}