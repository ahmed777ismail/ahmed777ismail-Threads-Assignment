using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Logging;
namespace OrderProcessing.Shared.Storage;
public static class DataSeeder
{
    public static async Task ResetAsync(CancellationToken ct)
    {
        if(Directory.Exists(DataFiles.Root)) Directory.Delete(DataFiles.Root,true);
        Directory.CreateDirectory(DataFiles.Root); var s=new JsonFileStore();
        var customers=new[]{C(1,"Ahmed Ali",true,LoyaltyTier.Gold),C(2,"Sara Mohamed",true,LoyaltyTier.Silver),C(3,"Omar Hassan",true,LoyaltyTier.Bronze),C(4,"Mona Ibrahim",false,LoyaltyTier.Silver),C(5,"Youssef Adel",true,LoyaltyTier.Gold),C(6,"Nour Khaled",true,LoyaltyTier.Bronze),C(7,"Karim Samir",true,LoyaltyTier.Silver),C(8,"Laila Mahmoud",true,LoyaltyTier.Gold),C(9,"Hassan Tarek",true,LoyaltyTier.Bronze),C(10,"Dina Fathy",true,LoyaltyTier.Silver)};
        var products=new[]{P(1,"Laptop",1200,10,2.2m),P(2,"Smartphone",800,15,.4m),P(3,"Wireless Mouse",25,2,.1m),P(4,"Mechanical Keyboard",90,1,.9m),P(5,"USB-C Hub",45,30,.2m),P(6,"Headphones",150,12,.3m),P(7,"27\" Monitor",350,6,5),P(8,"Webcam",70,20,.2m)};
        var coupons=new[]{new Coupon{Code="SAVE10",DiscountPercent=10,ExpiryDate=DateTime.Today.AddDays(180)},new Coupon{Code="WELCOME15",DiscountPercent=15,ExpiryDate=DateTime.Today.AddDays(90)},new Coupon{Code="FLASH20",DiscountPercent=20,ExpiryDate=DateTime.Today.AddDays(7)},new Coupon{Code="OLD20",DiscountPercent=20,ExpiryDate=DateTime.Today.AddDays(-30)}};
        var specs=new(int,int,(int,int)[],string?)[]{(1,1,[(1,1),(3,1)],"SAVE10"),(2,2,[(2,1)],null),(3,3,[(5,2),(8,1)],"WELCOME15"),(4,4,[(6,1)],null),(5,5,[(7,1)],null),(6,6,[(5,1)],"FLASH20"),(7,7,[(6,1),(8,1)],null),(8,8,[(1,1)],"SAVE10"),(9,9,[(3,5)],null),(10,10,[(4,1)],null),(11,1,[(4,1)],null),(12,2,[(2,1)],"OLD20"),(13,3,[(6,2)],null),(14,5,[(8,2),(5,1)],"WELCOME15"),(15,99,[(2,1)],null),(16,6,[(7,1),(5,1)],null),(17,7,[(2,1)],"SAVE10"),(18,8,[(6,1)],"FAKE50"),(19,9,[(8,1)],null),(20,10,[(1,1),(6,1)],"FLASH20")};
        var orders=specs.Select(x=>new Order{Id=x.Item1,CustomerId=x.Item2,Items=x.Item3.Select(i=>new OrderItem{ProductId=i.Item1,Quantity=i.Item2}).ToList(),CouponCode=x.Item4,Status=OrderStatus.Pending,CreatedAt=DateTime.Now}).ToList();
        await Put(s,DataFiles.Customers,customers,ct); await Put(s,DataFiles.Products,products,ct); await Put(s,DataFiles.Coupons,coupons,ct); await Put(s,DataFiles.IncomingOrders,orders,ct);
        await Put(s,DataFiles.Orders,Array.Empty<Order>(),ct); await Put(s,DataFiles.Payments,Array.Empty<PaymentTransaction>(),ct); await Put(s,DataFiles.Emails,Array.Empty<EmailMessage>(),ct); await Put(s,DataFiles.EmailRetry,Array.Empty<EmailMessage>(),ct);
        Directory.CreateDirectory(Path.Combine(DataFiles.Root,"invoices")); Directory.CreateDirectory(Path.Combine(DataFiles.Root,"reports"));
    }
    static Customer C(int id,string n,bool a,LoyaltyTier t)=>new(){Id=id,Name=n,Email=n.ToLower().Replace(' ','.')+"@example.com",IsActive=a,LoyaltyTier=t};
    static Product P(int id,string n,decimal p,int q,decimal w)=>new(){Id=id,Name=n,Price=p,StockQuantity=q,WeightKg=w};
    static async Task Put<T>(JsonFileStore s,string f,IEnumerable<T> v,CancellationToken ct){var list=v.ToList();await s.WriteAsync(f,list,ct);Log.Info($"[Seeder] {f} -> {list.Count} records");}
}
