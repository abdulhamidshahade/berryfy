namespace Berryfy.Application.Dtos.ProductDtos.Responses
{
    public class ProductFilter
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public string? SearchTerm { get; set; }
        public string? Category { get; set; }
        public string? SortBy { get; set; } = "name";
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool? IsActive { get; set; } = true;
        public bool IncludeInactive { get; set; } = false;
    }
}