using KanbanRedmine.Models;

namespace KanbanRedmine.State;

/// <summary>
/// Estado global de la aplicación
/// Maneja el estado compartido entre componentes
/// </summary>
public class AppState
{
    /// <summary>
    /// Usuario actualmente autenticado
    /// </summary>
    public RedmineUser? CurrentUser { get; private set; }
    
    /// <summary>
    /// API Key del usuario autenticado
    /// </summary>
    public string? ApiKey { get; private set; }
    
    /// <summary>
    /// Indica si el usuario está autenticado
    /// </summary>
    public bool IsAuthenticated => CurrentUser != null && !string.IsNullOrEmpty(ApiKey);
    
    /// <summary>
    /// Evento que se dispara cuando cambia el estado de autenticación
    /// </summary>
    public event Action? OnAuthStateChanged;
    
    /// <summary>
    /// Evento que se dispara cuando hay cambios en los datos
    /// </summary>
    public event Action? OnDataChanged;
    
    /// <summary>
    /// Establece el usuario autenticado
    /// </summary>
    public void SetUser(RedmineUser user, string apiKey)
    {
        CurrentUser = user;
        ApiKey = apiKey;
        NotifyAuthStateChanged();
    }
    
    /// <summary>
    /// Cierra la sesión del usuario
    /// </summary>
    public void Logout()
    {
        CurrentUser = null;
        ApiKey = null;
        NotifyAuthStateChanged();
    }
    
    /// <summary>
    /// Notifica que el estado de autenticación ha cambiado
    /// </summary>
    private void NotifyAuthStateChanged() => OnAuthStateChanged?.Invoke();
    
    /// <summary>
    /// Notifica que los datos han cambiado
    /// </summary>
    public void NotifyDataChanged() => OnDataChanged?.Invoke();
}
