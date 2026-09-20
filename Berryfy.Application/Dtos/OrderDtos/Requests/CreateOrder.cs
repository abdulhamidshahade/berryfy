namespace Berryfy.Application.Dtos.OrderDtos.Requests
{
    public class CreateOrder
    {
        public int UserId { get; set; }
        public int CartId { get; set; }
        public string CustomerEmail { get; set; }
        public string? CustomerPhone { get; set; }
        public string? ShippingName { get; set; }
        public string? ShippingAddressLine1 { get; set; }
        public string? ShippingAddressLine2 { get; set; }
        public string? ShippingCity { get; set; }
        public string? ShippingState { get; set; }
        public string? ShippingPostalCode { get; set; }
        public string? ShippingCountry { get; set; }
    }
}
