using Microsoft.ML.Data;

namespace Hackathon.Insight.Web.Models;

public class Forecast
{
    [VectorType]
    public float[] Values { get; set; }
}
