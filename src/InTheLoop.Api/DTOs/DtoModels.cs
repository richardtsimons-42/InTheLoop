namespace InTheLoop.Api.DTOs;

public record FamilyDto(
    int Id,
    string Name,
    string? Description,
    string OwnerName,
    int MemberCount,
    string? CoverPhotoUrl,
    DateTime CreatedAt,
    string UserRole = "member");

public record PostDto(
    int Id,
    string Content,
    string AuthorName,
    string? AuthorAvatar,
    int FamilyId,
    string FamilyName,
    List<string> PhotoUrls,
    DateTime CreatedAt);

public record FamilyMemberDto(
    int Id,
    string UserId,
    string Name,
    string? AvatarUrl,
    string Role,
    DateTime JoinedAt);
