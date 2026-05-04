using Bogus;
using DocumentManager.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace DocumentManager.Infrastructure.Data.Seed;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        if (await context.Folders.AnyAsync())
            return;

        var folderFaker = new Faker<Folder>()
            .RuleFor(f => f.Id, _ => Guid.NewGuid())
            .RuleFor(f => f.Name, f => f.Commerce.Department(3))
            .RuleFor(f => f.CreatedBy, f => f.Person.FullName)
            .RuleFor(f => f.CreatedAt, f => f.Date.Past(2, DateTime.UtcNow));

        var docFaker = new Faker<Document>()
            .RuleFor(d => d.Id, _ => Guid.NewGuid())
            .RuleFor(d => d.Name, f => f.System.FileName())
            .RuleFor(d => d.ContentType, f => f.System.MimeType())
            .RuleFor(d => d.SizeBytes, 102400)
            .RuleFor(d => d.CreatedAt, f => f.Date.Past(1, DateTime.UtcNow))
            .RuleFor(d => d.UpdatedAt, (f, d) => d.CreatedAt.AddDays(f.Random.Int(0, 30)))
            .RuleFor(d => d.Version, 1);

        var versionFaker = new Faker<DocumentVersion>()
            .RuleFor(v => v.Id, _ => Guid.NewGuid())
            .RuleFor(v => v.ChangeSummary, f => f.Lorem.Sentence())
            .RuleFor(v => v.CreatedBy, f => f.Person.FullName)
            .RuleFor(v => v.CreatedAt, f => f.Date.Recent(90, DateTime.UtcNow));

        const int targetFolderCount = 2000;
        var folders = new List<Folder>();
        var rootFolder = new Folder
        {
            Id = Guid.NewGuid(),
            Name = "Root",
            ParentFolderId = null,
            CreatedBy = "System",
            CreatedAt = DateTime.UtcNow.AddYears(-1)
        };
        folders.Add(rootFolder);
        Random rand = new Random();

        while (folders.Count < targetFolderCount)
        {
            var parent = folders[rand.Next(folders.Count)];
            int currentDepth = GetDepth(parent, folders);
            if (currentDepth >= 10) continue;
            var newFolder = folderFaker.Generate();
            newFolder.ParentFolderId = parent.Id;
            var existingNames = folders.Where(f => f.ParentFolderId == parent.Id).Select(f => f.Name).ToHashSet();
            while (existingNames.Contains(newFolder.Name))
                newFolder.Name = folderFaker.Generate().Name;
            folders.Add(newFolder);
        }

        await context.Folders.AddRangeAsync(folders);
        await context.SaveChangesAsync();

        var documents = new List<Document>();
        int targetDocCount = 8000;
        foreach (var folder in folders)
        {
            int docsInFolder = rand.Next(2, 6);
            var folderDocs = docFaker.Generate(docsInFolder);
            var usedNames = new HashSet<string>();
            foreach (var doc in folderDocs)
            {
                while (usedNames.Contains(doc.Name))
                    doc.Name = docFaker.Generate().Name;
                usedNames.Add(doc.Name);
                doc.FolderId = folder.Id;
                documents.Add(doc);
            }
            if (documents.Count >= targetDocCount) break;
        }

        await context.Documents.AddRangeAsync(documents);
        await context.SaveChangesAsync();

        var versions = new List<DocumentVersion>();
        foreach (var doc in documents)
        {
            int versionCount = rand.Next(1, 4);
            int currentVersion = 1;
            for (int i = 0; i < versionCount; i++)
            {
                var version = versionFaker.Generate();
                version.DocumentId = doc.Id;
                version.VersionNumber = currentVersion;
                version.CreatedAt = doc.CreatedAt.AddDays(rand.Next(0, 30));
                versions.Add(version);
                currentVersion++;
            }
            doc.Version = versionCount;
            doc.UpdatedAt = versions.Where(v => v.DocumentId == doc.Id).Max(v => v.CreatedAt);
        }

        await context.DocumentVersions.AddRangeAsync(versions);
        await context.SaveChangesAsync();
    }

    private static int GetDepth(Folder folder, List<Folder> allFolders)
    {
        int depth = 0;
        var current = folder;
        while (current.ParentFolderId != null)
        {
            depth++;
            current = allFolders.FirstOrDefault(f => f.Id == current.ParentFolderId);
            if (current == null) break;
        }
        return depth;
    }
}