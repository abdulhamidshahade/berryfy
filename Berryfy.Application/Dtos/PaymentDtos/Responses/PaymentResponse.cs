using Berryfy.Domain.Constants;
using Berryfy.Domain.Entities.PaymentEntities;

namespace Berryfy.Application.Dtos.PaymentDtos.Responses
{
    public class PaymentResponse
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public int? OrderId { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public PaymentStatus Status { get; set; }
        public PaymentMethod Method { get; set; }
        public string Provider { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public string? ProviderTransactionId { get; set; }
        public string? ProviderPaymentMethodId { get; set; }
        public string? CardLast4 { get; set; }
        public string? CardBrand { get; set; }
        public string? PayerEmail { get; set; }
        public string? PayerName { get; set; }
        public string? BillingAddress1 { get; set; }
        public string? BillingAddress2 { get; set; }
        public string? BillingCity { get; set; }
        public string? BillingState { get; set; }
        public string? BillingPostalCode { get; set; }
        public string? BillingCountry { get; set; }
        public decimal ProcessingFee { get; set; }
        public decimal NetAmount { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? FailedAt { get; set; }
        public DateTime? RefundedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public string? FailureReason { get; set; }
        public string? Metadata { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public string? UserName { get; set; }
        public string? UserEmail { get; set; }
        public string? OrderReferenceNumber { get; set; }

        public static PaymentResponse MapFromPayment(Payment payment)
        {
            return new PaymentResponse
            {
                Id = payment.Id,
                UserId = payment.UserId,
                OrderId = payment.OrderId,
                TransactionId = payment.TransactionId,
                Status = payment.Status,
                Method = payment.Method,
                Provider = payment.Provider,
                Amount = payment.Amount,
                Currency = payment.Currency,
                ProviderTransactionId = payment.ProviderTransactionId,
                ProviderPaymentMethodId = payment.ProviderPaymentMethodId,
                CardLast4 = payment.CardLast4,
                CardBrand = payment.CardBrand,
                PayerEmail = payment.PayerEmail,
                PayerName = payment.PayerName,
                BillingAddress1 = payment.BillingAddress1,
                BillingAddress2 = payment.BillingAddress2,
                BillingCity = payment.BillingCity,
                BillingState = payment.BillingState,
                BillingPostalCode = payment.BillingPostalCode,
                BillingCountry = payment.BillingCountry,
                ProcessingFee = payment.ProcessingFee,
                NetAmount = payment.NetAmount,
                ProcessedAt = payment.ProcessedAt,
                CompletedAt = payment.CompletedAt,
                FailedAt = payment.FailedAt,
                RefundedAt = payment.RefundedAt,
                ErrorMessage = payment.ErrorMessage,
                FailureReason = payment.FailureReason,
                Metadata = payment.Metadata,
                Notes = payment.Notes
            };
        }

        public static List<PaymentResponse> MapFromPayment(IEnumerable<Payment> payments)
        {
            List<PaymentResponse> paymentResponses = new List<PaymentResponse>();

            foreach(Payment payment in payments)
            {
                paymentResponses.Add(MapFromPayment(payment));
            }

            return paymentResponses;
        }
    }
}
