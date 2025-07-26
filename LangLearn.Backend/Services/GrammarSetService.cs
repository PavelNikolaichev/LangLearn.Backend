using LangLearn.Backend.Data;
using LangLearn.Backend.Dto;
using LangLearn.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace LangLearn.Backend.Services;

public class GrammarSetService(AppDbContext db)
{
    public async Task<IEnumerable<GrammarSetDto>> GetUserGrammarSetsAsync(Guid userId)
    {
        var grammarSets = await db.GrammarSets
            .Where(gs => gs.UserId == userId)
            .Include(gs => gs.Grammars)
            .ToListAsync();
        return grammarSets.Select(MapGrammarSetToDto);
    }

    public async Task<GrammarSetDto?> GetGrammarSetByIdAsync(Guid id, Guid userId)
    {
        var grammarSet = await db.GrammarSets
            .Include(gs => gs.Grammars)
            .FirstOrDefaultAsync(gs => gs.Id == id && gs.UserId == userId);
        return grammarSet == null ? null : MapGrammarSetToDto(grammarSet);
    }

    public async Task<GrammarSet> CreateGrammarSetAsync(GrammarSet grammarSet, Guid userId)
    {
        grammarSet.Id = Guid.NewGuid();
        grammarSet.UserId = userId;
        grammarSet.CreatedAt = DateTime.UtcNow;
        grammarSet.UpdatedAt = DateTime.UtcNow;

        db.GrammarSets.Add(grammarSet);
        await db.SaveChangesAsync();

        return grammarSet;
    }

    public async Task<GrammarSet?> UpdateGrammarSetAsync(Guid id, GrammarSet updatedGrammarSet, Guid userId)
    {
        var existingGrammarSet = await db.GrammarSets.FirstOrDefaultAsync(gs => gs.Id == id && gs.UserId == userId);
        if (existingGrammarSet == null)
            return null;

        existingGrammarSet.Name = updatedGrammarSet.Name;
        existingGrammarSet.Description = updatedGrammarSet.Description;
        existingGrammarSet.UpdatedAt = DateTime.UtcNow;

        db.GrammarSets.Update(existingGrammarSet);
        await db.SaveChangesAsync();

        return existingGrammarSet;
    }

    public async Task<bool> DeleteGrammarSetAsync(Guid id, Guid userId)
    {
        var grammarSet = await db.GrammarSets.FirstOrDefaultAsync(gs => gs.Id == id && gs.UserId == userId);
        if (grammarSet == null)
            return false;

        db.GrammarSets.Remove(grammarSet);
        await db.SaveChangesAsync();
        return true;
    }

    private static GrammarSetDto MapGrammarSetToDto(GrammarSet grammarSet)
    {
        return new GrammarSetDto
        {
            Id = grammarSet.Id,
            Name = grammarSet.Name,
            Description = grammarSet.Description,
            CreatedAt = grammarSet.CreatedAt,
            UpdatedAt = grammarSet.UpdatedAt,
            Grammars = grammarSet.Grammars?.Select(MapGrammarToDto).ToList() ?? new List<GrammarDto>()
        };
    }

    private static GrammarDto MapGrammarToDto(Grammar grammar)
    {
        return new GrammarDto
        {
            Id = grammar.Id,
            Name = grammar.Name,
            Description = grammar.Description,
            CreatedAt = grammar.CreatedAt,
            UpdatedAt = grammar.UpdatedAt
        };
    }
}
