using Berryfy.Domain.Constants;

namespace Berryfy.Application.Dtos.PaymentDtos.Requests
{
    public class UpdatePaymentStatus
    {
        public PaymentStatus Status { get; set; }
        public string? Notes { get; set; }
    }
}
