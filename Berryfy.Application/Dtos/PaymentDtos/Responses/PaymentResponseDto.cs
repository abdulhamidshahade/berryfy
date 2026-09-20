using Berryfy.Domain.Constants;

namespace Berryfy.Application.Dtos.PaymentDtos.Responses
{
    public class PaymentResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public PaymentResponse? Payment { get; set; }
        public string? TransactionId { get; set; }
        public PaymentStatus Status { get; set; }
        public string? RedirectUrl { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }
}
