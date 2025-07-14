
using LangLearn.Backend.Models;
using Microsoft.EntityFrameworkCore;
namespace LangLearn.Backend.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
}