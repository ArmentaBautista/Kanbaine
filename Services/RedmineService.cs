using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using KanbanRedmine.Models;
using Microsoft.Extensions.Options;

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
}
