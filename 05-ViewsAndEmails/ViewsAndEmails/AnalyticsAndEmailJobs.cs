namespace ViewsAndEmails;

public sealed class AnalyticsAndEmailJobs
{
    public readonly System.Collections.Concurrent.ConcurrentDictionary<string, int> Views = new();
    public readonly System.Collections.Concurrent.ConcurrentQueue<string> EmailJobs = new();
    public readonly List<string> SentEmails = new();

    readonly object _sentSync = new();
    public volatile bool ProducersDone;

    public void RecordView(string product)
    {
        Views.AddOrUpdate(product, 1, (_, count) => count + 1);
    }

    public void PlaceOrder(int orderId)
    {
        EmailJobs.Enqueue($"Email: order {orderId}");
    }

    public void EmailWorker()
    {
        while (true)
        {
                string? job = null;
                EmailJobs.TryDequeue(out job);

                if (job is not null)
                {
                    lock (_sentSync)
                        SentEmails.Add(job);
                    continue;
                }

                if (ProducersDone && EmailJobs.Count == 0)
                    return;

                Thread.Sleep(1);
        }
    }
}
