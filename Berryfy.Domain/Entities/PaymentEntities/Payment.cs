using System.Text.Json.Serialization;
using Berryfy.Domain.Constants;
using Berryfy.Domain.Entities.AuthEntities;
using Berryfy.Domain.Entities.OrderEntities;
using System.ComponentModel.DataAnnotations;

namespace Berryfy.Domain.Entities.PaymentEntities
{
    public class Payment
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public int? OrderId { get; set; }

        [Required]
        public string TransactionId { get; set; } = string.Empty;

        [Required]
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        [Required]
        public PaymentMethod Method { get; set; }

        [Required]
        public string Provider { get; set; } = string.Empty; 

        [Required]
        public decimal Amount { get; set; }

        [Required]
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

        [JsonIgnore] public User? User { get; set; }
        [JsonIgnore] public Order? Order { get; set; }
    }
}
