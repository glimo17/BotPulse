namespace BotPulse.Core.Exceptions;

/// <summary>Base exception for all BotPulse domain and application errors.</summary>
public class BotPulseException : Exception
{
    public BotPulseException(string message) : base(message) { }
    public BotPulseException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>Thrown when an RPA provider returns an error or is unreachable.</summary>
public class ProviderException : BotPulseException
{
    public string ProviderName { get; }
    public ProviderException(string providerName, string message) : base(message) => ProviderName = providerName;
    public ProviderException(string providerName, string message, Exception inner) : base(message, inner) => ProviderName = providerName;
}

/// <summary>Thrown when an authentication attempt fails.</summary>
public class AuthenticationException : BotPulseException
{
    public AuthenticationException(string message) : base(message) { }
}

/// <summary>Thrown when a user lacks permission to perform an action.</summary>
public class AuthorizationException : BotPulseException
{
    public AuthorizationException(string message) : base(message) { }
}

/// <summary>Thrown when input validation fails.</summary>
public class ValidationException : BotPulseException
{
    public IReadOnlyList<ValidationError> Errors { get; }

    public ValidationException(IReadOnlyList<ValidationError> errors)
        : base("One or more validation errors occurred.") => Errors = errors;
}

/// <summary>Represents a single field validation error.</summary>
public sealed record ValidationError(string Field, string Message);

/// <summary>Thrown when a requested entity does not exist.</summary>
public class EntityNotFoundException : BotPulseException
{
    public EntityNotFoundException(string entityType, object id)
        : base($"{entityType} with id '{id}' was not found.") { }
}
