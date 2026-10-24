namespace Whispbot.Tools.Bot;

internal sealed class GatewayRecoveryPolicy(TimeSpan recoveryTimeout, TimeSpan startupTimeout)
{
    private readonly Dictionary<int, TimeSpan> _unhealthySince = [];
    private readonly HashSet<int> _connected = [];

    public bool NeedsRestart(int shardId, bool connected, TimeSpan elapsed, bool shuttingDown)
    {
        if (shuttingDown) return false;

        if (connected)
        {
            _connected.Add(shardId);
            _unhealthySince.Remove(shardId);
            return false;
        }

        if (!_unhealthySince.TryGetValue(shardId, out var since))
        {
            since = elapsed;
            _unhealthySince[shardId] = since;
        }

        var timeout = _connected.Contains(shardId) ? recoveryTimeout : startupTimeout;
        return elapsed - since >= timeout;
    }
}
