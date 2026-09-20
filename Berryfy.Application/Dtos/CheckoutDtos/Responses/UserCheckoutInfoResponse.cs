namespace Berryfy.Application.Dtos.CheckoutDtos.Responses
{
    public class UserCheckoutInfoResponse
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string? SessionId { get; set; }
        
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string? Phone { get; set; }
        public string Address { get; set; }
        public string? Address2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string ZipCode { get; set; } = string.Empty;
        public string Country { get; set; } = "US";
        
        public string? PayerName { get; set; }
        public string? PayerEmail { get; set; }
        public string? BillingAddress1 { get; set; }
        public string? BillingAddress2 { get; set; }
        public string? BillingCity { get; set; }
        public string? BillingState { get; set; }
        public string? BillingPostalCode { get; set; }
        public string? BillingCountry { get; set; }
        
        public DateTime? LastUsedAt { get; set; }
    }
}
