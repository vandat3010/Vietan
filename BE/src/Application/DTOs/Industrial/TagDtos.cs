namespace Backend.Application.DTOs.Industrial;

/// <summary>
/// A single historian sample. The three value slots are all nullable because a tag
/// is analog, discrete or textual and only one slot is ever meaningful - this
/// mirrors how SCADA historians store heterogeneous tags in one wide table rather
/// than forcing a DTO per data type.
/// </summary>
public class TagValueDto
{
    public Guid TagId { get; set; }
    public string TagName { get; set; } = default!;
    public double? NumericValue { get; set; }
    public string? StringValue { get; set; }
    public bool? BooleanValue { get; set; }

    /// <summary>Timestamp of the reading at the source, not of the write, so replayed or buffered samples land in the right bucket.</summary>
    public DateTime TimestampUtc { get; set; }

    public TagQuality Quality { get; set; }
}

/// <summary>
/// Carried alongside every sample because a bad/uncertain reading must still be
/// stored - discarding it would silently turn a sensor failure into a data gap.
/// </summary>
public enum TagQuality
{
    Good = 0,
    Bad = 1,
    Uncertain = 2
}

/// <summary>
/// Query for historian reads. The aggregation fields exist because raw reads over
/// long ranges must be down-sampled server-side (TimescaleDB <c>time_bucket</c>),
/// never in the API layer: a month of one-second samples is millions of rows per
/// tag, and materialising them just to average them would exhaust memory and
/// bandwidth before the response is ever written.
/// </summary>
public class TagHistoryQuery
{
    /// <summary>Multiple tags per query so a trend chart is one round trip instead of one per pen.</summary>
    public IReadOnlyList<Guid> TagIds { get; set; } = [];

    public DateTime FromUtc { get; set; }
    public DateTime ToUtc { get; set; }

    /// <summary>Bucket width for down-sampling; null means return raw samples, which callers should only do for short ranges.</summary>
    public int? AggregationIntervalSeconds { get; set; }

    /// <summary>Ignored when <see cref="AggregationIntervalSeconds"/> is null.</summary>
    public TagAggregate? Aggregate { get; set; }
}

/// <summary>
/// <see cref="First"/> and <see cref="Last"/> are included because discrete and
/// textual tags cannot be averaged, so they need a bucket reducer that preserves
/// an actual observed value.
/// </summary>
public enum TagAggregate
{
    None = 0,
    Avg = 1,
    Min = 2,
    Max = 3,
    First = 4,
    Last = 5
}
