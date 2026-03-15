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
    
    /// <summary>
    /// Obtiene todos los issues del usuario (abiertos y cerrados) para estadísticas
    /// </summary>
    /// <param name="apiKey">API Key del usuario</param>
    /// <returns>Lista de todos los issues</returns>
    Task<List<RedmineIssue>> GetAllMyIssuesAsync(string apiKey);
    
    /// <summary>
    /// Obtiene un issue con su historial de cambios
    /// </summary>
    /// <param name="apiKey">API Key del usuario</param>
    /// <param name="issueId">ID del issue</param>
    /// <returns>Issue con journals</returns>
    Task<IssueWithJournals?> GetIssueWithJournalsAsync(string apiKey, int issueId);
    
    /// <summary>
    /// Obtiene el historial de cambios de estado recientes
    /// </summary>
    /// <param name="apiKey">API Key del usuario</param>
    /// <param name="limit">Número máximo de cambios a obtener</param>
    /// <returns>Lista de cambios de estado</returns>
    Task<List<StatusChange>> GetRecentStatusChangesAsync(string apiKey, int limit = 20);
    
    /// <summary>
    /// Obtiene las prioridades disponibles
    /// </summary>
    /// <param name="apiKey">API Key del usuario</param>
    /// <returns>Lista de prioridades</returns>
    Task<List<RedmineReference>> GetPrioritiesAsync(string apiKey);
    
    /// <summary>
    /// Obtiene los miembros de un proyecto
    /// </summary>
    /// <param name="apiKey">API Key del usuario</param>
    /// <param name="projectId">ID del proyecto</param>
    /// <returns>Lista de miembros del proyecto</returns>
    Task<List<RedmineReference>> GetProjectMembersAsync(string apiKey, int projectId);
    
    /// <summary>
    /// Actualiza la prioridad de un issue
    /// </summary>
    /// <param name="apiKey">API Key del usuario</param>
    /// <param name="issueId">ID del issue</param>
    /// <param name="priorityId">ID de la nueva prioridad</param>
    /// <returns>Resultado de la operación</returns>
    Task<OperationResult> UpdateIssuePriorityAsync(string apiKey, int issueId, int priorityId);
    
    /// <summary>
    /// Actualiza el usuario asignado a un issue
    /// </summary>
    /// <param name="apiKey">API Key del usuario</param>
    /// <param name="issueId">ID del issue</param>
    /// <param name="assigneeId">ID del usuario asignado (null para quitar asignación)</param>
    /// <returns>Resultado de la operación</returns>
    Task<OperationResult> UpdateIssueAssigneeAsync(string apiKey, int issueId, int? assigneeId);
    
    /// <summary>
    /// Agrega un comentario a un issue
    /// </summary>
    /// <param name="apiKey">API Key del usuario</param>
    /// <param name="issueId">ID del issue</param>
    /// <param name="comment">Texto del comentario</param>
    /// <returns>Resultado de la operación</returns>
    Task<OperationResult> AddCommentAsync(string apiKey, int issueId, string comment);
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
