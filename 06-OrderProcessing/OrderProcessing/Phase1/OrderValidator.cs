using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
namespace OrderProcessing.Phase1;
public sealed class OrderValidator(CustomerRepository customers,ProductRepository products,CouponRepository coupons,OrderRepository orders)
{
 public async Task ValidateOrderAsync(Order o,CancellationToken ct)
 {
  string? reason=null;var c=await customers.GetByIdAsync(o.CustomerId,ct);if(c is null)reason=$"Customer #{o.CustomerId} not found";else if(!c.IsActive)reason="Customer is inactive";
  if(reason is null)foreach(var i in o.Items){var p=await products.GetByIdAsync(i.ProductId,ct);if(p is null){reason=$"Product #{i.ProductId} not found";break;}if(p.StockQuantity<i.Quantity){reason=$"Insufficient stock for '{p.Name}'";break;}}
  if(reason is null&&o.CouponCode is not null){var cp=await coupons.GetByCodeAsync(o.CouponCode,ct);if(cp is null)reason=$"Coupon '{o.CouponCode}' does not exist";else if(cp.ExpiryDate<DateTime.Today)reason=$"Coupon '{o.CouponCode}' has expired";}
  o.Status=reason is null?OrderStatus.Validated:OrderStatus.Rejected;o.FailureReason=reason;await orders.SaveAsync(o,ct);
 }
}
