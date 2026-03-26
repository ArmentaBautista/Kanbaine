namespace KanbanRedmine.Models;

/// <summary>
/// Represents the login model for user authentication.
/// </summary>
public class LoginModel
{
    /// <summary>
    /// Gets or sets the username for login.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password for login.
    /// </summary>
    public string Password { get; set; } = string.Empty;
}