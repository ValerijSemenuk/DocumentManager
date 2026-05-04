using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using DocumentManager.IntegrationTests.Common;

namespace DocumentManager.IntegrationTests.Controllers;

public class FoldersControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public FoldersControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_Root_Folders_Should_Return_OK()
    {
        var response = await _client.GetAsync("/api/folders");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_Folder_By_Id_Should_Return_OK()
    {
        // ств нову папку
        var createResponse = await _client.PostAsJsonAsync("/api/folders", new
        {
            name = $"test_{Guid.NewGuid()}",
            parentFolderId = (Guid?)null,
            createdBy = "test"
        });
        createResponse.EnsureSuccessStatusCode();
        var folder = await createResponse.Content.ReadFromJsonAsync<FolderResponse>();

        // гет за айді
        var response = await _client.GetAsync($"/api/folders/{folder!.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_Folder_Should_Return_OK()
    {
        var response = await _client.PostAsJsonAsync("/api/folders", new
        {
            name = $"new_{Guid.NewGuid()}",
            parentFolderId = (Guid?)null,
            createdBy = "test"
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Delete_Empty_Folder_Should_Return_NoContent()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/folders", new
        {
            name = $"empty_{Guid.NewGuid()}",
            parentFolderId = (Guid?)null,
            createdBy = "test"
        });
        createResponse.EnsureSuccessStatusCode();
        var folder = await createResponse.Content.ReadFromJsonAsync<FolderResponse>();

        var deleteResponse = await _client.DeleteAsync($"/api/folders/{folder!.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_NonEmpty_Folder_Should_Return_BadRequest()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/folders", new
        {
            name = $"nonempty_{Guid.NewGuid()}",
            parentFolderId = (Guid?)null,
            createdBy = "test"
        });
        createResponse.EnsureSuccessStatusCode();
        var folder = await createResponse.Content.ReadFromJsonAsync<FolderResponse>();

        // додаємо док у папку
        await _client.PostAsJsonAsync("/api/documents", new
        {
            folderId = folder!.Id,
            name = $"doc_{Guid.NewGuid()}.txt",
            contentType = "text/plain",
            sizeBytes = 1000
        });

        var deleteResponse = await _client.DeleteAsync($"/api/folders/{folder.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private class FolderResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }
}