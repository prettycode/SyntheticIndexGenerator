using Microsoft.Extensions.Options;

namespace Data.Quotes.QuoteProvider;

public class FmpQuoteProviderOptions : IOptions<FmpQuoteProviderOptions>
{
    private const string ApiKeyEnvVarName = "FmpQuoteProviderOptionsApiKey";

    private static readonly string apiKey;

    static FmpQuoteProviderOptions()
    {
        // onlyExactPath: false says to traverse paternal ascendant directories for an .env file
        DotNetEnv.Env.Load(options: new DotNetEnv.LoadOptions(onlyExactPath: false));

        apiKey = Environment.GetEnvironmentVariable(ApiKeyEnvVarName)
            ?? throw new InvalidOperationException($"'{ApiKeyEnvVarName}' is not a defined environmental variable.");
    }

    public string ApiKey { get; init; } = apiKey;

    FmpQuoteProviderOptions IOptions<FmpQuoteProviderOptions>.Value => this;
}
