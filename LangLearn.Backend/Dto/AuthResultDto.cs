namespace LangLearn.Backend.Dto;

// ReSharper disable once NotAccessedPositionalProperty.Global - ExpiresAt is intended to be used by the frontend
public record AuthResultDto(bool Success, string Message, string? Token = null, DateTime? ExpiresAt = null);