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

namespace FunStrategies
{
    public class TrailPos : IExternalScript
    {
        public void Execute(IContext ctx, ISecurity sec)
        {
            //расчет коммисии в одну сторону мин 0,0002 макс 0,0004
            sec.Commission = (pos, price, shares, isEntry, isPart) => price * shares * 0.0004;

            TrailStopAbs trailAbs = new();


            //торговый цикл
            var barsCount = sec.Bars.Count;
            if (!ctx.IsLastBarUsed)
            {
                barsCount--;
            }

            for (int i = ctx.TradeFromBar; i < barsCount; i++)
            {
                //торговая логика
                bool signalBuy = true;

                //работа с позициями
                var longPos = sec.Positions.GetLastActiveForSignal("LE", i);
                if (longPos == null)
                {
                    if (signalBuy)
                    {
                        sec.Positions.BuyAtMarket(i + 1, 100, "LE", "FictitiousBuy", PositionExecution.Fictitious);
                    }
                }
                else
                {
                    // настройки трейла
                    var delta = longPos.EntryPrice - sec.Bars[i - 1].Low;

                    trailAbs.StopLoss = delta;
                    trailAbs.TrailEnable = 0.002;
                    trailAbs.TrailLoss = 0.002;

                    var stop = trailAbs.Execute(longPos, i);

                    longPos.CloseAtStop(i + 1, stop, "LX", "FictitiousClose");

                }
            }
        }
    }
}
