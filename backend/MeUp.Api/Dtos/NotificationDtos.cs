namespace MeUp.Api.Dtos;

public record NotificationDto(
    Guid Id,
    string Type,
    string Title,
    string Message,
    string? Link,
    bool IsRead,
    DateTime CreatedAt);
