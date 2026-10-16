using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using OrderProcessing.Shared.Exceptions;
namespace OrderProcessing.Phase2;
public sealed class PricingService(ProductRepository products,CustomerRepository customers,CouponRepository coupons,IEnumerable<int>? failShippingForOrderIds=null)
{
 readonly HashSet<int> _fail=(failShippingForOrderIds??[]).ToHashSet();
 public async Task<decimal> CalculateSubTotalAsync(Order o,CancellationToken ct){decimal x=0;foreach(var i in o.Items){var p=await products.GetByIdAsync(i.ProductId,ct)??throw new InvalidOperationException();i.UnitPrice=p.Price;x+=p.Price*i.Quantity;}return Math.Round(x,2,MidpointRounding.AwayFromZero);}
 public async Task<decimal> GetDiscountAsync(Order o,CancellationToken ct){await Task.Delay(150,ct);var c=await customers.GetByIdAsync(o.CustomerId,ct)??throw new CustomerNotFoundException($"Customer #{o.CustomerId} not found");int pc=0;if(o.CouponCode is not null){var q=await coupons.GetByCodeAsync(o.CouponCode,ct)??throw new InvalidCouponException($"Coupon '{o.CouponCode}' does not exist");if(q.ExpiryDate<DateTime.Today)throw new InvalidCouponException($"Coupon '{o.CouponCode}' has expired");pc=q.DiscountPercent;}await Task.Delay(Random.Shared.Next(500,1501),ct);int loyalty=c.LoyaltyTier==LoyaltyTier.Gold?10:c.LoyaltyTier==LoyaltyTier.Silver?5:0;return Math.Round(o.SubTotal*Math.Min(30,loyalty+pc)/100m,2,MidpointRounding.AwayFromZero);}
 public async Task<decimal> CalculateShippingCostAsync(Order o,CancellationToken ct){await Task.Delay(150,ct);if(_fail.Contains(o.Id))throw new ShippingProviderUnavailableException("Shipping provider unavailable");await Task.Delay(300,ct);decimal w=0;foreach(var i in o.Items)w+=(await products.GetByIdAsync(i.ProductId,ct))!.WeightKg*i.Quantity;await Task.Delay(Random.Shared.Next(500,1501),ct);return o.SubTotal>=1000?0:Math.Round(5+1.5m*o.Items.Sum(x=>x.Quantity)+2*w,2,MidpointRounding.AwayFromZero);}
}
public sealed class OrderPricer(PricingService service)
{
 public async Task PriceOrderAsync(Order o,CancellationToken ct){o.SubTotal=await service.CalculateSubTotalAsync(o,ct);var d=service.GetDiscountAsync(o,ct);var s=service.CalculateShippingCostAsync(o,ct);o.Discount=await d;o.ShippingCost=await s;o.TotalAmount=Math.Round(o.SubTotal-o.Discount+o.ShippingCost,2,MidpointRounding.AwayFromZero);}
 public async Task PriceOrderSequentialAsync(Order o,CancellationToken ct){o.SubTotal=await service.CalculateSubTotalAsync(o,ct);o.Discount=await service.GetDiscountAsync(o,ct);o.ShippingCost=await service.CalculateShippingCostAsync(o,ct);o.TotalAmount=Math.Round(o.SubTotal-o.Discount+o.ShippingCost,2);}
}
public sealed class InvoiceGenerator
{
 public async Task GenerateAsync(Order o,CancellationToken ct){long x=0;await Task.Run(()=>{for(int i=0;i<20_000_000;i++){ct.ThrowIfCancellationRequested();x=(x+i)%1_000_003;}},ct);string path=Path.Combine(DataFiles.Root,"invoices",$"invoice_{o.Id}.txt");await File.WriteAllTextAsync(path,$"Order #{o.Id}\nTotal: {o.TotalAmount:F2}\nRender: {x}",ct);}
}
