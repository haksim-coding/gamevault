using System.Windows;
using System.Windows.Input;
using GameVault.Services;

namespace GameVault.ViewModels;

public class ReportsViewModel : ViewModelBase
{
    private readonly ReportDataService _dataService;
    private readonly PdfReportService _pdfReportService;
    private int _totalGames;
    private string _totalPlayTime = string.Empty;
    private string _mostCommonGenre = string.Empty;
    private string _newestAddedGame = string.Empty;
    private int _unlockedAchievements;
    private string _statusMessage = string.Empty;

    public ReportsViewModel(
        ReportDataService dataService,
        PdfReportService pdfReportService)
    {
        _dataService = dataService;
        _pdfReportService = pdfReportService;
        GeneratePdfCommand = new RelayCommand(GeneratePdf);
        Refresh();
    }

    public int TotalGames
    {
        get => _totalGames;
        private set => SetProperty(ref _totalGames, value);
    }

    public string TotalPlayTime
    {
        get => _totalPlayTime;
        private set => SetProperty(ref _totalPlayTime, value);
    }

    public string MostCommonGenre
    {
        get => _mostCommonGenre;
        private set => SetProperty(ref _mostCommonGenre, value);
    }

    public string NewestAddedGame
    {
        get => _newestAddedGame;
        private set => SetProperty(ref _newestAddedGame, value);
    }

    public int UnlockedAchievements
    {
        get => _unlockedAchievements;
        private set => SetProperty(ref _unlockedAchievements, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public ICommand GeneratePdfCommand { get; }

    public void Refresh()
    {
        var data = _dataService.GetReportData();
        TotalGames = data.TotalGames;
        TotalPlayTime = FormatMinutes(data.TotalPlayTimeMinutes);
        MostCommonGenre = data.MostCommonGenre;
        NewestAddedGame = data.NewestAddedGame?.Title ?? "No games";
        UnlockedAchievements = data.UnlockedAchievementCount;
    }

    private void GeneratePdf()
    {
        try
        {
            var filePath = _pdfReportService.GenerateLibraryReport(_dataService.GetReportData());
            StatusMessage = $"Report saved to: {filePath}";
            MessageBox.Show(
                $"PDF report successfully generated:\n{filePath}",
                "GameVault Report",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception exception)
        {
            StatusMessage = $"Report could not be generated: {exception.Message}";
            MessageBox.Show(
                StatusMessage,
                "PDF Report Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private static string FormatMinutes(int minutes) =>
        $"{minutes / 60}h {minutes % 60}m";
}
