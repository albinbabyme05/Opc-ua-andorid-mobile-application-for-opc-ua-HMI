using Microcharts;
using SkiaSharp;

namespace CMLGapp.Views;

public partial class ProdDefectContentpage : ContentPage
{
	public ProdDefectContentpage()
	{
		InitializeComponent();
	}

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadAndDisplayChart();
    }


    private async void LoadAndDisplayChart()
    {
        string filePath = Path.Combine(FileSystem.AppDataDirectory, "CMLGData", "databank.txt");

        if (!File.Exists(filePath))
            return;

        var AllLines = await File.ReadAllLinesAsync(filePath);
        var entries = new List<ChartEntry>();

        foreach (var line in AllLines)
        {
            if(string.IsNullOrWhiteSpace(line))
            continue;

            var parts = line.Split(',');
            if (parts.Length < 4) continue;


            var time = parts[1].Trim();
            var accCountString = parts[3].Trim();

            if (float.TryParse(accCountString, out float accCount))
            {
                entries.Add(new ChartEntry(accCount)
                {
                    Label = time,
                    ValueLabel = accCount.ToString(),
                    Color = SKColor.Parse("#2c3e50")
                });
            }
        }
        barChart.Chart = new BarChart
        {
            Entries = entries,
            LabelTextSize = 28,
            //BackgroundColor = SKColor.Parse("#000000"),
            LabelColor = SKColor.Parse("#FFFFFF"),
            
        };
           
    }
}