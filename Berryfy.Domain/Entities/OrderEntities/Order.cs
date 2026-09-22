using System.Text.Json.Serialization;
using Berryfy.Domain.Constants;
using Berryfy.Domain.Entities.AuthEntities;
using Berryfy.Domain.Entities.Base;
using Berryfy.Domain.Entities.PaymentEntities;
using Berryfy.Domain.Entities.ShoppingCartEntities;
namespace Berryfy.Domain.Entities.OrderEntities
{
    public class Order : IAuditableEntity
    {

        public int Id { get; set; }
        public int UserId { get; set; }
        public int CartId { get; set; }
        public string? sessionId { get; set; }
        public bool isPaid { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal ShippingAmount { get; set; }
        public decimal Total { get; set; }

        public decimal DiscountTotal { get; set; }

        public string CustomerEmail { get; set; }
        public string? CustomerPhone { get; set; }


        public string? ReferenceNumber { get; set; }

        public DateTime? CompletedAt { get; set; }
        public DateTime? CancalledAt { get; set; }

        public string? ShippingName { get; set; }
        public string? ShippingAddress1 { get; set; }
        public string? ShippingAddress2 { get; set; }

        public string? ShippingCity { get; set; }

        public string? ShippingState { get; set; }

        public string? ShippingPostalCode { get; set; }

        public string? ShippingCountry { get; set; }


        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }



        [JsonIgnore] public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        [JsonIgnore] public User User { get; set; }
        [JsonIgnore] public Cart Cart { get; set; }

        [JsonIgnore] public List<Payment> Payments { get; set; }

        
    }
}
