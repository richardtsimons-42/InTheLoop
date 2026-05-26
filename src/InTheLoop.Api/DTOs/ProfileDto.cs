namespace InTheLoop.Api.DTOs;

public record ProfileResponseDto(
    string Id,
    string Email,
    string FirstName,
    string LastName,
    string? AvatarUrl,
    bool IsVerified,
    DateTime CreatedAt);
