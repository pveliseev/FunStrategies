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
    public class FalseBreak : IExternalScript
    {
        public void Execute(IContext ctx, ISecurity sec)
        {
            //расчет коммисии в одну сторону мин 0,0002 макс 0,0004
            sec.Commission = (pos, price, shares, isEntry, isPart) => price * shares * 0.0004;

            // наборы            
            var changePct = ctx.GetData("ChangePct", new[] { "" }, () => sec.Bars.Select(bar => (bar.Close - bar.Open) / bar.Open * 100).ToList());
            var lowest = Series.Lowest(sec.LowPrices, 5);

            var trail = new TrailStop()
            {
                //StopLoss = 0.7,
                TrailEnable = 1.5,
                TrailLoss = 1.5
            };


            //торговый цикл
            var barsCount = sec.Bars.Count;
            if (!ctx.IsLastBarUsed)
            {
                barsCount--;
            }

            for (int i = ctx.TradeFromBar; i < barsCount; i++)
            {
                //торговая логика
                bool signalBuy = (changePct[i - 1] < -0.5d && sec.Bars[i - 1].Close < lowest[i - 2] && sec.Bars[i].Close * 1.001 >= sec.Bars[i - 1].Open) ||
                                 (changePct[i - 2] < -0.5d && sec.Bars[i - 2].Close < lowest[i - 3] && sec.Bars[i].Close >= sec.Bars[i - 2].Open);

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
                    // скидывать часть позиции на первом импульсе
                    longPos.ChangeAtProfit(i + 1, longPos.EntryPrice * 1.01, 50, "PE");
                    
                    // стопить по трейлингу
                    trail.StopLoss = 0.7;
                    

                    var stop = trail.Execute(longPos, i);
                    longPos.CloseAtStop(i + 1, stop, "LX", "FictitiousClose");

                }
            }


            if (ctx.IsOptimization)
            {
                return;
            }

            var changePane = ctx.CreateGraphPane("Change", "Change", false);

            ctx.First.AddList("Lowest", lowest, ListStyles.LINE, ScriptColors.Magenta, LineStyles.SOLID, PaneSides.RIGHT);
            changePane.AddList("Range", changePct, ListStyles.HISTOHRAM, ScriptColors.Gold, LineStyles.SOLID, PaneSides.RIGHT);
        }
    }
}
