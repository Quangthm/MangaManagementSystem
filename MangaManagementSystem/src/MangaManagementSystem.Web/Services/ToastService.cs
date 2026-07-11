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
    public event Func<ToastMessage, Task>? OnShow;
    public event Func<int, Task>? OnClose;

    public async Task Show(string message, ToastType type = ToastType.Success)
    {
        if (OnShow != null)
            await OnShow.Invoke(new ToastMessage { Message = message, Type = type });
    }

    public async Task Close(int id)
    {
        if (OnClose != null)
            await OnClose.Invoke(id);
    }
}
