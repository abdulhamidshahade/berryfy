namespace Berryfy.Domain.Constants;

public static class PricingPolicy
{
    public static decimal Discount(decimal subtotal, decimal discount) => Math.Clamp(discount, 0, Math.Max(0, subtotal));
    public static decimal Tax(decimal subtotal, decimal discount) =>
        Math.Round(Math.Max(0, subtotal - Discount(subtotal, discount)) * 0.1m, 2, MidpointRounding.AwayFromZero);
    public static decimal Shipping(decimal subtotal) => subtotal > 100 ? 0 : 10;
}
