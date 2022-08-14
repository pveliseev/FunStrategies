using System;
using System.Collections.Generic;
using System.Linq;
using TSLab.DataSource;
using TSLab.Script.Handlers;
using TSLab.Utils;
using MathNet.Numerics.Statistics;
using MathNet.Numerics.LinearRegression;

namespace Helpers
{
    public enum ValueMode { Normal, Percent }
    public enum DataValueMode { None, Absolute }
    public enum CorrelationType { Pearson, Spearman }
    public static class Series
    {
        //изменение в процентах данных в наборе a относительно данных в наборе b
        public static IList<double> Change(IList<double> a, IList<double> b)
        {
            if (a == null)
                throw new ArgumentNullException(nameof(a));
            if (b == null)
                throw new ArgumentNullException(nameof(b));

            int length = Math.Max(a.Count, b.Count);
            double[] list = new double[length];

            if (length != 0)
            {
                for (int i = 0; i < length; i++)
                {
                    list[i] = Math.Round((a[i] - b[i]) / b[i] * 100, 2);
                }
            }

            return list.ToList<double>();
        }


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

        //эестремумы. в разработке
        public static (IList<double> Min, IList<double> Max) Extremum(IReadOnlyList<IDataBar> bars)
        {
            int length = bars != null ? bars.Count : throw new ArgumentNullException(nameof(bars));
            double[] min = new double[length];
            double[] max = new double[length];

            min[0] = bars[0].Low;
            max[0] = bars[0].High;

            for (int i = 1; i < length; i++)
            {
                if (bars[i].Close <= max[i - 1] && bars[i].Close >= min[i - 1])
                {
                    min[i] = min[i - 1];// Math.Min(min[i - 1], bars[i].Low);
                    max[i] = max[i - 1];// Math.Max(max[i - 1], bars[i].High);
                }
                else
                {
                    min[i] = bars[i].Low;
                    max[i] = bars[i].High;
                }
            }

            return (min.ToList<double>(), max.ToList<double>());
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
            if (mode == DataValueMode.Absolute)
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

        //Pearson Correlation
        public static IList<double> TwoDataCorrelation(CorrelationType type, IEnumerable<double> dataA, IEnumerable<double> dataB, int period)
        {
            if (dataA == null)
                throw new ArgumentNullException(nameof(dataA));
            if (dataB == null)
                throw new ArgumentNullException(nameof(dataB));
            if (period < 1)
                throw new ArgumentOutOfRangeException(nameof(period));
            int length = Math.Max(dataA.Count(), dataB.Count());
            double[] list = new double[length];
            if (length != 0)
            {
                Queue<double> A = new();
                Queue<double> B = new();
                //выбор метода расчета корреляции
                Func<IEnumerable<double>, IEnumerable<double>, double> mode;
                if (type == CorrelationType.Pearson)
                    mode = Correlation.Pearson;
                else
                    mode = Correlation.Spearman;

                for (int i = 0; i < length; i++)
                {
                    A.Enqueue(dataA.ElementAt(i));
                    B.Enqueue(dataB.ElementAt(i));
                    if (i >= period)
                    {
                        A.Dequeue();
                        B.Dequeue();
                    }
                    list[i] = mode(A, B);
                }
            }
            return list.ToList<double>();
        }

        //LinearRegression
        public static IList<double> LinearFitData(IList<double> independent, IList<double> dependent, int period)
        {
            if (independent == null)
                throw new ArgumentNullException(nameof(independent));
            if (dependent == null)
                throw new ArgumentNullException(nameof(dependent));
            if (period < 1)
                throw new ArgumentOutOfRangeException(nameof(period));
            int length = Math.Max(independent.Count, dependent.Count);
            double[] list = new double[length];
            if (length != 0)
            {
                Queue<double> X = new();
                Queue<double> Y = new();

                for (int i = 0; i < length; i++)
                {
                    X.Enqueue(independent[i]);
                    Y.Enqueue(dependent[i]);
                    if (i >= period)
                    {
                        X.Dequeue();
                        Y.Dequeue();
                    }

                    if (i == 0)
                    {
                        list[i] = dependent[i];
                        continue;
                    }

                    (double A, double B) = SimpleRegression.Fit(X.ToArray<double>(), Y.ToArray<double>());

                    list[i] = A + B * independent[i];
                }
            }
            return list.ToList<double>();
        }
    }
}
