namespace LangLearn.Backend.Dtos;

using System;
using System.Collections.Generic;

public class GrammarDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

