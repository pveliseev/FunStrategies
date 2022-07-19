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
            //изменение баров в %, (close[i] - close[i-1])/close[i-1]*100
            var barChangePercent = ctx.GetData("BarChangePercent", Array.Empty<string>(), () => CalcBarChangePercent(bars));


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
                bool signal = barChangePercent[i] >= 0.3;

                // работа с позициями. (Фиктивное исполнение заявок работает только с заявками "По рынку")
                if (longPos == null)
                {
                    if (signal)
                    {
                        buyPrice = bars[i].High - (bars[i].High - bars[i].Low)/3;
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

            // отрисовка графиков
            paneOne.AddList("BarChangePercent", barChangePercent, ListStyles.HISTOHRAM, ScriptColors.BlueViolet, LineStyles.SOLID, PaneSides.RIGHT);
        }

        #region вспомогательные методы
        private static IList<double> CalcBarChangePercent(IReadOnlyList<IDataBar> bars)
        {
            int barsCount = bars.Count;
            double[] list = new double[barsCount];
            for (int i = 1; i < barsCount; i++)
            {
                double change = (bars[i].Close - bars[i - 1].Close) / bars[i - 1].Close * 100;
                list[i] = Math.Round(change, 2);
            }
            return list.ToList<double>();
        }


        #endregion
    }
}
