using KanbanRedmine.Models;

namespace KanbanRedmine.Services;

/// <summary>
/// Interfaz para el servicio de Redmine
/// Define las operaciones disponibles para comunicarse con la API de Redmine
/// </summary>
public interface IRedmineService
{
    /// <summary>
    /// Autentica un usuario con sus credenciales de Redmine
    /// </summary>
    /// <param name="username">Nombre de usuario</param>
    /// <param name="password">Contraseña</param>
    /// <returns>Información del usuario si la autenticación es exitosa, null si falla</returns>
    Task<AuthenticationResult?> AuthenticateAsync(string username, string password);
    
    /// <summary>
    /// Obtiene los issues asignados al usuario actual
    /// </summary>
    /// <param name="apiKey">API Key del usuario</param>
    /// <returns>Lista de issues</returns>
    Task<List<RedmineIssue>> GetMyIssuesAsync(string apiKey);
    
    /// <summary>
    /// Obtiene todos los estados de issues disponibles
    /// </summary>
    /// <param name="apiKey">API Key del usuario</param>
    /// <returns>Lista de estados</returns>
    Task<List<RedmineStatus>> GetStatusesAsync(string apiKey);
    
    /// <summary>
    /// Actualiza el estado de un issue
    /// </summary>
    /// <param name="apiKey">API Key del usuario</param>
    /// <param name="issueId">ID del issue</param>
    /// <param name="newStatusId">ID del nuevo estado</param>
    /// <returns>True si se actualizó correctamente</returns>
    Task<bool> UpdateIssueStatusAsync(string apiKey, int issueId, int newStatusId);
    
    /// <summary>
    /// Actualiza el estado de un issue con resultado detallado
    /// </summary>
    /// <param name="apiKey">API Key del usuario</param>
    /// <param name="issueId">ID del issue</param>
    /// <param name="newStatusId">ID del nuevo estado</param>
    /// <returns>Resultado de la operación con detalles del error si falla</returns>
    Task<OperationResult> UpdateIssueStatusWithResultAsync(string apiKey, int issueId, int newStatusId);
    
    /// <summary>
    /// Obtiene los proyectos del usuario
    /// </summary>
    /// <param name="apiKey">API Key del usuario</param>
    /// <returns>Lista de proyectos</returns>
    Task<List<RedmineProject>> GetProjectsAsync(string apiKey);
}

/// <summary>
/// Resultado de autenticación
/// </summary>
public class AuthenticationResult
{
    public int UserId { get; set; }
    public string Login { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Mail { get; set; }
    public string ApiKey { get; set; } = string.Empty;
}
