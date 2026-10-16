namespace MenuViews;

public sealed class MenuViewCounter
{
    int _views;
    public int Views => Volatile.Read(ref _views);

    public void RegisterView()
    {
        Interlocked.Increment(ref _views);
    }
}
