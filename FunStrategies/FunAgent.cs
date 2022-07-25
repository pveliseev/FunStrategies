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

namespace FunStrategies
{
    public class FunAgent : IExternalScript
    {
        public void Execute(IContext ctx, ISecurity sec)
        {
            // расчет коммисии на бинансе за сделку в одну сторону: мин 0,0002 макс 0,0004
            sec.Commission = (pos, price, shares, isEntry, isPart) => price * shares * 0.0004;

            var bars = sec.Bars;

            //наборы
            var barChangePercent = ctx.GetData("BarChangePercent", Array.Empty<string>(), () => Helpers.Series.BarChange(bars, ValueMode.Percent));
            var barTemper = ctx.GetData("BarTemper", Array.Empty<string>(), () => Helpers.Series.BarTemper(bars));
            var frq = ctx.GetData("Frq", Array.Empty<string>(), () => Helpers.Series.FrequencyDistribution(barChangePercent, 7, DataValueMode.Absolute));

            //**********************************************************************
            double buyPrice = default;
            // торговый цикл
            var barsCount = bars.Count;
            if (!ctx.IsLastBarUsed)
            {
                barsCount--;
            }
            for (int i = ctx.TradeFromBar; i < barsCount; i++)
            {
                var longPos = sec.Positions.GetLastActiveForSignal("LE", i);

                // торговые сигналы
                bool signal = barTemper[i] == -1;

                // работа с позициями. (Фиктивное исполнение заявок работает только с заявками "По рынку")
                if (longPos == null)
                {
                    if (signal)
                    {
                        buyPrice = bars[i].Close;  //bars[i].High - (bars[i].High - bars[i].Low)/3;
                        sec.Positions.BuyAtPrice(i + 1, 100, buyPrice, "LE");
                    }
                }
                else
                {
                    longPos.CloseAtStop(i + 0, longPos.EntryPrice * 0.997, "StopLX");
                    longPos.CloseAtProfit(i + 0, longPos.EntryPrice * 1.003, "ProfitLX");
                }
            }
            //**********************************************************************


            // если в режиме оптимизации то не выводим данные на график
            if (ctx.IsOptimization)
            {
                return;
            }

            // создание панелей
            var paneOne = ctx.CreateGraphPane("BarChangePercent", "BarChangePercent", false);
            //var paneTwo = ctx.CreateGraphPane("BarTemper", "BarTemper", false);
            var paneThree = ctx.CreateGraphPane("Frq", "Frq", false);            

            // отрисовка графиков
            paneOne.AddList("BarChangePercent", barChangePercent, ListStyles.HISTOHRAM, ScriptColors.BlueViolet, LineStyles.SOLID, PaneSides.RIGHT);
            //paneTwo.AddList("BarTemper", barTemper, ListStyles.HISTOHRAM, ScriptColors.BlueViolet, LineStyles.SOLID, PaneSides.RIGHT);
            paneThree.AddList("Frq", frq, ListStyles.HISTOHRAM, ScriptColors.BlueViolet, LineStyles.SOLID, PaneSides.RIGHT);
        }
    }
}
