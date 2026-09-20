namespace Berryfy.Application.Dtos.CheckoutDtos.Requests
{
    public class SavePaymentBilling
    {
        public string PayerName { get; set; } = string.Empty;
        public string PayerEmail { get; set; } = string.Empty;
        public string BillingAddress1 { get; set; } = string.Empty;
        public string? BillingAddress2 { get; set; }
        public string BillingCity { get; set; } = string.Empty;
        public string BillingState { get; set; } = string.Empty;
        public string BillingPostalCode { get; set; } = string.Empty;
        public string BillingCountry { get; set; } = "US";
    }
}
