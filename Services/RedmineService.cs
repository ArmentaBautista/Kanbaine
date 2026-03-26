using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using KanbanRedmine.Models;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace KanbanRedmine.Services;

/// <summary>
/// Implementación del servicio de Redmine
/// </summary>
public class RedmineService : IRedmineService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly RedmineSettings _settings;
    private readonly ILogger<RedmineService> _logger;
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public RedmineService(
        IHttpClientFactory httpClientFactory,
        IOptions<RedmineSettings> settings,
        ILogger<RedmineService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AuthenticationResult?> AuthenticateAsync(string username, string password)
    {
        try
        {
            var client = CreateClient();
            
            // Agregar autenticación básica
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            
            var response = await client.GetAsync("users/current.json");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Autenticación fallida para usuario {Username}. Status: {Status}", 
                    username, response.StatusCode);
                return null;
            }
            
            var content = await response.Content.ReadAsStringAsync();
            var userResponse = JsonSerializer.Deserialize<CurrentUserResponse>(content, JsonOptions);
            
            if (userResponse?.User == null)
            {
                _logger.LogWarning("Respuesta de usuario inválida para {Username}", username);
                return null;
            }
            
            var user = userResponse.User;
            
            return new AuthenticationResult
            {
                UserId = user.Id,
                Login = user.Login,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Mail = user.Mail,
                ApiKey = user.ApiKey ?? string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante autenticación de usuario {Username}", username);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<List<RedmineIssue>> GetMyIssuesAsync(string apiKey)
    {
        try
        {
            var client = CreateClientWithApiKey(apiKey);
            
            // Obtener issues asignados al usuario actual, no cerrados
            var response = await client.GetAsync("issues.json?assigned_to_id=me&status_id=open&limit=100");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Error al obtener issues. Status: {Status}", response.StatusCode);
                return new List<RedmineIssue>();
            }
            
            var content = await response.Content.ReadAsStringAsync();
            var issuesResponse = JsonSerializer.Deserialize<IssuesResponse>(content, JsonOptions);
            
            return issuesResponse?.Issues ?? new List<RedmineIssue>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener issues");
            return new List<RedmineIssue>();
        }
    }

    /// <inheritdoc />
    public async Task<List<RedmineStatus>> GetStatusesAsync(string apiKey)
    {
        try
        {
            var client = CreateClientWithApiKey(apiKey);
            
            var response = await client.GetAsync("issue_statuses.json");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Error al obtener estados. Status: {Status}", response.StatusCode);
                return new List<RedmineStatus>();
            }
            
            var content = await response.Content.ReadAsStringAsync();
            var statusesResponse = JsonSerializer.Deserialize<StatusesResponse>(content, JsonOptions);
            
            return statusesResponse?.IssueStatuses ?? new List<RedmineStatus>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener estados");
            return new List<RedmineStatus>();
        }
    }

    /// <inheritdoc />
    public async Task<bool> UpdateIssueStatusAsync(string apiKey, int issueId, int newStatusId)
    {
        var result = await UpdateIssueStatusWithResultAsync(apiKey, issueId, newStatusId);
        return result.Success;
    }
    
    /// <inheritdoc />
    public async Task<OperationResult> UpdateIssueStatusWithResultAsync(string apiKey, int issueId, int newStatusId)
    {
        try
        {
            var client = CreateClientWithApiKey(apiKey);
            
            var payload = new IssueUpdatePayload
            {
                Issue = new IssueUpdate
                {
                    StatusId = newStatusId
                }
            };
            
            var jsonContent = JsonSerializer.Serialize(payload, JsonOptions);
            _logger.LogDebug("Payload JSON: {Json}", jsonContent);
            
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            
            var response = await client.PutAsync($"issues/{issueId}.json", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Error al actualizar issue {IssueId}. Status: {Status}. Response: {Response}", 
                    issueId, response.StatusCode, errorBody);
                
                return response.StatusCode switch
                {
                    System.Net.HttpStatusCode.Unauthorized => 
                        OperationResult.Fail("Sesión expirada. Por favor, inicie sesión nuevamente.", ErrorType.Unauthorized),
                    System.Net.HttpStatusCode.NotFound => 
                        OperationResult.Fail($"La tarea #{issueId} no fue encontrada.", ErrorType.NotFound),
                    System.Net.HttpStatusCode.UnprocessableEntity => 
                        OperationResult.Fail(ParseRedmineError(errorBody) ?? "No es posible realizar esta transición de estado.", ErrorType.WorkflowRestriction),
                    System.Net.HttpStatusCode.Forbidden =>
                        OperationResult.Fail("No tienes permisos para modificar esta tarea.", ErrorType.Unauthorized),
                    _ => 
                        OperationResult.Fail($"Error del servidor: {response.StatusCode}", ErrorType.ServerError)
                };
            }
            
            _logger.LogInformation("Issue {IssueId} actualizado a estado {StatusId}", issueId, newStatusId);
            return OperationResult.Ok();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error de red al actualizar issue {IssueId}", issueId);
            return OperationResult.Fail("Error de conexión. Verifique su red e intente nuevamente.", ErrorType.NetworkError);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Timeout al actualizar issue {IssueId}", issueId);
            return OperationResult.Fail("La conexión tardó demasiado. Intente nuevamente.", ErrorType.Timeout);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar issue {IssueId}", issueId);
            return OperationResult.Fail("Ocurrió un error inesperado.", ErrorType.Unknown);
        }
    }
    
    /// <summary>
    /// Parsea el mensaje de error de Redmine
    /// </summary>
    private string? ParseRedmineError(string errorBody)
    {
        try
        {
            using var doc = JsonDocument.Parse(errorBody);
            if (doc.RootElement.TryGetProperty("errors", out var errors) && 
                errors.ValueKind == JsonValueKind.Array && 
                errors.GetArrayLength() > 0)
            {
                return errors[0].GetString();
            }
        }
        catch
        {
            // Ignorar errores de parsing
        }
        return null;
    }

    /// <inheritdoc />
    public async Task<List<RedmineProject>> GetProjectsAsync(string apiKey)
    {
        try
        {
            var client = CreateClientWithApiKey(apiKey);
            
            var response = await client.GetAsync("projects.json?limit=100");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Error al obtener proyectos. Status: {Status}", response.StatusCode);
                return new List<RedmineProject>();
            }
            
            var content = await response.Content.ReadAsStringAsync();
            var projectsResponse = JsonSerializer.Deserialize<ProjectsResponse>(content, JsonOptions);
            
            return projectsResponse?.Projects ?? new List<RedmineProject>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener proyectos");
            return new List<RedmineProject>();
        }
    }

    /// <inheritdoc />
    public async Task<List<RedmineIssue>> GetAllMyIssuesAsync(string apiKey)
    {
        try
        {
            var client = CreateClientWithApiKey(apiKey);
            var allIssues = new List<RedmineIssue>();
            
            // Obtener issues abiertos
            var openResponse = await client.GetAsync("issues.json?assigned_to_id=me&status_id=open&limit=100");
            if (openResponse.IsSuccessStatusCode)
            {
                var content = await openResponse.Content.ReadAsStringAsync();
                var issuesResponse = JsonSerializer.Deserialize<IssuesResponse>(content, JsonOptions);
                if (issuesResponse?.Issues != null)
                {
                    allIssues.AddRange(issuesResponse.Issues);
                }
            }
            
            // Obtener issues cerrados
            var closedResponse = await client.GetAsync("issues.json?assigned_to_id=me&status_id=closed&limit=100");
            if (closedResponse.IsSuccessStatusCode)
            {
                var content = await closedResponse.Content.ReadAsStringAsync();
                var issuesResponse = JsonSerializer.Deserialize<IssuesResponse>(content, JsonOptions);
                if (issuesResponse?.Issues != null)
                {
                    allIssues.AddRange(issuesResponse.Issues);
                }
            }
            
            return allIssues;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener todos los issues");
            return new List<RedmineIssue>();
        }
    }

    /// <inheritdoc />
    public async Task<IssueWithJournals?> GetIssueWithJournalsAsync(string apiKey, int issueId)
    {
        try
        {
            var client = CreateClientWithApiKey(apiKey);
            
            var response = await client.GetAsync($"issues/{issueId}.json?include=journals");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Error al obtener issue {IssueId} con journals. Status: {Status}", 
                    issueId, response.StatusCode);
                return null;
            }
            
            var content = await response.Content.ReadAsStringAsync();
            var issueResponse = JsonSerializer.Deserialize<IssueWithJournalsResponse>(content, JsonOptions);
            
            return issueResponse?.Issue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener issue {IssueId} con journals", issueId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<List<StatusChange>> GetRecentStatusChangesAsync(string apiKey, int limit = 20)
    {
        try
        {
            var client = CreateClientWithApiKey(apiKey);
            var statusChanges = new List<StatusChange>();
            
            // Obtener issues recientes (ordenados por actualización)
            var response = await client.GetAsync($"issues.json?assigned_to_id=me&sort=updated_on:desc&limit={Math.Min(limit, 25)}");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Error al obtener issues recientes. Status: {Status}", response.StatusCode);
                return statusChanges;
            }
            
            var content = await response.Content.ReadAsStringAsync();
            var issuesResponse = JsonSerializer.Deserialize<IssuesResponse>(content, JsonOptions);
            
            if (issuesResponse?.Issues == null) return statusChanges;
            
            // Obtener los estados disponibles para mapear IDs a nombres
            var statuses = await GetStatusesAsync(apiKey);
            var statusMap = statuses.ToDictionary(s => s.Id.ToString(), s => s.Name);
            
            // Obtener journals de cada issue
            foreach (var issue in issuesResponse.Issues.Take(10)) // Limitar para rendimiento
            {
                var issueWithJournals = await GetIssueWithJournalsAsync(apiKey, issue.Id);
                if (issueWithJournals?.Journals == null) continue;
                
                // Filtrar solo cambios de estado
                foreach (var journal in issueWithJournals.Journals.OrderByDescending(j => j.CreatedOn))
                {
                    var statusDetail = journal.Details
                        .FirstOrDefault(d => d.Property == "attr" && d.Name == "status_id");
                    
                    if (statusDetail != null)
                    {
                        statusChanges.Add(new StatusChange
                        {
                            IssueId = issue.Id,
                            IssueSubject = issue.Subject,
                            UserName = journal.User?.Name ?? "Usuario desconocido",
                            OldStatus = statusDetail.OldValue != null && statusMap.TryGetValue(statusDetail.OldValue, out var oldName) 
                                ? oldName : statusDetail.OldValue,
                            NewStatus = statusDetail.NewValue != null && statusMap.TryGetValue(statusDetail.NewValue, out var newName) 
                                ? newName : statusDetail.NewValue,
                            ChangedOn = journal.CreatedOn,
                            Notes = journal.Notes
                        });
                        
                        if (statusChanges.Count >= limit) break;
                    }
                }
                
                if (statusChanges.Count >= limit) break;
            }
            
            return statusChanges.OrderByDescending(c => c.ChangedOn).Take(limit).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener historial de cambios de estado");
            return new List<StatusChange>();
        }
    }

    /// <inheritdoc />
    public async Task<List<RedmineReference>> GetPrioritiesAsync(string apiKey)
    {
        try
        {
            var client = CreateClientWithApiKey(apiKey);
            
            var response = await client.GetAsync("enumerations/issue_priorities.json");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Error al obtener prioridades. Status: {Status}", response.StatusCode);
                return new List<RedmineReference>();
            }
            
            var content = await response.Content.ReadAsStringAsync();
            var prioritiesResponse = JsonSerializer.Deserialize<PrioritiesResponse>(content, JsonOptions);
            
            return prioritiesResponse?.IssuePriorities ?? new List<RedmineReference>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener prioridades");
            return new List<RedmineReference>();
        }
    }

    /// <inheritdoc />
    public async Task<List<RedmineReference>> GetProjectMembersAsync(string apiKey, int projectId)
    {
        try
        {
            var client = CreateClientWithApiKey(apiKey);
            
            var response = await client.GetAsync($"projects/{projectId}/memberships.json?limit=100");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Error al obtener miembros del proyecto {ProjectId}. Status: {Status}", 
                    projectId, response.StatusCode);
                return new List<RedmineReference>();
            }
            
            var content = await response.Content.ReadAsStringAsync();
            var membershipsResponse = JsonSerializer.Deserialize<MembershipsResponse>(content, JsonOptions);
            
            return membershipsResponse?.Memberships?
                .Where(m => m.User != null)
                .Select(m => m.User!)
                .ToList() ?? new List<RedmineReference>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener miembros del proyecto {ProjectId}", projectId);
            return new List<RedmineReference>();
        }
    }

    /// <inheritdoc />
    public async Task<OperationResult> UpdateIssuePriorityAsync(string apiKey, int issueId, int priorityId)
    {
        return await UpdateIssueFieldAsync(apiKey, issueId, new IssueUpdate { PriorityId = priorityId });
    }

    /// <inheritdoc />
    public async Task<OperationResult> UpdateIssueAssigneeAsync(string apiKey, int issueId, int? assigneeId)
    {
        return await UpdateIssueFieldAsync(apiKey, issueId, new IssueUpdate { AssignedToId = assigneeId ?? 0 });
    }

    /// <inheritdoc />
    public async Task<OperationResult> AddCommentAsync(string apiKey, int issueId, string comment)
    {
        return await UpdateIssueFieldAsync(apiKey, issueId, new IssueUpdate { Notes = comment });
    }

    /// <summary>
    /// Método genérico para actualizar campos de un issue
    /// </summary>
    private async Task<OperationResult> UpdateIssueFieldAsync(string apiKey, int issueId, IssueUpdate update)
    {
        try
        {
            var client = CreateClientWithApiKey(apiKey);
            
            var payload = new IssueUpdatePayload { Issue = update };
            var jsonContent = JsonSerializer.Serialize(payload, JsonOptions);
            
            _logger.LogDebug("Actualizando issue {IssueId} con payload: {Json}", issueId, jsonContent);
            
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            var response = await client.PutAsync($"issues/{issueId}.json", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Error al actualizar issue {IssueId}. Status: {Status}. Response: {Response}", 
                    issueId, response.StatusCode, errorBody);
                
                return response.StatusCode switch
                {
                    System.Net.HttpStatusCode.Unauthorized => 
                        OperationResult.Fail("Sesión expirada", ErrorType.Unauthorized),
                    System.Net.HttpStatusCode.NotFound => 
                        OperationResult.Fail("Tarea no encontrada", ErrorType.NotFound),
                    System.Net.HttpStatusCode.UnprocessableEntity => 
                        OperationResult.Fail(ParseRedmineError(errorBody) ?? "No se puede realizar esta operación", ErrorType.ValidationError),
                    System.Net.HttpStatusCode.Forbidden =>
                        OperationResult.Fail("No tienes permisos para esta acción", ErrorType.Unauthorized),
                    _ => 
                        OperationResult.Fail($"Error del servidor", ErrorType.ServerError)
                };
            }
            
            return OperationResult.Ok();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error de red al actualizar issue {IssueId}", issueId);
            return OperationResult.Fail("Error de conexión", ErrorType.NetworkError);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar issue {IssueId}", issueId);
            return OperationResult.Fail("Error inesperado", ErrorType.Unknown);
        }
    }

    /// <inheritdoc />
    public async Task<List<RedmineReference>> GetUsersAsync(string apiKey)
    {
        try
        {
            var client = CreateClientWithApiKey(apiKey);
            var allUsers = new List<RedmineUserInfo>();
            int offset = 0;
            int limit = 100;
            int totalCount;
            
            // Paginar para obtener todos los usuarios
            do
            {
                var response = await client.GetAsync($"users.json?status=1&limit={limit}&offset={offset}");
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Error al obtener usuarios. Status: {Status}", response.StatusCode);
                    break;
                }
                
                var content = await response.Content.ReadAsStringAsync();
                var usersResponse = JsonSerializer.Deserialize<UsersResponse>(content, JsonOptions);
                
                if (usersResponse?.Users == null || usersResponse.Users.Count == 0)
                    break;
                    
                allUsers.AddRange(usersResponse.Users);
                totalCount = usersResponse.TotalCount;
                offset += limit;
                
            } while (offset < totalCount);
            
            // Convertir a RedmineReference ordenados por nombre
            return allUsers
                .Select(u => new RedmineReference { Id = u.Id, Name = u.FullName })
                .OrderBy(u => u.Name)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener usuarios");
            return new List<RedmineReference>();
        }
    }

    /// <summary>
    /// Crea un HttpClient base
    /// </summary>
    private HttpClient CreateClient()
    {
        var client = _httpClientFactory.CreateClient("RedmineClient");
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    /// <summary>
    /// Crea un HttpClient con API Key para autenticación
    /// </summary>
    private HttpClient CreateClientWithApiKey(string apiKey)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Redmine-API-Key", apiKey);
        return client;
    }

    public async Task<List<RedmineIssue>> GetIssuesByAssigneeAsync(string apiKey, int? assigneeId)
    {
        try
        {
            var client = CreateClientWithApiKey(apiKey);
            string assignedParam = (assigneeId == null || assigneeId == 0) ? "me" : assigneeId?.ToString() ?? "me";
            var response = await client.GetAsync($"issues.json?assigned_to_id={assignedParam}&status_id=open&limit=100");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Error al obtener issues por usuario. Status: {Status}", response.StatusCode);
                return new List<RedmineIssue>();
            }
            var content = await response.Content.ReadAsStringAsync();
            var issuesResponse = JsonSerializer.Deserialize<IssuesResponse>(content, JsonOptions);
            return issuesResponse?.Issues ?? new List<RedmineIssue>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener issues por usuario");
            return new List<RedmineIssue>();
        }
    }

    public async Task<OperationResult> CreateIssueAsync(string apiKey, NewIssueModel model)
    {
        try
        {
            var client = CreateClientWithApiKey(apiKey);
            var content = JsonSerializer.Serialize(model, JsonOptions);
            var response = await client.PostAsync("issues.json", new StringContent(content, Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Error al crear tarea. Status: {Status}, Response: {Response}", response.StatusCode, errorContent);
                return OperationResult.Fail("Error al crear tarea");
            }

            return OperationResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear tarea");
            return OperationResult.Fail("Error de conexión. Verifique su red e intente nuevamente.", ErrorType.NetworkError);
        }
    }

    /// <summary>
    /// Fetches the list of trackers from Redmine.
    /// </summary>
    public async Task<List<RedmineReference>> GetTrackersAsync(string apiKey)
    {
        try
        {
            var client = CreateClientWithApiKey(apiKey);
            var response = await client.GetAsync("trackers.json");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Error fetching trackers. Status: {Status}", response.StatusCode);
                return new List<RedmineReference>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var trackersResponse = JsonSerializer.Deserialize<TrackersResponse>(content, JsonOptions);

            return trackersResponse?.Trackers ?? new List<RedmineReference>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching trackers");
            return new List<RedmineReference>();
        }
    }
}

public class NewIssueModel
{
    [Required]
    public int? ProjectId { get; set; }
    [Required]
    public int? TrackerId { get; set; }
    [Required]
    public int? StatusId { get; set; }
    [Required]
    [MaxLength(255)]
    public string Subject { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? PriorityId { get; set; }
    public int? AssignedToId { get; set; }
    public DateTime? DueDate { get; set; }
}
