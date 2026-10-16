using System.Diagnostics;using OrderProcessing.Shared.Models;using OrderProcessing.Shared.Storage;using OrderProcessing.Shared.Exceptions;
namespace OrderProcessing.Phase4;
public sealed class CustomerDashboardService(JsonFileStore store,CustomerRepository customers,IEnumerable<int>? failPaymentsForCustomerIds=null)
{
 readonly HashSet<int> _fails=(failPaymentsForCustomerIds??[]).ToHashSet();
 async Task<T> Load<T>(Func<Task<T>> read,CancellationToken ct){await Task.Delay(200,ct);await Task.Delay(Random.Shared.Next(300,601),ct);return await read();}
 public async Task<CustomerDashboard> LoadAsync(int id,CancellationToken ct)
 {
  var profile=Load(async()=>await customers.GetByIdAsync(id,ct)??throw new CustomerNotFoundException($"Customer #{id} not found"),ct);
  var order=Load(async()=> (await store.ReadAsync<Order>(DataFiles.Orders,ct)).Where(x=>x.CustomerId==id).ToList(),ct);
  var payment=Load(async()=>{if(_fails.Contains(id))throw new TimeoutException("Payments store timeout");var ids=(await store.ReadAsync<Order>(DataFiles.Orders,ct)).Where(x=>x.CustomerId==id).Select(x=>x.Id).ToHashSet();return (await store.ReadAsync<PaymentTransaction>(DataFiles.Payments,ct)).Where(x=>ids.Contains(x.OrderId)).ToList();},ct);
  var email=Load(async()=>{var ids=(await store.ReadAsync<Order>(DataFiles.Orders,ct)).Where(x=>x.CustomerId==id).Select(x=>x.Id).ToHashSet();return (await store.ReadAsync<EmailMessage>(DataFiles.Emails,ct)).Where(x=>ids.Contains(x.OrderId)).ToList();},ct);
  Task all=Task.WhenAll(profile,order,payment,email);try{await all;}catch{throw new AggregateException(new Task[]{profile,order,payment,email}.Where(x=>x.IsFaulted).SelectMany(x=>x.Exception!.InnerExceptions));}
  return new(){Customer=profile.Result,Orders=order.Result,Payments=payment.Result,Emails=email.Result};
 }
}
public interface IShippingCarrier{string Name{get;}Task<decimal> GetQuoteAsync(Order order,CancellationToken ct);}
public sealed class ShippingCarrier(string name,int min,int max,decimal factor,IEnumerable<int>? failures=null):IShippingCarrier
{
 readonly HashSet<int> _fail=(failures??[]).ToHashSet();public string Name=>name;
 public async Task<decimal> GetQuoteAsync(Order o,CancellationToken ct){try{await Task.Delay(150,ct);await Task.Delay(Random.Shared.Next(min,max+1),ct);if(_fail.Contains(o.Id))throw new ShippingProviderUnavailableException($"{Name} unavailable");decimal basePrice=o.ShippingCost==0?25:o.ShippingCost;return Math.Round(basePrice*factor,2);}catch(OperationCanceledException){Console.WriteLine($"[{Name}] cancelled");throw;}}
}
public sealed class ShippingQuoteService(IEnumerable<IShippingCarrier> carriers)
{
 public async Task<ShippingQuote> GetFastestQuoteAsync(Order o,CancellationToken ct)
 {
  var sw=Stopwatch.StartNew();using var linked=CancellationTokenSource.CreateLinkedTokenSource(ct);linked.CancelAfter(1200);var map=carriers.ToDictionary(x=>x.GetQuoteAsync(o,linked.Token),x=>x);
  try{while(map.Count>0){var done=await Task.WhenAny(map.Keys);var carrier=map[done];map.Remove(done);try{decimal price=await done;linked.Cancel();return new(carrier.Name,price,sw.ElapsedMilliseconds);}catch(ShippingProviderUnavailableException){if(map.Count==0)throw new ShippingProviderUnavailableException("All carriers failed");}}}
  catch(OperationCanceledException) when(!ct.IsCancellationRequested){linked.Cancel();return new("Flat rate",25,sw.ElapsedMilliseconds);}throw new ShippingProviderUnavailableException("All carriers failed");
 }
}
