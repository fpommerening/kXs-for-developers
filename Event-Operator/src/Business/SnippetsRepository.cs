using System.Text.Json;

namespace FP.ContainerTraining.EventOperator.Business;

public class SnippetsRepository
{
    private readonly List<Snippet> _snippets = new();
    private bool _isInit;
    private readonly SemaphoreSlim _repositoryLock = new(1, 1);
    private const string RootPath = "/app/snippets";
    public async Task AddSnippetAsync(string content)
    {
        var id = Guid.NewGuid();

        var snipped = new Snippet
        {
            Id = id,
            CreatedAt = DateTimeOffset.Now,
            Content = content
        };
        _snippets.Add(snipped);
        var path = Path.Combine(RootPath, $"{id:N}.json");
        await using var createStream = File.Create(path);
        await JsonSerializer.SerializeAsync(createStream, snipped);
        await createStream.DisposeAsync();
    }

    public async Task<List<Snippet>> GetSnippetsAsync()
    {
        if (_isInit)
        {
            return _snippets;
        }

        try
        {
            await _repositoryLock.WaitAsync();
            if (!Directory.Exists(RootPath))
            {
                Directory.CreateDirectory(RootPath);
            }
            
            var files = Directory.GetFiles(RootPath, "*.json", SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                var jsonString = await File.ReadAllTextAsync(file);
                var snippet = JsonSerializer.Deserialize<Snippet>(jsonString)!;
                _snippets.Add(snippet);
            }
            _isInit = true;
        }
        finally
        {
            _repositoryLock.Release();
        }
        return _snippets;
    }
    
}