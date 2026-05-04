using Microsoft.EntityFrameworkCore;
using DocumentManager.Infrastructure.Data; // або твій реальний namespace DbContext

namespace DocumentManager.UnitTests.Common;

public static class TestDbContextFactory
{
    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}