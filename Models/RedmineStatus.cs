using System.Text.Json.Serialization;

namespace KanbanRedmine.Models;

/// <summary>
/// Estado de issues de Redmine
/// </summary>
public class RedmineStatus
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("is_closed")]
    public bool IsClosed { get; set; }
}

/// <summary>
/// Respuesta de la API para lista de estados
/// </summary>
public class StatusesResponse
{
    [JsonPropertyName("issue_statuses")]
    public List<RedmineStatus> IssueStatuses { get; set; } = new();
}
