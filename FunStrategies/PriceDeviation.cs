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
using Helpers;
using MathNet.Numerics.Distributions;

namespace FunStrategies
{
    public class PriceDeviation : IExternalScript2
    {
        public void Execute(IContext ctx, ISecurity sec1, ISecurity sec2)
        {
            //наборы
            var bars = sec1.Bars;
            var fitToLeader = ctx.GetData("FitToLeader", Array.Empty<string>(), () => Helpers.Series.LinearFitData(sec2.ClosePrices, sec1.ClosePrices, 24));
            //var diff = ctx.GetData("diff", Array.Empty<string>(), () => TSLab.Script.Helpers.Series.Sub(sec1.ClosePrices, fitToLeader));
            var change = ctx.GetData("change", Array.Empty<string>(), () => Helpers.Series.Change(sec1.ClosePrices, fitToLeader));

            var barsCount = bars.Count;
            if (!ctx.IsLastBarUsed)
            {
                barsCount--;
            }
            for (int i = barsCount - 1; i < barsCount; i++)
            {
                if (change[i] <= -0.7 || change[i] >= 0.7)
                {
                    ctx.Log($"{sec1.Symbol} fit {sec2.Symbol}: {change[i]}%", MessageType.Info, true);
                }
            }

            if (ctx.IsOptimization)
            {
                return;
            }

            ctx.First.AddList($"Fit to {sec2.Symbol}", fitToLeader, ListStyles.LINE_WO_ZERO, ScriptColors.Blue, LineStyles.SOLID, PaneSides.RIGHT);

            //var paneOne = ctx.CreateGraphPane("Diff", "Diff", false);
            var paneTwo = ctx.CreateGraphPane("Change", "Change,%", false);

            //paneOne.AddList("Diff", diff, ListStyles.HISTOHRAM, ScriptColors.BlueViolet, LineStyles.SOLID, PaneSides.RIGHT);
            paneTwo.AddList("Change,%", change, ListStyles.HISTOHRAM, ScriptColors.BlueViolet, LineStyles.SOLID, PaneSides.RIGHT);
        }
    }
}
