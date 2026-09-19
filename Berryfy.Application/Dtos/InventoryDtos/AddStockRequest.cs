namespace Berryfy.Application.Dtos.InventoryDtos
{
    public class AddStockRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public string Notes { get; set; } = string.Empty;
        public int? PerformedByUserId { get; set; }
    }
}
