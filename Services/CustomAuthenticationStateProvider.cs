using System.Security.Claims;
using KanbanRedmine.Models;
using KanbanRedmine.State;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace KanbanRedmine.Services;

/// <summary>
/// Proveedor de estado de autenticación personalizado
/// Maneja la autenticación basada en la sesión con Redmine
/// </summary>
public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ProtectedSessionStorage _sessionStorage;
    private readonly AppState _appState;
    private readonly IRedmineService _redmineService;
    private readonly ILogger<CustomAuthenticationStateProvider> _logger;
    
    private const string UserSessionKey = "RedmineUser";
    private const string ApiKeySessionKey = "RedmineApiKey";
    
    private ClaimsPrincipal _anonymous = new(new ClaimsIdentity());

    public CustomAuthenticationStateProvider(
        ProtectedSessionStorage sessionStorage,
        AppState appState,
        IRedmineService redmineService,
        ILogger<CustomAuthenticationStateProvider> logger)
    {
        _sessionStorage = sessionStorage;
        _appState = appState;
        _redmineService = redmineService;
        _logger = logger;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var userResult = await _sessionStorage.GetAsync<RedmineUser>(UserSessionKey);
            var apiKeyResult = await _sessionStorage.GetAsync<string>(ApiKeySessionKey);
            
            if (!userResult.Success || userResult.Value == null || 
                !apiKeyResult.Success || string.IsNullOrEmpty(apiKeyResult.Value))
            {
                return new AuthenticationState(_anonymous);
            }
            
            var user = userResult.Value;
            var apiKey = apiKeyResult.Value;
            
            // Actualizar AppState
            _appState.SetUser(user, apiKey);
            
            var claims = CreateClaims(user);
            var identity = new ClaimsIdentity(claims, "RedmineAuth");
            var principal = new ClaimsPrincipal(identity);
            
            return new AuthenticationState(principal);
        }
        catch (InvalidOperationException)
        {
            // Durante el prerenderizado, JavaScript no está disponible
            // Retornamos estado anónimo y se verificará después del render
            return new AuthenticationState(_anonymous);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener estado de autenticación");
            return new AuthenticationState(_anonymous);
        }
    }

    /// <summary>
    /// Autentica al usuario con Redmine
    /// </summary>
    public async Task<AuthenticationResult?> LoginAsync(string username, string password)
    {
        try
        {
            var result = await _redmineService.AuthenticateAsync(username, password);
            
            if (result == null)
            {
                return null;
            }
            
            var user = new RedmineUser
            {
                Id = result.UserId,
                Login = result.Login,
                FirstName = result.FirstName,
                LastName = result.LastName,
                Mail = result.Mail,
                ApiKey = result.ApiKey
            };
            
            // Guardar en sesión
            await _sessionStorage.SetAsync(UserSessionKey, user);
            await _sessionStorage.SetAsync(ApiKeySessionKey, result.ApiKey);
            
            // Actualizar AppState
            _appState.SetUser(user, result.ApiKey);
            
            // Notificar cambio de estado
            var claims = CreateClaims(user);
            var identity = new ClaimsIdentity(claims, "RedmineAuth");
            var principal = new ClaimsPrincipal(identity);
            
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(principal)));
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante login");
            return null;
        }
    }

    /// <summary>
    /// Cierra la sesión del usuario
    /// </summary>
    public async Task LogoutAsync()
    {
        try
        {
            await _sessionStorage.DeleteAsync(UserSessionKey);
            await _sessionStorage.DeleteAsync(ApiKeySessionKey);
            
            _appState.Logout();
            
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_anonymous)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante logout");
        }
    }

    /// <summary>
    /// Crea los claims para el usuario autenticado
    /// </summary>
    private static List<Claim> CreateClaims(RedmineUser user)
    {
        return new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Login),
            new(ClaimTypes.GivenName, user.FirstName),
            new(ClaimTypes.Surname, user.LastName),
            new(ClaimTypes.Email, user.Mail ?? string.Empty),
            new("FullName", user.FullName)
        };
    }
}
