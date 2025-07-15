using LangLearn.Backend.Data;
using LangLearn.Backend.Dtos;
using LangLearn.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace LangLearn.Backend.Services;

public class FlashcardService
{
    private readonly AppDbContext _db;

    public FlashcardService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<FlashcardDto>> GetDeckFlashcardsAsync(Guid deckId, Guid userId)
    {
        var deck = await _db.Decks.Include(d => d.Flashcards)
            .FirstOrDefaultAsync(d => d.Id == deckId && d.UserId == userId);
        return deck?.Flashcards?.Select(MapFlashcardToDto).ToList() ?? new List<FlashcardDto>();
    }

    public async Task<FlashcardDto?> GetFlashcardByIdAsync(Guid flashcardId, Guid deckId, Guid userId)
    {
        var flashcard = await _db.Flashcards
            .FirstOrDefaultAsync(fc => fc.Id == flashcardId && 
                                      fc.DeckId == deckId && 
                                      fc.Deck != null && 
                                      fc.Deck.UserId == userId);
        return flashcard == null ? null : MapFlashcardToDto(flashcard);
    }

    public async Task<Flashcard?> CreateFlashcardAsync(Flashcard flashcard, Guid deckId, Guid userId)
    {
        var deck = await _db.Decks.FirstOrDefaultAsync(d => d.Id == deckId && d.UserId == userId);
        if (deck == null)
            return null;

        flashcard.Id = Guid.NewGuid();
        flashcard.DeckId = deckId;
        flashcard.UserId = userId;
        flashcard.CreatedAt = DateTime.UtcNow;
        flashcard.UpdatedAt = DateTime.UtcNow;

        _db.Flashcards.Add(flashcard);
        await _db.SaveChangesAsync();

        return flashcard;
    }

    public async Task<Flashcard?> UpdateFlashcardAsync(Guid flashcardId, Guid deckId, Flashcard updatedFlashcard, Guid userId)
    {
        var flashcard = await _db.Flashcards
            .FirstOrDefaultAsync(fc => fc.Id == flashcardId && 
                                      fc.DeckId == deckId && 
                                      fc.Deck != null && 
                                      fc.Deck.UserId == userId);
        
        if (flashcard == null)
            return null;

        flashcard.Front = updatedFlashcard.Front;
        flashcard.Back = updatedFlashcard.Back;
        flashcard.Notes = updatedFlashcard.Notes ?? flashcard.Notes;
        flashcard.UpdatedAt = DateTime.UtcNow;

        _db.Flashcards.Update(flashcard);
        await _db.SaveChangesAsync();

        return flashcard;
    }

    public async Task<bool> DeleteFlashcardAsync(Guid flashcardId, Guid deckId, Guid userId)
    {
        var flashcard = await _db.Flashcards
            .FirstOrDefaultAsync(fc => fc.Id == flashcardId && 
                                      fc.DeckId == deckId && 
                                      fc.Deck != null && 
                                      fc.Deck.UserId == userId);
        
        if (flashcard == null)
            return false;

        _db.Flashcards.Remove(flashcard);
        await _db.SaveChangesAsync();
        return true;
    }

    private static FlashcardDto MapFlashcardToDto(Flashcard flashcard)
    {
        return new FlashcardDto
        {
            Id = flashcard.Id,
            Front = flashcard.Front,
            Back = flashcard.Back,
            Notes = flashcard.Notes,
            CreatedAt = flashcard.CreatedAt,
            UpdatedAt = flashcard.UpdatedAt
        };
    }
}
