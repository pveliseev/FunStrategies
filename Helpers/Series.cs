using System;
using System.Collections.Generic;
using System.Linq;
using TSLab.DataSource;
using TSLab.Script.Handlers;
using TSLab.Utils;

namespace Helpers
{
    public static class Series
    {
        //изменение бара
        public static IList<double> BarChange(IReadOnlyList<IDataBar> candles)
        {
            int length = candles != null ? candles.Count : throw new ArgumentNullException(nameof(candles));
            double[] list = new double[length];
            for (int i = 0; i < length; i++)
            {
                if (i > 0)
                {
                    list[i] = candles[i].Close - candles[i - 1].Close;
                }
                else
                {
                    list[i] = candles[i].Close - candles[i].Open;
                }
            }
            return list.ToList<double>();
        }

        //изменение бара в %
        public static IList<double> BarChangePercent(IReadOnlyList<IDataBar> candles)
        {
            int length = candles != null ? candles.Count : throw new ArgumentNullException(nameof(candles));
            double[] list = new double[length];
            for (int i = 0; i < length; i++)
            {
                if (i > 0)
                {
                    list[i] = Math.Round((candles[i].Close - candles[i - 1].Close) / candles[i - 1].Close * 100, 2);
                }
                else
                {
                    list[i] = Math.Round((candles[i].Close - candles[i].Open) / candles[i].Open * 100, 2);
                }
            }
            return list.ToList<double>();
        }
    }
}
