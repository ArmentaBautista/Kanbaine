namespace KanbanRedmine.Models;

/// <summary>
/// Argumentos del evento cuando se mueve un issue entre columnas
/// </summary>
public class IssueMoveEventArgs
{
    /// <summary>
    /// ID del issue que se movió
    /// </summary>
    public int IssueId { get; set; }
    
    /// <summary>
    /// ID del nuevo estado
    /// </summary>
    public int NewStatusId { get; set; }
}
