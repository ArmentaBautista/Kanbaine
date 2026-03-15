namespace KanbanRedmine.Models;

/// <summary>
/// Representa una columna del tablero Kanban
/// </summary>
public class KanbanColumnData
{
    /// <summary>
    /// Estado de Redmine asociado a esta columna
    /// </summary>
    public RedmineStatus Status { get; set; } = new();
    
    /// <summary>
    /// Issues en esta columna
    /// </summary>
    public List<RedmineIssue> Issues { get; set; } = new();
    
    /// <summary>
    /// Color de fondo de la columna (opcional)
    /// </summary>
    public string? BackgroundColor { get; set; }
    
    /// <summary>
    /// Límite WIP (Work In Progress) - 0 significa sin límite
    /// </summary>
    public int WipLimit { get; set; } = 0;
    
    /// <summary>
    /// Indica si se excede el límite WIP
    /// </summary>
    public bool IsOverWipLimit => WipLimit > 0 && Issues.Count > WipLimit;
    
    /// <summary>
    /// Cantidad de issues en la columna
    /// </summary>
    public int Count => Issues.Count;
}
