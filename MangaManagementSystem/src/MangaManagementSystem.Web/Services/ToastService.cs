namespace MangaManagementSystem.Web.Services;

public enum ToastType
{
    Success,
    Error,
    Warning,
    Info
}

public sealed class ToastMessage
{
    public int Id { get; init; } = Environment.TickCount;
    public string Message { get; init; } = string.Empty;
    public ToastType Type { get; init; } = ToastType.Success;
}

public sealed class ToastService
{
    public event Action<ToastMessage>? OnShow;
    public event Action<int>? OnClose;

    public void Show(string message, ToastType type = ToastType.Success)
    {
        OnShow?.Invoke(new ToastMessage { Message = message, Type = type });
    }

    public void Close(int id) => OnClose?.Invoke(id);
}
