namespace Berryfy.Application.Dtos.PaymentDtos.Requests
{
    public class RefundPayment
    {
        public decimal? RefundAmount { get; set; }
        public string? Reason { get; set; }
    }
}
