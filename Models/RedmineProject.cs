using System.Text.Json.Serialization;

namespace KanbanRedmine.Models;

/// <summary>
/// Proyecto de Redmine
/// </summary>
public class RedmineProject
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("status")]
    public int Status { get; set; }
    
    [JsonPropertyName("is_public")]
    public bool IsPublic { get; set; }
    
    [JsonPropertyName("created_on")]
    public DateTime? CreatedOn { get; set; }
    
    [JsonPropertyName("updated_on")]
    public DateTime? UpdatedOn { get; set; }
}

/// <summary>
/// Respuesta de la API para lista de proyectos
/// </summary>
public class ProjectsResponse
{
    [JsonPropertyName("projects")]
    public List<RedmineProject> Projects { get; set; } = new();
    
    [JsonPropertyName("total_count")]
    public int TotalCount { get; set; }
    
    [JsonPropertyName("offset")]
    public int Offset { get; set; }
    
    [JsonPropertyName("limit")]
    public int Limit { get; set; }
}
