namespace Berryfy.Application.Dtos.InventoryDtos.Requests
{
    public class AddStockRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public string Notes { get; set; }
        public int? PerformedByUserId { get; set; }
    }
}
