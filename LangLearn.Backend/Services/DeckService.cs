using LangLearn.Backend.Data;
using LangLearn.Backend.Dtos;
using LangLearn.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace LangLearn.Backend.Services;

public class DeckService
{
    private readonly AppDbContext _db;

    public DeckService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<DeckDto>> GetUserDecksAsync(Guid userId)
    {
        var decks = await _db.Decks
            .Where(d => d.UserId == userId)
            .Include(d => d.Flashcards)
            .ToListAsync();
        return decks.Select(MapDeckToDto);
    }

    public async Task<DeckDto?> GetDeckByIdAsync(Guid id, Guid userId)
    {
        var deck = await _db.Decks
            .Include(d => d.Flashcards)
            .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);
        return deck == null ? null : MapDeckToDto(deck);
    }

    public async Task<Deck> CreateDeckAsync(Deck deck, Guid userId)
    {
        deck.Id = Guid.NewGuid();
        deck.UserId = userId;
        deck.CreatedAt = DateTime.UtcNow;
        deck.UpdatedAt = DateTime.UtcNow;

        _db.Decks.Add(deck);
        await _db.SaveChangesAsync();

        return deck;
    }

    public async Task<Deck?> UpdateDeckAsync(Guid id, Deck updatedDeck, Guid userId)
    {
        var existingDeck = await _db.Decks.FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);
        if (existingDeck == null)
            return null;

        existingDeck.Name = updatedDeck.Name;
        existingDeck.Description = updatedDeck.Description;
        existingDeck.UpdatedAt = DateTime.UtcNow;

        _db.Decks.Update(existingDeck);
        await _db.SaveChangesAsync();

        return existingDeck;
    }

    public async Task<bool> DeleteDeckAsync(Guid id, Guid userId)
    {
        var deck = await _db.Decks.FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);
        if (deck == null)
            return false;

        _db.Decks.Remove(deck);
        await _db.SaveChangesAsync();
        return true;
    }

    private static DeckDto MapDeckToDto(Deck deck)
    {
        return new DeckDto
        {
            Id = deck.Id,
            Name = deck.Name,
            Description = deck.Description,
            CreatedAt = deck.CreatedAt,
            UpdatedAt = deck.UpdatedAt,
            Flashcards = deck.Flashcards?.Select(MapFlashcardToDto).ToList() ?? new List<FlashcardDto>()
        };
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
