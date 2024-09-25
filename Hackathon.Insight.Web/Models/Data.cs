using Microsoft.ML.Data;

namespace Hackathon.Insight.Web.Models;

/// <summary>
/// The class representing the CSV source file
/// </summary>
public class Data
{
    [LoadColumn(0)]
    public DateTime Date { get; set; }

    [LoadColumn(1)]
    public float NumberOfTickets { get; set; }
}
