using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GameVault.Services;

public class PdfReportService
{
    public PdfReportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public string GenerateLibraryReport(ReportData data, string? outputFolder = null)
    {
        var reportsFolder = outputFolder ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "GameVault",
            "Reports");
        Directory.CreateDirectory(reportsFolder);

        var filePath = Path.Combine(
            reportsFolder,
            $"GameVault-Library-{DateTime.Now:yyyyMMdd-HHmmss}.pdf");

        Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(32);
                page.DefaultTextStyle(style => style.FontSize(9).FontColor(Colors.Grey.Darken3));

                page.Header().Column(column =>
                {
                    column.Item().Text("GAMEVAULT")
                        .FontSize(24)
                        .Bold()
                        .FontColor("#7C5CFC");
                    column.Item().Text("Personal Game Library Report")
                        .FontSize(13)
                        .SemiBold();
                    column.Item().Text($"Generated: {DateTime.Now:dd MMMM yyyy HH:mm}")
                        .FontColor(Colors.Grey.Darken1);
                });

                page.Content().PaddingVertical(20).Column(column =>
                {
                    column.Spacing(14);

                    column.Item().Element(container => ComposeProfile(container, data));
                    column.Item().Element(container => ComposeStatistics(container, data));
                    column.Item().Text("Game Library").FontSize(15).Bold();
                    column.Item().Element(container => ComposeGamesTable(container, data));
                    column.Item().Text("Achievements").FontSize(15).Bold();
                    column.Item().Element(container => ComposeAchievements(container, data));
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("GameVault • Page ");
                    text.CurrentPageNumber();
                    text.Span(" of ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf(filePath);

        return filePath;
    }

    private static void ComposeProfile(IContainer container, ReportData data)
    {
        container.Background("#F4F1FF").Padding(14).Column(column =>
        {
            column.Item().Text("User Profile").FontSize(14).Bold().FontColor("#5B3FD4");
            column.Item().PaddingTop(5).Text($"{data.Profile.DisplayName} (@{data.Profile.Username})");
            column.Item().Text($"Favorite game: {ValueOrFallback(data.Profile.FavoriteGame)}");
            column.Item().Text($"Bio: {ValueOrFallback(data.Profile.Bio)}");
        });
    }

    private static void ComposeStatistics(IContainer container, ReportData data)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.Spacing(8);
                row.RelativeItem().Element(item =>
                    StatisticCard(item, "Total games", data.TotalGames.ToString()));
                row.RelativeItem().Element(item =>
                    StatisticCard(item, "Total playtime", FormatMinutes(data.TotalPlayTimeMinutes)));
                row.RelativeItem().Element(item =>
                    StatisticCard(item, "Top genre", data.MostCommonGenre));
                row.RelativeItem().Element(item =>
                    StatisticCard(item, "Unlocked", data.UnlockedAchievementCount.ToString()));
            });

            column.Item().PaddingTop(8).Text(
                $"Newest added game: {data.NewestAddedGame?.Title ?? "No games"}");
        });
    }

    private static void StatisticCard(IContainer container, string title, string value)
    {
        container.Border(1)
            .BorderColor("#DED8F7")
            .Padding(9)
            .Column(column =>
            {
                column.Item().Text(title).FontSize(8).FontColor(Colors.Grey.Darken1);
                column.Item().Text(value).Bold().FontColor("#5B3FD4");
            });
    }

    private static void ComposeGamesTable(IContainer container, ReportData data)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2);
                columns.RelativeColumn(1.4f);
                columns.ConstantColumn(46);
                columns.RelativeColumn(1.5f);
                columns.ConstantColumn(65);
            });

            table.Header(header =>
            {
                HeaderCell(header.Cell(), "Title");
                HeaderCell(header.Cell(), "Genre");
                HeaderCell(header.Cell(), "Year");
                HeaderCell(header.Cell(), "Developer");
                HeaderCell(header.Cell(), "Playtime");
            });

            foreach (var game in data.Games)
            {
                BodyCell(table.Cell(), game.Title);
                BodyCell(table.Cell(), ValueOrFallback(game.Genre));
                BodyCell(table.Cell(), game.ReleaseYear.ToString());
                BodyCell(table.Cell(), ValueOrFallback(game.Developer));
                BodyCell(table.Cell(), FormatMinutes(game.PlayTimeMinutes));
            }
        });
    }

    private static void ComposeAchievements(IContainer container, ReportData data)
    {
        container.Column(column =>
        {
            column.Spacing(6);
            foreach (var achievement in data.Achievements)
            {
                column.Item()
                    .Border(1)
                    .BorderColor(achievement.IsUnlocked ? "#7C5CFC" : "#D9D9DF")
                    .Padding(9)
                    .Row(row =>
                    {
                        row.RelativeItem().Column(details =>
                        {
                            details.Item().Text(achievement.Title).SemiBold();
                            details.Item().Text(achievement.Description)
                                .FontSize(8)
                                .FontColor(Colors.Grey.Darken1);
                        });
                        row.AutoItem().AlignMiddle().Text(
                                achievement.IsUnlocked
                                    ? $"Unlocked {achievement.UnlockedAt:dd MMM yyyy}"
                                    : "Locked")
                            .FontSize(8)
                            .FontColor(achievement.IsUnlocked ? "#4C9B70" : Colors.Grey.Darken1);
                    });
            }
        });
    }

    private static void HeaderCell(IContainer container, string text)
    {
        container.Background("#7C5CFC").Padding(6)
            .Text(text).FontColor(Colors.White).SemiBold();
    }

    private static void BodyCell(IContainer container, string text)
    {
        container.BorderBottom(1).BorderColor("#E5E5EA").Padding(6).Text(text);
    }

    private static string FormatMinutes(int minutes)
    {
        var hours = minutes / 60;
        var remainingMinutes = minutes % 60;
        return $"{hours}h {remainingMinutes}m";
    }

    private static string ValueOrFallback(string value) =>
        string.IsNullOrWhiteSpace(value) ? "Not specified" : value;
}
