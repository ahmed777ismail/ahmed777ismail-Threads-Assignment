using OrderProcessing.Shared.Models;using OrderProcessing.Shared.Storage;using OrderProcessing.Shared.Exceptions;using OrderProcessing.Phase2;
namespace OrderProcessing.Phase3;
public sealed class OrderProcessor(CustomerService customers,InventoryService inventory,OrderPricer pricer,IPaymentGateway payments,OrderRepository orders,IEmailService emails,JsonFileStore store)
{
 public async Task<bool> ProcessOrderAsync(Order o,CancellationToken ct)
 {
  Customer? c=null;bool reserved=false;string? tx=null;
  try
  {
   c=await customers.GetCustomerAsync(o.CustomerId,ct);if(!c.IsActive)return await Stop(o,OrderStatus.Rejected,"Customer is inactive",ct);
   if(!await inventory.ReserveStockAsync(o,ct)){var item=await Missing(o,ct);return await Stop(o,OrderStatus.Rejected,$"Insufficient stock for '{item}'",ct);}reserved=true;
   try{await pricer.PriceOrderAsync(o,ct);}catch(InvalidCouponException ex){await inventory.ReleaseStockAsync(o,CancellationToken.None);reserved=false;return await Stop(o,OrderStatus.Rejected,ex.Message,ct);}
   PaymentResult pay;using(var timeout=CancellationTokenSource.CreateLinkedTokenSource(ct)){timeout.CancelAfter(3000);try{pay=await payments.ProcessPaymentAsync(o.Id,o.TotalAmount,timeout.Token);}catch(OperationCanceledException) when(!ct.IsCancellationRequested){await inventory.ReleaseStockAsync(o,CancellationToken.None);reserved=false;return await Stop(o,OrderStatus.PaymentFailed,"Payment gateway timeout (3s)",ct);}}
   if(!pay.Success){await inventory.ReleaseStockAsync(o,CancellationToken.None);reserved=false;return await Stop(o,OrderStatus.PaymentFailed,pay.Message!,ct);}tx=pay.TransactionId;o.TransactionId=tx;o.Status=OrderStatus.Completed;
   try{await orders.SaveAsync(o,ct);}catch(DatabaseWriteException){await payments.RefundAsync(tx!,CancellationToken.None);await inventory.ReleaseStockAsync(o,CancellationToken.None);reserved=false;o.Status=OrderStatus.Failed;o.FailureReason=$"Database save failed after payment; payment {tx} refunded";await store.UpdateAsync<Order>(DataFiles.Orders,l=>{l.RemoveAll(x=>x.Id==o.Id);l.Add(o);return l;},CancellationToken.None);return false;}
   await SendWithRetry(o,c,ct);return true;
  }
  catch(CustomerNotFoundException ex){return await Stop(o,OrderStatus.Rejected,ex.Message,ct);}
  catch(OperationCanceledException){if(tx is not null)await payments.RefundAsync(tx,CancellationToken.None);if(reserved)await inventory.ReleaseStockAsync(o,CancellationToken.None);o.Status=OrderStatus.Pending;o.FailureReason="Cancelled";await orders.SaveAsync(o,CancellationToken.None);throw;}
 }
 async Task<string> Missing(Order o,CancellationToken ct){foreach(var i in o.Items){var p=(await store.ReadAsync<Product>(DataFiles.Products,ct)).First(x=>x.Id==i.ProductId);if(p.StockQuantity<i.Quantity)return p.Name;}return "item";}
 async Task<bool> Stop(Order o,OrderStatus s,string reason,CancellationToken ct){o.Status=s;o.FailureReason=reason;await orders.SaveAsync(o,ct);return false;}
 async Task SendWithRetry(Order o,Customer c,CancellationToken ct){for(int a=1;a<=4;a++)try{await emails.SendConfirmationAsync(o,c,ct);return;}catch(SmtpUnavailableException ex){if(a==4){await store.UpdateAsync<EmailMessage>(DataFiles.EmailRetry,l=>{l.Add(new(){Id=Guid.NewGuid(),OrderId=o.Id,To=c.Email,Subject=$"Order #{o.Id}",Body="Thank you",SentAt=DateTime.Now,Attempts=4,LastError=ex.Message});return l;},ct);return;}await Task.Delay(a==1?500:a==2?1000:2000,ct);}}
}
