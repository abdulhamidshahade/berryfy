namespace Berryfy.Application.Dtos.OrderDtos.Requests
{
    public class MarkOrderPaidRequest
    {
        public int PaymentTransactionId { get; set; }
        public string PaymentProvider { get; set; }
    }

}
