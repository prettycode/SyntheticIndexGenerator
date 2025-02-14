﻿namespace Data.Quotes.Extensions;

public static class DecimalExtensions
{
    public static decimal TrimZeros(this decimal value)
    {
        return decimal.Parse(value.ToString().TrimEnd('0'));
    }

    public static decimal FillCents(this decimal value)
    {
        var s = value.ToString();

        if (s.EndsWith('.'))
        {
            return decimal.Parse(s + "00");
        }
        else if (s.Length > 2 && s[^2] == '.')
        {
            return decimal.Parse(s + '0');
        }
        else if (!s.Contains('.'))
        {
            return decimal.Parse(s + ".00");
        }

        return value;
    }

    public static decimal ToQuotePrice(this decimal value, int digits = 4, bool trimZeros = false)
    {
        var forcedPrecision = Decimal.Parse("0." + new string('0', digits));
        var rounded = Math.Round(value + forcedPrecision, digits);

        return trimZeros
            ? rounded.TrimZeros()
            : rounded;
    }
}
