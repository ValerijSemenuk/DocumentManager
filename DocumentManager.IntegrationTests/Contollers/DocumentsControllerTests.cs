using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using DocumentManager.IntegrationTests.Common;

namespace DocumentManager.IntegrationTests.Controllers;

public class DocumentsControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public DocumentsControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

   private async Task<Guid> CreateFolderAsync()
{
    var uniqueName = $"TestFolder_{Guid.NewGuid():N}";
    var response = await _client.PostAsJsonAsync("/api/folders", new
    {
        name = uniqueName,
        parentFolderId = (Guid?)null,
        createdBy = "test"
    });
    var content = await response.Content.ReadAsStringAsync();
Console.WriteLine(content);
    var folder = await response.Content.ReadFromJsonAsync<FolderResponse>();
    return folder!.Id;
}

    [Fact]
    public async Task Create_Document_Should_Return_OK()
    {
        var folderId = await CreateFolderAsync();

        var response = await _client.PostAsJsonAsync("/api/documents", new
        {
            folderId,
            name = $"file_{Guid.NewGuid()}.txt",
            contentType = "text/plain",
            sizeBytes = 1000
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_Document_Duplicate_Name_Should_Return_BadRequest()
    {
        var folderId = await CreateFolderAsync();
        var name = $"duplicate_{Guid.NewGuid()}.txt";

        var first = await _client.PostAsJsonAsync("/api/documents", new
        {
            folderId,
            name,
            contentType = "text/plain",
            sizeBytes = 1000
        });
        first.EnsureSuccessStatusCode();

        var duplicate = await _client.PostAsJsonAsync("/api/documents", new
        {
            folderId,
            name,
            contentType = "text/plain",
            sizeBytes = 1000
        });

        duplicate.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_Document_Should_Return_OK()
    {
        var folderId = await CreateFolderAsync();

        var created = await _client.PostAsJsonAsync("/api/documents", new
        {
            folderId,
            name = $"get_{Guid.NewGuid()}.txt",
            contentType = "text/plain",
            sizeBytes = 1000
        });
        created.EnsureSuccessStatusCode();
        var doc = await created.Content.ReadFromJsonAsync<DocumentResponse>();

        var response = await _client.GetAsync($"/api/documents/{doc!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Update_Document_Should_Create_New_Version()
    {
        var folderId = await CreateFolderAsync();

        var created = await _client.PostAsJsonAsync("/api/documents", new
        {
            folderId,
            name = $"version_{Guid.NewGuid()}.txt",
            contentType = "text/plain",
            sizeBytes = 1000
        });
        created.EnsureSuccessStatusCode();
        var doc = await created.Content.ReadFromJsonAsync<DocumentResponse>();

        var update = await _client.PutAsJsonAsync($"/api/documents/{doc!.Id}", new
        {
            name = $"updated_{Guid.NewGuid()}.txt",
            sizeBytes = 2000
        });

        update.StatusCode.Should().Be(HttpStatusCode.OK);

        var versions = await _client.GetAsync($"/api/documents/{doc.Id}/versions");
        versions.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Delete_Document_Should_Return_NoContent()
    {
        var folderId = await CreateFolderAsync();

        var created = await _client.PostAsJsonAsync("/api/documents", new
        {
            folderId,
            name = $"delete_{Guid.NewGuid()}.txt",
            contentType = "text/plain",
            sizeBytes = 1000
        });
        created.EnsureSuccessStatusCode();
        var doc = await created.Content.ReadFromJsonAsync<DocumentResponse>();

        var response = await _client.DeleteAsync($"/api/documents/{doc!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
[Fact]
public async Task Search_Documents_By_Name_Should_Return_OK()
{
    var folderId = await CreateFolderAsync();
    var uniqueName = $"search_{Guid.NewGuid()}.txt";
    var created = await _client.PostAsJsonAsync("/api/documents", new
    {
        folderId,
        name = uniqueName,
        contentType = "text/plain",
        sizeBytes = 1000
    });
    created.EnsureSuccessStatusCode();

    // Пошук за назвою без розширення .txt
    var searchTerm = uniqueName.Replace(".txt", "");
    var response = await _client.GetAsync($"/api/documents/search?name={searchTerm}");
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    var content = await response.Content.ReadAsStringAsync();
    content.Should().Contain(uniqueName);
}
    private class FolderResponse
    {
        public Guid Id { get; set; }
    }

    private class DocumentResponse
    {
        public Guid Id { get; set; }
    }
}