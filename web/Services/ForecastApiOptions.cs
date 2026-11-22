namespace web.Services;

public class ForecastApiOptions
{
    public string BaseUrl { get; set; }
        = "http://127.0.0.1:8000/";

    public int TimeoutSeconds { get; set; } = 120;
}