namespace STS2OrchisNecrobinderSkinFix.Diagnostics;

internal sealed class LogLimiter(string message, int limit = 3)
{
    private int _count;

    public void Info()
    {
        var count = Interlocked.Increment(ref _count);
        if (count <= limit) Main.Logger.Info($"{message} Count={count}");
    }

    public void Info(string detail)
    {
        var count = Interlocked.Increment(ref _count);
        if (count <= limit) Main.Logger.Info($"{message}: {detail} Count={count}");
    }
}