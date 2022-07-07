using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TSLab.Script;
using TSLab.Script.Handlers;
using TSLab.DataSource;
using TSLab.Script.Optimization;
using TSLab.Script.Helpers;
using TSLab.Script.Control;
using System.Diagnostics;
using MathNet.Numerics.Statistics;

namespace FunStrategies
{
    public class BarsStatistic : IExternalScript2
    {
        public void Execute(IContext ctx, ISecurity sec1, ISecurity sec2)
        {
            // замер времени работы скрипта: старт
            var sw = Stopwatch.StartNew();

            var volumes = sec1.Volumes;
            var barsRangeProcent = ctx.GetData("BarsRangeProcent", Array.Empty<string>(), () => sec1.Bars.Select(bar => ((bar.High - bar.Low) / bar.Low * 100)).ToList());

            var closes1 = sec1.ClosePrices.ToArray();
            var closes2 = sec2.ClosePrices.ToArray();
            int period = 200;
            var barsCount = sec1.Bars.Count;
            var cc = new double[barsCount];
            for (int i = period - 1; i < barsCount; i++)
            {
                double[] partA = new double[period];
                double[] partB = new double[period];

                Array.Copy(closes1, i - period + 1, partA, 0, period);
                Array.Copy(closes2, i - period + 1, partB, 0, period);

                cc[i] = Correlation.Pearson(partA, partB);
            }

            Histogram histVol = new(volumes, 20);
            double dataCount = histVol.DataCount;
            StringBuilder sb = new();
            sb.AppendLine($"DataCount: {dataCount}");
            sb.AppendLine($"Volumes Histogram ({sec1.Symbol})");
            for (int i = 0; i < histVol.BucketCount; i++)
            {
                var countProcent = Math.Round((histVol[i].Count / dataCount * 100), 2);
                sb.AppendLine($"({histVol[i].LowerBound.ToString("N0")} : {histVol[i].UpperBound.ToString("N0")}] = {histVol[i].Count} ({countProcent}%)");
            }

            Histogram histRangePrs = new(barsRangeProcent, 10);
            sb.AppendLine($"BarsRangeProcent Histogram ({sec1.Symbol})");
            for (int i = 0; i < histRangePrs.BucketCount; i++)
            {
                var countProcent = Math.Round((histRangePrs[i].Count / dataCount * 100), 2);
                sb.AppendLine($"({histRangePrs[i].LowerBound.ToString("N2")} : {histRangePrs[i].UpperBound.ToString("N2")}]% = {histRangePrs[i].Count} ({countProcent}%)");
            }

            var cntPane = ctx.CreateControlPane("Statistic", "Statistic", "Statistic");
            cntPane.AddTextElement("histogram", true, 10.0, 10.0, 300, 700, AlphaColors.AliceBlue, sb.ToString());

            var ccpane = ctx.CreateGraphPane("cc", "cc", false);
            var rangePane = ctx.CreateGraphPane("Range", "BarsRangeProcent", false);
            var volPane = ctx.CreateGraphPane("Volume", "Volume", false);

            rangePane.AddList("Range", barsRangeProcent, ListStyles.HISTOHRAM, ScriptColors.Gold, LineStyles.SOLID, PaneSides.RIGHT);
            volPane.AddList("Volume", volumes, ListStyles.HISTOHRAM, ScriptColors.BlueViolet, LineStyles.SOLID, PaneSides.RIGHT);
            ccpane.AddList("cc", cc, ListStyles.HISTOHRAM, ScriptColors.BlueViolet, LineStyles.SOLID, PaneSides.RIGHT);

            // пишем в лог время расчета скрипта только в режиме лабаратории
            if (!ctx.Runtime.IsAgentMode)
            {
                ctx.Log($"Время расчета скрипта: {sw.Elapsed}", MessageType.Info, true);
            }
        }
    }
}
