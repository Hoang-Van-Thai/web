using Microsoft.AspNetCore.Mvc.Rendering;

namespace web.ViewModels;

public class ForecastViewModel
{
    public int ProductId { get; set; }

    public int StoreId { get; set; }

    public int Days { get; set; } = 7;

    public List<SelectListItem> Products { get; set; } = new();

    public List<SelectListItem> Stores { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public bool ApiAvailable { get; set; }

    public string? ActiveModel { get; set; }
}