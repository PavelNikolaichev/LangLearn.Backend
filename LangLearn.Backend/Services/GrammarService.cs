using LangLearn.Backend.Data;
using LangLearn.Backend.Dtos;
using LangLearn.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace LangLearn.Backend.Services;

public class GrammarService
{
    private readonly AppDbContext _db;

    public GrammarService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<GrammarDto>> GetGrammarSetGrammarsAsync(Guid setId, Guid userId)
    {
        var grammarSet = await _db.GrammarSets.Include(gs => gs.Grammars)
            .FirstOrDefaultAsync(gs => gs.Id == setId && gs.UserId == userId);
        return grammarSet?.Grammars?.Select(MapGrammarToDto).ToList() ?? new List<GrammarDto>();
    }

    public async Task<GrammarDto?> GetGrammarByIdAsync(Guid grammarId, Guid setId, Guid userId)
    {
        var grammar = await _db.Grammars
            .FirstOrDefaultAsync(g => g.Id == grammarId && 
                                     g.GrammarSetId == setId && 
                                     g.UserId == userId);
        return grammar == null ? null : MapGrammarToDto(grammar);
    }

    public async Task<Grammar?> CreateGrammarAsync(Grammar grammar, Guid setId, Guid userId)
    {
        var grammarSet = await _db.GrammarSets.FirstOrDefaultAsync(gs => gs.Id == setId && gs.UserId == userId);
        if (grammarSet == null)
            return null;

        grammar.Id = Guid.NewGuid();
        grammar.UserId = userId;
        grammar.GrammarSetId = setId;
        grammar.CreatedAt = DateTime.UtcNow;
        grammar.UpdatedAt = DateTime.UtcNow;

        _db.Grammars.Add(grammar);
        await _db.SaveChangesAsync();

        return grammar;
    }

    public async Task<Grammar?> UpdateGrammarAsync(Guid grammarId, Guid setId, Grammar updatedGrammar, Guid userId)
    {
        var grammar = await _db.Grammars
            .FirstOrDefaultAsync(g => g.Id == grammarId && 
                                     g.GrammarSetId == setId && 
                                     g.UserId == userId);
        
        if (grammar == null)
            return null;

        grammar.Name = updatedGrammar.Name;
        grammar.Description = updatedGrammar.Description;
        grammar.UpdatedAt = DateTime.UtcNow;

        _db.Grammars.Update(grammar);
        await _db.SaveChangesAsync();

        return grammar;
    }

    public async Task<bool> DeleteGrammarAsync(Guid grammarId, Guid setId, Guid userId)
    {
        var grammar = await _db.Grammars
            .FirstOrDefaultAsync(g => g.Id == grammarId && 
                                     g.GrammarSetId == setId && 
                                     g.UserId == userId);
        
        if (grammar == null)
            return false;

        _db.Grammars.Remove(grammar);
        await _db.SaveChangesAsync();
        return true;
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
