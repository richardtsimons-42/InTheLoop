namespace InTheLoop.Api.DTOs;

public record FamilyDto(
    int Id,
    string Name,
    string? Description,
    string OwnerName,
    int MemberCount,
    string? CoverPhotoUrl,
    DateTime CreatedAt);

public record PostDto(
    int Id,
    string Content,
    string AuthorName,
    string? AuthorAvatar,
    int FamilyId,
    string FamilyName,
    List<string> PhotoUrls,
    DateTime CreatedAt);
