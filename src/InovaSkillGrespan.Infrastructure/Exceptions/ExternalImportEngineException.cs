namespace InovaSkillGrespan.Infrastructure.Exceptions;

public sealed class ExternalImportEngineException(
    string message,
    Exception? innerException = null)
    : Exception(message, innerException);
