namespace InovaSkillGrespan.Application.Abstractions;

public sealed record DatabaseConnectionStatus(
    bool IsConnected,
    string DatabaseName,
    string ServerVersion);
