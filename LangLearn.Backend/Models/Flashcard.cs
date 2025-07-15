using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace LangLearn.Backend.Models;

public class Flashcard
{
    [Key] public Guid Id { get; set; }

    [Required] public string Front { get; set; } = string.Empty;

    [Required] public string Back { get; set; } = string.Empty;

    public string? Notes { get; set; }

    [Required] public Guid UserId { get; set; }
    [ForeignKey("UserId")] public User? User { get; set; }

    [Required] public Guid DeckId { get; set; }
    [ForeignKey("DeckId")] [JsonIgnore] public Deck? Deck { get; set; }

    [Required] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Required] public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}