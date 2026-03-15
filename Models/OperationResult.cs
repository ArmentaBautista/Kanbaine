namespace KanbanRedmine.Models;

/// <summary>
/// Resultado de operación con información de error detallada
/// </summary>
public class OperationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public ErrorType? ErrorType { get; set; }

    public static OperationResult Ok() => new() { Success = true };
    
    public static OperationResult Fail(string message, ErrorType type = Models.ErrorType.Unknown) => 
        new() { Success = false, ErrorMessage = message, ErrorType = type };
}

/// <summary>
/// Resultado de operación con datos
/// </summary>
/// <typeparam name="T">Tipo de datos</typeparam>
public class OperationResult<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public ErrorType? ErrorType { get; set; }

    public static OperationResult<T> Ok(T data) => new() { Success = true, Data = data };
    
    public static OperationResult<T> Fail(string message, ErrorType type = Models.ErrorType.Unknown) => 
        new() { Success = false, ErrorMessage = message, ErrorType = type };
}

/// <summary>
/// Tipo de error para mejor manejo en la UI
/// </summary>
public enum ErrorType
{
    Unknown,
    NetworkError,
    Timeout,
    Unauthorized,
    NotFound,
    ValidationError,
    ServerError,
    WorkflowRestriction
}
