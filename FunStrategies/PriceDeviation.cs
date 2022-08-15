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
        // параметры для оптимизатора
        public IntOptimProperty Period = new(24, 12, 48, 1);
        public OptimProperty Limit = new(0.7, 0.1, 2.0, 0.1);

        //месенджер
        private readonly MessageHandler message = new();

        public void Execute(IContext ctx, ISecurity sec1, ISecurity sec2)
        {
            var bars = sec1.Bars;
            var fitToLeader = ctx.GetData("FitToLeader", Array.Empty<string>(), () => Helpers.Series.LinearFitData(sec2.ClosePrices, sec1.ClosePrices, Period));
            var change = ctx.GetData("change", Array.Empty<string>(), () => Helpers.Series.Change(sec1.ClosePrices, fitToLeader));

            var barsCount = bars.Count;
            var lastBarIndex = barsCount - (ctx.IsLastBarUsed ? 1 : 2);

            //формируем сообщение
            message.IsInCycle = false;
            message.Context = ctx;
            message.Message = $"{sec1.Symbol} fit {sec2.Symbol}: {change[lastBarIndex]}%";
            message.Tag = "Signal";
            message.Type = MessageType.Info;

            //условие отправки сообщения
            bool signal = change[lastBarIndex] <= (-1.0 * Limit) | change[lastBarIndex] >= Limit;

            message.Execute(signal, lastBarIndex);

            if (ctx.IsOptimization)
            {
                return;
            }

            ctx.First.AddList($"Fit to {sec2.Symbol}", fitToLeader, ListStyles.LINE_WO_ZERO, ScriptColors.Blue, LineStyles.SOLID, PaneSides.RIGHT);
            var paneOne = ctx.CreateGraphPane("Change", "Change,%", false);
            paneOne.AddList("Change,%", change, ListStyles.HISTOHRAM, ScriptColors.BlueViolet, LineStyles.SOLID, PaneSides.RIGHT);
        }
    }
}
