using System.Text.Json.Serialization;

namespace KanbanRedmine.Models;

/// <summary>
/// Issue (tarea) de Redmine
/// </summary>
public class RedmineIssue
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("status")]
    public RedmineReference? Status { get; set; }
    
    [JsonPropertyName("priority")]
    public RedmineReference? Priority { get; set; }
    
    [JsonPropertyName("project")]
    public RedmineReference? Project { get; set; }
    
    [JsonPropertyName("tracker")]
    public RedmineReference? Tracker { get; set; }
    
    [JsonPropertyName("author")]
    public RedmineReference? Author { get; set; }
    
    [JsonPropertyName("assigned_to")]
    public RedmineReference? AssignedTo { get; set; }
    
    [JsonPropertyName("due_date")]
    public string? DueDateString { get; set; }
    
    [JsonPropertyName("start_date")]
    public string? StartDateString { get; set; }
    
    [JsonPropertyName("done_ratio")]
    public int DoneRatio { get; set; }
    
    [JsonPropertyName("estimated_hours")]
    public double? EstimatedHours { get; set; }
    
    [JsonPropertyName("created_on")]
    public DateTime CreatedOn { get; set; }
    
    [JsonPropertyName("updated_on")]
    public DateTime UpdatedOn { get; set; }
    
    /// <summary>
    /// Fecha de vencimiento parseada
    /// </summary>
    [JsonIgnore]
    public DateTime? DueDate => string.IsNullOrEmpty(DueDateString) 
        ? null 
        : DateTime.TryParse(DueDateString, out var date) ? date : null;
    
    /// <summary>
    /// Indica si la tarea está vencida
    /// </summary>
    [JsonIgnore]
    public bool IsOverdue => DueDate.HasValue && DueDate.Value.Date < DateTime.Today;
    
    /// <summary>
    /// Indica si la tarea vence pronto (en los próximos 3 días)
    /// </summary>
    [JsonIgnore]
    public bool IsDueSoon => DueDate.HasValue 
        && DueDate.Value.Date >= DateTime.Today 
        && DueDate.Value.Date <= DateTime.Today.AddDays(3);
    
    /// <summary>
    /// Obtiene la clase CSS según la prioridad
    /// </summary>
    [JsonIgnore]
    public string PriorityCssClass
    {
        get
        {
            if (Priority == null) return "priority-normal";
            
            return Priority.Name?.ToLower() switch
            {
                "low" or "baja" => "priority-low",
                "normal" => "priority-normal",
                "high" or "alta" => "priority-high",
                "urgent" or "urgente" => "priority-urgent",
                "immediate" or "inmediata" => "priority-immediate",
                _ => "priority-normal"
            };
        }
    }
}

/// <summary>
/// Referencia genérica de Redmine (usado para relaciones como status, priority, project, etc.)
/// </summary>
public class RedmineReference
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Respuesta de la API para lista de issues
/// </summary>
public class IssuesResponse
{
    [JsonPropertyName("issues")]
    public List<RedmineIssue> Issues { get; set; } = new();
    
    [JsonPropertyName("total_count")]
    public int TotalCount { get; set; }
    
    [JsonPropertyName("offset")]
    public int Offset { get; set; }
    
    [JsonPropertyName("limit")]
    public int Limit { get; set; }
}

/// <summary>
/// Payload para actualizar un issue
/// </summary>
public class IssueUpdatePayload
{
    [JsonPropertyName("issue")]
    public IssueUpdate Issue { get; set; } = new();
}

/// <summary>
/// Datos de actualización de issue
/// </summary>
public class IssueUpdate
{
    [JsonPropertyName("status_id")]
    public int? StatusId { get; set; }
    
    [JsonPropertyName("priority_id")]
    public int? PriorityId { get; set; }
    
    [JsonPropertyName("assigned_to_id")]
    public int? AssignedToId { get; set; }
    
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}
