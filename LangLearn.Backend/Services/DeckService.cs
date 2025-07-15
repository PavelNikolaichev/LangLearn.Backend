using LangLearn.Backend.Data;
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

    public async Task<IEnumerable<Deck>> GetUserDecksAsync(Guid userId)
    {
        return await _db.Decks
            .Where(d => d.UserId == userId)
            .Include(d => d.Flashcards)
            .ToListAsync();
    }

    public async Task<Deck?> GetDeckByIdAsync(Guid id, Guid userId)
    {
        return await _db.Decks
            .Include(d => d.Flashcards)
            .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);
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
}
