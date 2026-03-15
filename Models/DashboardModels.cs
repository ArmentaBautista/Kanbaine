using System.Text.Json.Serialization;

namespace KanbanRedmine.Models;

/// <summary>
/// Estadísticas del dashboard
/// </summary>
public class DashboardStats
{
    public int TotalIssues { get; set; }
    public int OpenIssues { get; set; }
    public int ClosedIssues { get; set; }
    public List<StatusStat> IssuesByStatus { get; set; } = new();
    public List<ProjectStat> IssuesByProject { get; set; } = new();
    public List<PriorityStat> IssuesByPriority { get; set; } = new();
}

/// <summary>
/// Estadísticas por estado
/// </summary>
public class StatusStat
{
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public int Count { get; set; }
    public string Color { get; set; } = "#6b7280";
    public double Percentage { get; set; }
}

/// <summary>
/// Estadísticas por proyecto
/// </summary>
public class ProjectStat
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

/// <summary>
/// Estadísticas por prioridad
/// </summary>
public class PriorityStat
{
    public int PriorityId { get; set; }
    public string PriorityName { get; set; } = string.Empty;
    public int Count { get; set; }
    public string Color { get; set; } = "#6b7280";
}

/// <summary>
/// Punto de datos para el gráfico burndown
/// </summary>
public class BurndownPoint
{
    public DateTime Date { get; set; }
    public int OpenIssues { get; set; }
    public int ClosedIssues { get; set; }
    public int TotalIssues { get; set; }
}

/// <summary>
/// Entrada del historial de cambios de un issue
/// </summary>
public class IssueJournal
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("user")]
    public RedmineReference? User { get; set; }
    
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
    
    [JsonPropertyName("created_on")]
    public DateTime CreatedOn { get; set; }
    
    [JsonPropertyName("details")]
    public List<JournalDetail> Details { get; set; } = new();
}

/// <summary>
/// Detalle de un cambio en el journal
/// </summary>
public class JournalDetail
{
    [JsonPropertyName("property")]
    public string Property { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("old_value")]
    public string? OldValue { get; set; }
    
    [JsonPropertyName("new_value")]
    public string? NewValue { get; set; }
}

/// <summary>
/// Cambio de estado formateado para mostrar en UI
/// </summary>
public class StatusChange
{
    public int IssueId { get; set; }
    public string IssueSubject { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? OldStatus { get; set; }
    public string? NewStatus { get; set; }
    public DateTime ChangedOn { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Respuesta de issue con journals
/// </summary>
public class IssueWithJournalsResponse
{
    [JsonPropertyName("issue")]
    public IssueWithJournals? Issue { get; set; }
}

/// <summary>
/// Issue con historial de cambios
/// </summary>
public class IssueWithJournals : RedmineIssue
{
    [JsonPropertyName("journals")]
    public List<IssueJournal> Journals { get; set; } = new();
}
