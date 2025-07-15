using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LangLearn.Backend.Models;

public class Deck
{
    [Key] public Guid Id { get; set; }

    [Required] public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required] public Guid UserId { get; set; }
    [ForeignKey("UserId")] public User? User { get; set; }

    public ICollection<Flashcard>? Flashcards { get; set; } = new List<Flashcard>();

    [Required] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Required] public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}