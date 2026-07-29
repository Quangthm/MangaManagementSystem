namespace MangaManagementSystem.Application.Features.Ranking.Warnings;

public sealed record DevelopmentTimeState(
    DateTime RealUtc,
    DateTime EffectiveUtc,
    bool IsFakeTimeActive,
    string Mode,
    double OffsetMinutes,
    DateTime? FixedUtc);

public sealed class DevelopmentTimeProvider : TimeProvider
{
    private readonly object _syncRoot = new();

    private DateTimeOffset? _fixedUtc;
    private TimeSpan _offset = TimeSpan.Zero;

    public override DateTimeOffset GetUtcNow()
    {
        lock (_syncRoot)
        {
            if (_fixedUtc.HasValue)
            {
                return _fixedUtc.Value;
            }

            return TimeProvider.System.GetUtcNow() + _offset;
        }
    }

    public DevelopmentTimeState GetState()
    {
        lock (_syncRoot)
        {
            var realUtc = TimeProvider.System.GetUtcNow().UtcDateTime;
            var effectiveUtc = _fixedUtc?.UtcDateTime ?? (realUtc + _offset);

            return new DevelopmentTimeState(
                realUtc,
                effectiveUtc,
                _fixedUtc.HasValue || _offset != TimeSpan.Zero,
                _fixedUtc.HasValue
                    ? "FixedUtc"
                    : _offset == TimeSpan.Zero
                        ? "RealTime"
                        : "Offset",
                _offset.TotalMinutes,
                _fixedUtc?.UtcDateTime);
        }
    }

    public DevelopmentTimeState SetFixedUtc(
        DateTimeOffset fixedUtc)
    {
        lock (_syncRoot)
        {
            _fixedUtc = fixedUtc.ToUniversalTime();
            _offset = TimeSpan.Zero;
            return GetStateUnsafe();
        }
    }

    public DevelopmentTimeState ApplyOffset(
        TimeSpan offset)
    {
        lock (_syncRoot)
        {
            _fixedUtc = null;
            _offset = offset;
            return GetStateUnsafe();
        }
    }

    public DevelopmentTimeState Reset()
    {
        lock (_syncRoot)
        {
            _fixedUtc = null;
            _offset = TimeSpan.Zero;
            return GetStateUnsafe();
        }
    }

    private DevelopmentTimeState GetStateUnsafe()
    {
        var realUtc = TimeProvider.System.GetUtcNow().UtcDateTime;
        var effectiveUtc = _fixedUtc?.UtcDateTime ?? (realUtc + _offset);

        return new DevelopmentTimeState(
            realUtc,
            effectiveUtc,
            _fixedUtc.HasValue || _offset != TimeSpan.Zero,
            _fixedUtc.HasValue
                ? "FixedUtc"
                : _offset == TimeSpan.Zero
                    ? "RealTime"
                    : "Offset",
            _offset.TotalMinutes,
            _fixedUtc?.UtcDateTime);
    }
}
