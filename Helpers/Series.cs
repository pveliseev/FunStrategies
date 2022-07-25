using System;
using System.Collections.Generic;
using System.Linq;
using TSLab.DataSource;
using TSLab.Script.Handlers;
using TSLab.Utils;
using MathNet.Numerics.Statistics;

namespace Helpers
{
    public enum ValueMode { Normal, Percent }
    public enum DataValueMode { None, Absolute }
    public static class Series
    {
        //изменение бара
        public static IList<double> BarChange(IReadOnlyList<IDataBar> bars, ValueMode mode)
        {
            int length = bars != null ? bars.Count : throw new ArgumentNullException(nameof(bars));
            double[] list = new double[length];

            switch (mode)
            {
                case ValueMode.Normal:
                    list[0] = bars[0].Close - bars[0].Open;
                    for (int i = 1; i < length; i++)
                    {
                        list[i] = bars[i].Close - bars[i - 1].Close;
                    }
                    break;
                case ValueMode.Percent:
                    list[0] = Math.Round((bars[0].Close - bars[0].Open) / bars[0].Open * 100, 2);
                    for (int i = 1; i < length; i++)
                    {
                        list[i] = Math.Round((bars[i].Close - bars[i - 1].Close) / bars[i - 1].Close * 100, 2);
                    }
                    break;
            }

            return list.ToList<double>();
        }

        //диапазон бара
        public static IList<double> BarRange(IReadOnlyList<IDataBar> bars, ValueMode mode)
        {
            int length = bars != null ? bars.Count : throw new ArgumentNullException(nameof(bars));
            double[] list = new double[length];

            switch (mode)
            {
                case ValueMode.Normal:
                    for (int i = 0; i < length; i++)
                    {
                        list[i] = bars[i].High - bars[i].Low;
                    }
                    break;
                case ValueMode.Percent:
                    for (int i = 0; i < length; i++)
                    {
                        list[i] = Math.Round((bars[i].High - bars[i].Low) / bars[i].Open * 100, 2);
                    }
                    break;
            }

            return list.ToList<double>();
        }

        //положение цены закрытия бара
        public static IList<int> BarClosePosition(IReadOnlyList<IDataBar> bars)
        {
            int length = bars != null ? bars.Count : throw new ArgumentNullException(nameof(bars));
            int[] list = new int[length];
            for (int i = 0; i < length; i++)
            {
                //TODO если high-low равно 0

                double part = (bars[i].High - bars[i].Low) / 3.0;

                if (bars[i].Close > bars[i].Low + 2 * part)
                    list[i] = 3; //high
                else if (bars[i].Close < bars[i].Low + part)
                    list[i] = 1; //low
                else
                    list[i] = 2; //mid
            }
            return list.ToList<int>();
        }

        //сравнение цены закрытия текущего бара с экстремумами предыдущего бара
        public static IList<int> BarCloseComparison(IReadOnlyList<IDataBar> bars)
        {
            int length = bars != null ? bars.Count : throw new ArgumentNullException(nameof(bars));
            int[] list = new int[length];
            for (int i = 1; i < length; i++)
            {
                if (bars[i].Close > bars[i - 1].High)
                    list[i] = 1;
                else if (bars[i].Close < bars[i - 1].Low)
                    list[i] = -1;
                else
                    list[i] = 0;
            }
            return list.ToList<int>();
        }

        //характер бара
        public static IList<int> BarTemper(IReadOnlyList<IDataBar> bars)
        {
            int length = bars != null ? bars.Count : throw new ArgumentNullException(nameof(bars));
            int[] list = new int[length];
            var barClosePosition = BarClosePosition(bars);
            var barCloseComparison = BarCloseComparison(bars);
            for (int i = 1; i < length; i++)
            {
                if (barCloseComparison[i] != -1)
                {
                    list[i] = barCloseComparison[i] * barClosePosition[i];
                }
                else
                {
                    list[i] = barCloseComparison[i] * (4 - barClosePosition[i]);
                }
            }
            return list.ToList<int>();
        }

        public static IList<double> FrequencyDistribution(IEnumerable<double> data, int nbuckets, DataValueMode mode = DataValueMode.None)
        {
            if(mode == DataValueMode.Absolute)
            {
                data = data.Select(x => Math.Abs(x));
            }

            Histogram hist = new(data, nbuckets);
            double dataCount = hist.DataCount;

            int length = data.Count();
            double[] list = new double[length];
            for (int i = 0; i < length; i++)
            {
                list[i] = Math.Round((hist.GetBucketOf(data.ElementAt(i)).Count / dataCount * 100), 2);
            }
            return list.ToList<double>();
        }
    }
}
