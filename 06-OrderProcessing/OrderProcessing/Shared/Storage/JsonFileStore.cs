using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using OrderProcessing.Shared.Exceptions;

namespace OrderProcessing.Shared.Storage;

public static class DataFiles
{
    public const string Customers="customers.json", Products="products.json", Coupons="coupons.json", IncomingOrders="incoming_orders.json", Orders="orders.json", Payments="payments.json", Emails="emails.json", EmailRetry="email_retry.json";
    public static readonly string Root = Path.Combine(AppContext.BaseDirectory, "Data");
    public static string PathFor(string file) => Path.Combine(Root, file);
}

public sealed class JsonFileStore
{
    static readonly Dictionary<string, SemaphoreSlim> Gates = new(StringComparer.OrdinalIgnoreCase);
    static readonly object GateSync = new();
    public static readonly JsonSerializerOptions Options = new() { WriteIndented=true, PropertyNamingPolicy=JsonNamingPolicy.CamelCase, Converters={new JsonStringEnumConverter()} };
    static SemaphoreSlim Gate(string file) { lock(GateSync) return Gates.TryGetValue(file,out var g)?g:(Gates[file]=new(1,1)); }

    public async Task<List<T>> ReadAsync<T>(string file, CancellationToken ct)
    {
        var gate=Gate(file); await gate.WaitAsync(ct);
        try { return await ReadUnlockedAsync<T>(file,ct); } finally { gate.Release(); }
    }
    async Task<List<T>> ReadUnlockedAsync<T>(string file,CancellationToken ct)
    {
        string path=DataFiles.PathFor(file); if(!File.Exists(path)) return [];
        try { await using var s=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read,4096,true); return await JsonSerializer.DeserializeAsync<List<T>>(s,Options,ct)??[]; }
        catch(JsonException ex){ throw new DataFileCorruptedException($"Invalid JSON in {file}",ex); }
    }
    public async Task WriteAsync<T>(string file,IEnumerable<T> values,CancellationToken ct)
    {
        var gate=Gate(file); await gate.WaitAsync(ct);
        try { await WriteUnlockedAsync(file,values,ct); } finally { gate.Release(); }
    }
    public async Task UpdateAsync<T>(string file,Func<List<T>,List<T>> update,CancellationToken ct)
    {
        var gate=Gate(file); await gate.WaitAsync(ct);
        try { var items=await ReadUnlockedAsync<T>(file,ct); await WriteUnlockedAsync(file,update(items),ct); } finally { gate.Release(); }
    }
    async Task WriteUnlockedAsync<T>(string file,IEnumerable<T> values,CancellationToken ct)
    {
        Directory.CreateDirectory(DataFiles.Root); string path=DataFiles.PathFor(file), temp=path+".tmp";
        await using(var s=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None,4096,true)) await JsonSerializer.SerializeAsync(s,values,Options,ct);
        File.Move(temp,path,true);
    }
    public List<T> Read<T>(string file){ string p=DataFiles.PathFor(file); if(!File.Exists(p))return[]; try{return JsonSerializer.Deserialize<List<T>>(File.ReadAllText(p),Options)??[];}catch(JsonException ex){throw new DataFileCorruptedException($"Invalid JSON in {file}",ex);} }
    public void Write<T>(string file,IEnumerable<T> values){Directory.CreateDirectory(DataFiles.Root);string p=DataFiles.PathFor(file),t=p+".tmp";File.WriteAllText(t,JsonSerializer.Serialize(values,Options),Encoding.UTF8);File.Move(t,p,true);}
}
