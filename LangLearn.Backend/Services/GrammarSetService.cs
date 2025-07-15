using LangLearn.Backend.Data;
using LangLearn.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace LangLearn.Backend.Services;

public class GrammarSetService
{
    private readonly AppDbContext _db;

    public GrammarSetService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<GrammarSet>> GetUserGrammarSetsAsync(Guid userId)
    {
        return await _db.GrammarSets
            .Where(gs => gs.UserId == userId)
            .Include(gs => gs.Grammars)
            .ToListAsync();
    }

    public async Task<GrammarSet?> GetGrammarSetByIdAsync(Guid id, Guid userId)
    {
        return await _db.GrammarSets
            .Include(gs => gs.Grammars)
            .FirstOrDefaultAsync(gs => gs.Id == id && gs.UserId == userId);
    }

    public async Task<GrammarSet> CreateGrammarSetAsync(GrammarSet grammarSet, Guid userId)
    {
        grammarSet.Id = Guid.NewGuid();
        grammarSet.UserId = userId;
        grammarSet.CreatedAt = DateTime.UtcNow;
        grammarSet.UpdatedAt = DateTime.UtcNow;

        _db.GrammarSets.Add(grammarSet);
        await _db.SaveChangesAsync();

        return grammarSet;
    }

    public async Task<GrammarSet?> UpdateGrammarSetAsync(Guid id, GrammarSet updatedGrammarSet, Guid userId)
    {
        var existingGrammarSet = await _db.GrammarSets.FirstOrDefaultAsync(gs => gs.Id == id && gs.UserId == userId);
        if (existingGrammarSet == null)
            return null;

        existingGrammarSet.Name = updatedGrammarSet.Name;
        existingGrammarSet.Description = updatedGrammarSet.Description;
        existingGrammarSet.UpdatedAt = DateTime.UtcNow;

        _db.GrammarSets.Update(existingGrammarSet);
        await _db.SaveChangesAsync();

        return existingGrammarSet;
    }

    public async Task<bool> DeleteGrammarSetAsync(Guid id, Guid userId)
    {
        var grammarSet = await _db.GrammarSets.FirstOrDefaultAsync(gs => gs.Id == id && gs.UserId == userId);
        if (grammarSet == null)
            return false;

        _db.GrammarSets.Remove(grammarSet);
        await _db.SaveChangesAsync();
        return true;
    }
}
