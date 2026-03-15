namespace KanbanRedmine.Models;

/// <summary>
/// Configuración de conexión a Redmine desde appsettings.json
/// </summary>
public class RedmineSettings
{
    public const string SectionName = "Redmine";
    
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiPath { get; set; } = "/";
    public int TimeoutSeconds { get; set; } = 30;
    
    public string GetApiUrl() => $"{BaseUrl.TrimEnd('/')}{ApiPath}";
}
