using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Logging;
namespace OrderProcessing.Shared.Storage;
public abstract class RepositoryBase(JsonFileStore store)
{
    protected JsonFileStore Store {get;}=store;
    protected async Task QueryDelay(string text,CancellationToken ct){Log.Info($"{text} -> Opening connection...");await Task.Delay(200,ct);Log.Info($"{text} -> Executing query...");await Task.Delay(Random.Shared.Next(300,601),ct);}
    protected async Task WriteDelay(string text,CancellationToken ct){Log.Info($"{text} -> Opening connection...");await Task.Delay(200,ct);await Task.Delay(100,ct);await Task.Delay(400,ct);await Task.Delay(200,ct);}
}
public sealed class CustomerRepository(JsonFileStore s):RepositoryBase(s){public async Task<Customer?> GetByIdAsync(int id,CancellationToken ct){await QueryDelay(nameof(CustomerRepository),ct);return (await Store.ReadAsync<Customer>(DataFiles.Customers,ct)).FirstOrDefault(x=>x.Id==id);}}
public sealed class CouponRepository(JsonFileStore s):RepositoryBase(s){public async Task<Coupon?> GetByCodeAsync(string code,CancellationToken ct){await QueryDelay(nameof(CouponRepository),ct);return (await Store.ReadAsync<Coupon>(DataFiles.Coupons,ct)).FirstOrDefault(x=>x.Code.Equals(code,StringComparison.OrdinalIgnoreCase));}}
public sealed class ProductRepository(JsonFileStore s):RepositoryBase(s)
{
 public async Task<Product?> GetByIdAsync(int id,CancellationToken ct){await QueryDelay(nameof(ProductRepository),ct);return (await Store.ReadAsync<Product>(DataFiles.Products,ct)).FirstOrDefault(x=>x.Id==id);}
 public async Task<bool> ReserveStockAsync(int id,int qty,CancellationToken ct){await WriteDelay(nameof(ProductRepository),ct);bool ok=false;await Store.UpdateAsync<Product>(DataFiles.Products,list=>{var p=list.FirstOrDefault(x=>x.Id==id);if(p!=null&&p.StockQuantity>=qty){p.StockQuantity-=qty;ok=true;}return list;},ct);return ok;}
 public async Task ReleaseStockAsync(int id,int qty,CancellationToken ct){await WriteDelay(nameof(ProductRepository),ct);await Store.UpdateAsync<Product>(DataFiles.Products,list=>{var p=list.First(x=>x.Id==id);p.StockQuantity+=qty;return list;},ct);}
 public async Task<bool> ReserveStockUnsafeAsync(int id,int qty,CancellationToken ct){var list=await Store.ReadAsync<Product>(DataFiles.Products,ct);var p=list.First(x=>x.Id==id);if(p.StockQuantity<qty)return false;await WriteDelay("Unsafe reserve",ct);p.StockQuantity-=qty;await Store.WriteAsync(DataFiles.Products,list,ct);return true;}
}
public sealed class OrderRepository(JsonFileStore s,IEnumerable<int>? failSaveForOrderIds=null):RepositoryBase(s)
{
 readonly HashSet<int> _fail=(failSaveForOrderIds??[]).ToHashSet();
 public async Task SaveAsync(Order o,CancellationToken ct){await WriteDelay(nameof(OrderRepository),ct);if(_fail.Contains(o.Id)&&o.Status==OrderStatus.Completed)throw new Exceptions.DatabaseWriteException("Connection lost during commit");await Store.UpdateAsync<Order>(DataFiles.Orders,list=>{int i=list.FindIndex(x=>x.Id==o.Id);if(i<0)list.Add(o);else list[i]=o;return list;},ct);}
 public async Task<List<Order>> GetAllAsync(CancellationToken ct){await QueryDelay(nameof(OrderRepository),ct);return await Store.ReadAsync<Order>(DataFiles.Orders,ct);} public async Task<List<Order>> GetByStatusAsync(OrderStatus status,CancellationToken ct)=>(await GetAllAsync(ct)).Where(x=>x.Status==status).ToList();
}
public sealed class IncomingOrderRepository(JsonFileStore s):RepositoryBase(s){public async Task<List<Order>> GetAllAsync(CancellationToken ct){await QueryDelay(nameof(IncomingOrderRepository),ct);return await Store.ReadAsync<Order>(DataFiles.IncomingOrders,ct);}}
