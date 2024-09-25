using Hackathon.Insight.Web.Models;
using Hackathon.Insight.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ML;
using Newtonsoft.Json;
using System.Diagnostics;

namespace Hackathon.Insight.Web.Controllers;
public class HomeController : Controller
{
    private const int numberOfMonthsToForecast = 1;

    public IActionResult Index()
    {
        var mlContext = new MLContext();

        var csvDataList = new List<Data>();

        // Load data from CSV
        using (var reader = new StreamReader(Path.Combine(Environment.CurrentDirectory, "DataSource.csv")))
        {
            // Skip header
            reader.ReadLine();

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line!.Split(',');

                if (DateTime.TryParse(values[0], out var date)
                    && float.TryParse(new string(values[1].Where(char.IsDigit).ToArray()), out var numberOfTickets))
                {
                    csvDataList.Add(new Data { Date = date, NumberOfTickets = numberOfTickets });
                }
            }
        }

        var dataView = mlContext.Data.LoadFromEnumerable(csvDataList);

        // Create the time series forecasting pipeline
        var forecastingEstimator = mlContext.Forecasting.ForecastBySsa(
            outputColumnName: nameof(Forecast.Values),
            inputColumnName: nameof(Data.NumberOfTickets),
            windowSize: 6, // Use the last 6 months of data
            seriesLength: csvDataList.Count,
            trainSize: csvDataList.Count,
            horizon: numberOfMonthsToForecast);

        // Fit the model
        var forecastingTransformer = forecastingEstimator.Fit(dataView);

        // Create a new IDataView for future predictions
        var futureDates = new List<Data>();

        for (int i = 1; i <= numberOfMonthsToForecast; i++)
        {
            futureDates.Add(new Data()
            {
                Date = DateTime.Now.AddMonths(i)
            });
        }

        var futureDataView = mlContext.Data.LoadFromEnumerable(futureDates);

        // Make predictions using the model
        var forecastedResult = forecastingTransformer.Transform(futureDataView);

        // Get forecasted values
        var forecastedValues = mlContext.Data.CreateEnumerable<Forecast>(forecastedResult, reuseRowObject: false).ToList();

        // Get historical data
        var historicalDataList = csvDataList.Select(
            d => new HistoricalData()
            {
                Date = d.Date,
                NumberOfTickets = d.NumberOfTickets
            }
        ).ToList();

        GroupHistoricalDataByYearAndQuarter(historicalDataList);

        var model = new ForecastViewModel()
        {
            // ForecastValue = forecastedValues[0].Values[0]
            ForecastValue = 290 // Hard code forecasted value
        };

        // Set forecast message to type
        ViewBag.ForecastMessage = LoremIpsum();

        return View(model);
    }

    private void GroupHistoricalDataByYearAndQuarter(List<HistoricalData> list)
    {
        var result = list
            .GroupBy(historicalData => new
            {
                historicalData.Date.Year,
                Quarter = (historicalData.Date.Month - 1) / 3 + 1
            })
            .Select(group => new
            {
                group.Key.Year,
                group.Key.Quarter,
                Months = group.GroupBy(d => d.Date.Month)
                              .Select(mGroup => new
                              {
                                  Month = mGroup.Key,
                                  TotalTickets = mGroup.Sum(d => d.NumberOfTickets)
                              })
                              .ToList()
            })
            .ToList();

        // Serialize to JSON for ChartJS
        ViewBag.HistoricalData = JsonConvert.SerializeObject(result);

        // Displaying the grouped data in the console (to check results)
        /*foreach (var quarterGroup in result)
        {
            Console.WriteLine($"Year: {quarterGroup.Year}, Quarter: {quarterGroup.Quarter}");
            foreach (var monthGroup in quarterGroup.Months)
            {
                Console.WriteLine($"\tMonth: {monthGroup.Month}, Total Tickets: {monthGroup.TotalTickets}");
            }
        }*/
    }

    private string LoremIpsum()
    {
        return @"Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's 
standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book. 
It has survived not only five centuries, but also the leap into electronic typesetting, remaining essentially unchanged. 
It was popularised in the 1960s with the release of Letraset sheets containing Lorem Ipsum passages, and more recently with 
desktop publishing software like Aldus PageMaker including versions of Lorem Ipsum.";
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
