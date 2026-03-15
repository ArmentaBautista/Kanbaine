namespace KanbanRedmine.Services;

/// <summary>
/// Servicio para manejar notificaciones toast
/// </summary>
public class NotificationService
{
    public event Action<ToastNotification>? OnShow;
    public event Action<Guid>? OnHide;

    public void ShowSuccess(string message, int durationMs = 3000)
    {
        Show(new ToastNotification
        {
            Message = message,
            Type = ToastType.Success,
            DurationMs = durationMs
        });
    }

    public void ShowError(string message, int durationMs = 5000)
    {
        Show(new ToastNotification
        {
            Message = message,
            Type = ToastType.Error,
            DurationMs = durationMs
        });
    }

    public void ShowWarning(string message, int durationMs = 4000)
    {
        Show(new ToastNotification
        {
            Message = message,
            Type = ToastType.Warning,
            DurationMs = durationMs
        });
    }

    public void ShowInfo(string message, int durationMs = 3000)
    {
        Show(new ToastNotification
        {
            Message = message,
            Type = ToastType.Info,
            DurationMs = durationMs
        });
    }

    private void Show(ToastNotification notification)
    {
        OnShow?.Invoke(notification);
    }

    public void Hide(Guid id)
    {
        OnHide?.Invoke(id);
    }
}

public class ToastNotification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Message { get; set; } = string.Empty;
    public ToastType Type { get; set; } = ToastType.Info;
    public int DurationMs { get; set; } = 3000;
}

public enum ToastType
{
    Success,
    Error,
    Warning,
    Info
}
