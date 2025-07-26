using LangLearn.Backend.Dto;

namespace LangLearn.Backend.Dto;

public class DeckDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<FlashcardDto> Flashcards { get; set; } = new();
}

