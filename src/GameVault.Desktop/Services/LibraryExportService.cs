using System.IO;
using System.Text.Json;
using GameVault.Core.Models;
using GameVault.Data;

namespace GameVault.Services;

public class LibraryExportService
{
    private readonly GameRepository _repository;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public LibraryExportService(GameRepository repository)
    {
        _repository = repository;
    }

    public void ExportJson(string filePath)
    {
        File.WriteAllText(filePath, CreateJson());
    }

    public int ImportJson(string filePath)
    {
        return ImportJsonContent(File.ReadAllText(filePath));
    }

    public string CreateJson()
    {
        return JsonSerializer.Serialize(GetRecords(), _jsonOptions);
    }

    public int ImportJsonContent(string json)
    {
        var records = JsonSerializer.Deserialize<List<LibraryGameRecord>>(json, _jsonOptions)
                      ?? throw new InvalidDataException("JSON file does not contain a game list.");
        return ImportRecords(records);
    }

    private List<LibraryGameRecord> GetRecords() =>
        _repository.GetAllGames().Select(game => new LibraryGameRecord
        {
            Title = game.Title,
            Genre = game.Genre,
            ReleaseYear = game.ReleaseYear,
            Developer = game.Developer,
            InstallPath = game.InstallPath,
            ExecutablePath = game.ExecutablePath,
            PlayTimeMinutes = game.PlayTimeMinutes,
            DateAdded = game.DateAdded
        }).ToList();

    private int ImportRecords(IEnumerable<LibraryGameRecord> records)
    {
        var games = records
            .Where(record => !string.IsNullOrWhiteSpace(record.Title))
            .Select(record => new Game
            {
                Title = record.Title.Trim(),
                Genre = record.Genre.Trim(),
                ReleaseYear = record.ReleaseYear,
                Developer = record.Developer.Trim(),
                InstallPath = record.InstallPath.Trim(),
                ExecutablePath = record.ExecutablePath.Trim(),
                PlayTimeMinutes = Math.Max(0, record.PlayTimeMinutes),
                DateAdded = record.DateAdded == default ? DateTime.Now : record.DateAdded
            });

        return _repository.ImportGames(games);
    }
}
