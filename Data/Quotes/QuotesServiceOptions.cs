using Microsoft.Extensions.Options;

namespace Data.Quotes;

public class QuotesServiceOptions : IOptions<QuotesServiceOptions>
{
    public required bool SkipDownloadingUncachedQuotes { get; init; }

    public int ThrottleQuoteProviderRequestsMs { get; init; }

    QuotesServiceOptions IOptions<QuotesServiceOptions>.Value
    {
        get { return this; }
    }
}
