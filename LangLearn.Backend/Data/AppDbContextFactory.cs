using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LangLearn.Backend.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBulder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBulder.UseSqlite("Data Source=langlearn.db");

        return new AppDbContext(optionsBulder.Options);
    }

}