using System.Text.Json.Serialization;

namespace KanbanRedmine.Models;

/// <summary>
/// Usuario de Redmine
/// </summary>
public class RedmineUser
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("login")]
    public string Login { get; set; } = string.Empty;
    
    [JsonPropertyName("firstname")]
    public string FirstName { get; set; } = string.Empty;
    
    [JsonPropertyName("lastname")]
    public string LastName { get; set; } = string.Empty;
    
    [JsonPropertyName("mail")]
    public string? Mail { get; set; }
    
    [JsonPropertyName("api_key")]
    public string? ApiKey { get; set; }
    
    [JsonPropertyName("created_on")]
    public DateTime? CreatedOn { get; set; }
    
    [JsonPropertyName("last_login_on")]
    public DateTime? LastLoginOn { get; set; }
    
    [JsonIgnore]
    public string FullName => $"{FirstName} {LastName}".Trim();
}

/// <summary>
/// Respuesta de la API para usuario actual
/// </summary>
public class CurrentUserResponse
{
    [JsonPropertyName("user")]
    public RedmineUser? User { get; set; }
}
