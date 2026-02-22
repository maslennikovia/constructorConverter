namespace constructor.converter.step.Models;

public record ConversionProgress
{
    public double Percentage { get; set; }
    public string Stage { get; set; }
    public string CurrentOperation { get; set; }
}