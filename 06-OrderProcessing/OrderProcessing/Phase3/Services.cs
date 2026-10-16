using OrderProcessing.Shared.Models;using OrderProcessing.Shared.Storage;using OrderProcessing.Shared.Exceptions;
namespace OrderProcessing.Phase3;
public interface IPaymentGateway{Task<PaymentResult> ProcessPaymentAsync(int orderId,decimal amount,CancellationToken ct);Task RefundAsync(string transactionId,CancellationToken ct);}
public interface IEmailService{Task SendConfirmationAsync(Order order,Customer customer,CancellationToken ct);}
public sealed class CustomerService(CustomerRepository repo){public async Task<Customer> GetCustomerAsync(int id,CancellationToken ct)=>await repo.GetByIdAsync(id,ct)??throw new CustomerNotFoundException($"Customer #{id} not found");}
public sealed class InventoryService(ProductRepository products)
{
 public async Task<bool> ReserveStockAsync(Order o,CancellationToken ct){var done=new List<OrderItem>();foreach(var i in o.Items){if(!await products.ReserveStockAsync(i.ProductId,i.Quantity,ct)){foreach(var x in done)await products.ReleaseStockAsync(x.ProductId,x.Quantity,ct);return false;}done.Add(i);}return true;}
 public async Task ReleaseStockAsync(Order o,CancellationToken ct){foreach(var i in o.Items)await products.ReleaseStockAsync(i.ProductId,i.Quantity,ct);}
}
public sealed class PaymentGateway(JsonFileStore store,IEnumerable<int>? timeoutOrderIds=null,IEnumerable<int>? declineOrderIds=null):IPaymentGateway
{
 readonly HashSet<int> _timeouts=(timeoutOrderIds??[]).ToHashSet(),_declines=(declineOrderIds??[]).ToHashSet();
 public async Task<PaymentResult> ProcessPaymentAsync(int id,decimal amount,CancellationToken ct){await Task.Delay(800,ct);await Task.Delay(500,ct);if(_declines.Contains(id)){var r=new PaymentResult{Success=false,Status=PaymentStatus.Declined,Message="Card declined by issuer"};await Add(id,amount,r,ct);return r;}await Task.Delay(_timeouts.Contains(id)?5000:700,ct);var ok=new PaymentResult{Success=true,Status=PaymentStatus.Captured,TransactionId="TXN-"+Guid.NewGuid().ToString("N")[..12].ToUpper()};await Add(id,amount,ok,ct);return ok;}
 async Task Add(int id,decimal amount,PaymentResult r,CancellationToken ct)=>await store.UpdateAsync<PaymentTransaction>(DataFiles.Payments,l=>{l.Add(new(){OrderId=id,Amount=amount,Status=r.Status,TransactionId=r.TransactionId,CreatedAt=DateTime.Now});return l;},ct);
 public async Task RefundAsync(string tx,CancellationToken ct){await Task.Delay(950,ct);await store.UpdateAsync<PaymentTransaction>(DataFiles.Payments,l=>{var p=l.First(x=>x.TransactionId==tx);p.Status=PaymentStatus.Refunded;return l;},ct);}
}
public sealed class EmailService(JsonFileStore store,Dictionary<int,int>? failAttempts=null):IEmailService
{
 readonly Dictionary<int,int> _fail=failAttempts??[];readonly System.Collections.Concurrent.ConcurrentDictionary<int,int> _attempts=new();
 public async Task SendConfirmationAsync(Order o,Customer c,CancellationToken ct){int a=_attempts.AddOrUpdate(o.Id,1,(_,x)=>x+1);await Task.Delay(200,ct);await Task.Delay(300,ct);await Task.Delay(500,ct);if(a<=_fail.GetValueOrDefault(o.Id))throw new SmtpUnavailableException("SMTP provider unavailable");await store.UpdateAsync<EmailMessage>(DataFiles.Emails,l=>{if(l.All(x=>x.OrderId!=o.Id))l.Add(new(){Id=Guid.NewGuid(),OrderId=o.Id,To=c.Email,Subject=$"Order #{o.Id}",Body="Thank you",SentAt=DateTime.Now,Attempts=a});return l;},ct);}
}
