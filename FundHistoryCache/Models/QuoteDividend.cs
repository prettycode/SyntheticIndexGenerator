﻿using YahooQuotesApi;
using Legacy = YahooFinanceApi;

namespace FundHistoryCache.Models
{
    public struct QuoteDividend
    {
        public DateTime DateTime { get; set; }

        public decimal Dividend { get; set; }

        public QuoteDividend() { }

        public QuoteDividend(DividendTick dividend)
        {
            ArgumentNullException.ThrowIfNull(dividend);

            DateTime = dividend.Date.ToDateTimeUnspecified();
            Dividend = Convert.ToDecimal(dividend.Dividend);
        }

        public QuoteDividend(Legacy.DividendTick dividend)
        {
            ArgumentNullException.ThrowIfNull(dividend);

            DateTime = dividend.DateTime;
            Dividend = dividend.Dividend;
        }
    }
}