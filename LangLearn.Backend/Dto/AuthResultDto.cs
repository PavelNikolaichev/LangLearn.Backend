namespace LangLearn.Backend.Dto;

public record AuthResultDto(bool Success, string Message, string? Token = null);