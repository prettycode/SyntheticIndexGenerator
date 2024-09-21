namespace Data.BackTest;

internal partial class BackTestService
{
    private static BackTestDrawdownPeriod[] GetBackTestPeriodReturnDrawdownPeriods(BackTestPeriodReturn[] returns)
    {
        var returnsCount = returns.Length;

        if (returnsCount == 0)
        {
            return [];
        }

        var result = new List<BackTestDrawdownPeriod>();
        var drawdownStartingBalance = returns[0].StartingBalance;
        var inDrawdown = returns[0].ReturnPercentage < 0;
        DateTime? drawdownFirstPeriodStart = !inDrawdown ? null : returns[0].PeriodStart;

        for (var i = 0; i < returnsCount; i++)
        {
            var wasInDrawdown = inDrawdown;
            var currentReturn = returns[i];

            inDrawdown = currentReturn.EndingBalance < drawdownStartingBalance;

            if (!inDrawdown)
            {
                drawdownStartingBalance = currentReturn.EndingBalance;
            }

            // Drawdown or positive returns continue
            if ((wasInDrawdown && inDrawdown) || (!wasInDrawdown && !inDrawdown))
            {
                continue;
            }

            var drawdownIsUnfinished = inDrawdown && i == returnsCount - 1;

            // Drawdown started
            if (!wasInDrawdown && inDrawdown)
            {
                drawdownFirstPeriodStart = currentReturn.PeriodStart;

                if (!drawdownIsUnfinished)
                {
                    continue;
                }
            }

            // Drawdown ended
            var drawdownEndsWithCurrentReturn = wasInDrawdown && !inDrawdown;

            if (drawdownIsUnfinished || drawdownEndsWithCurrentReturn)
            {
                if (drawdownFirstPeriodStart == null)
                {
                    throw new InvalidOperationException("Drawdown first period start is null.");
                }

                result.Add(new BackTestDrawdownPeriod(currentReturn)
                {
                    Ticker = currentReturn.Ticker,
                    FirstNegativePeriodStart = drawdownFirstPeriodStart.Value,
                    FirstPositivePeriodStart = currentReturn.PeriodStart
                });

                drawdownFirstPeriodStart = null;

                continue;
            }

            throw new InvalidOperationException();
        }


        return [.. result];
    }

    private static BackTestPeriodReturn[] GetBackTestPeriodReturnDrawdownReturns(BackTestPeriodReturn[] returns)
    {
        var returnsCount = returns.Length;

        if (returnsCount == 0)
        {
            return [];
        }

        var drawdowns = new List<BackTestPeriodReturn>();
        var drawdownStartingBalance = returns[0].StartingBalance;

        for (var i = 0; i < returnsCount; i++)
        {
            var currentReturn = returns[i];
            var inDrawdown = currentReturn.EndingBalance < drawdownStartingBalance;

            if (!inDrawdown)
            {
                drawdownStartingBalance = currentReturn.EndingBalance;

                drawdowns.Add(new BackTestPeriodReturn()
                {
                    PeriodStart = currentReturn.PeriodStart,
                    PeriodType = currentReturn.PeriodType,
                    Ticker = currentReturn.Ticker,
                    // TODO StartingBalance is not appropriate, should be null
                    StartingBalance = currentReturn.StartingBalance,
                    ReturnPercentage = 0
                });

                continue;
            }

            drawdowns.Add(new BackTestPeriodReturn()
            {
                PeriodStart = currentReturn.PeriodStart,
                PeriodType = currentReturn.PeriodType,
                Ticker = currentReturn.Ticker,
                // TODO StartingBalance is not appropriate, should be null
                StartingBalance = currentReturn.StartingBalance,
                ReturnPercentage = ((currentReturn.EndingBalance / drawdownStartingBalance - 1) * 100)
            });
        }

        return [.. drawdowns];
    }
}
