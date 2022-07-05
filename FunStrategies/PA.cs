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
    public class PA : IExternalScript
    {
        private const double BALANCE = 100.0;
        // параметры для оптимизатора
        public IntOptimProperty StartHour = new(0, 0, 12, 1);
        public IntOptimProperty EndHour = new(23, 12, 23, 1);

        public void Execute(IContext ctx, ISecurity sec)
        {
            // замер времени работы скрипта: старт
            var sw = Stopwatch.StartNew();

            // расчет коммисии на бинансе за сделку в одну сторону: мин 0,0002 макс 0,0004
            sec.Commission = (pos, price, shares, isEntry, isPart) => price * shares * 0.0004;

            var bars = sec.Bars;

            // наборы создаем через кэш
            IList<double> makerBarChange() => bars.Select(CalcBarChange).ToList();
            IList<int> makerClosePosition() => bars.Select(CalcClosePosition).ToList();

            var barChange = ctx.GetData("BarChange%", Array.Empty<string>(), makerBarChange);
            var closePosition = ctx.GetData("ClosePosition", Array.Empty<string>(), makerClosePosition);
            var closeComparison = ctx.GetData("CloseComparison", Array.Empty<string>(), () => CalcCloseComparison(bars));

            // для фильтра сделок по времени
            var timeStart = new TimeSpan(StartHour, 0, 0);
            var timeEnd = new TimeSpan(EndHour, 59, 59);

            //**********************************************************************
            // торговый цикл
            var barsCount = sec.Bars.Count;
            if (!ctx.IsLastBarUsed)
            {
                barsCount--;
            }

            double weightLong = 0.0;
            double deadPrice = 1000000.0;
            double close = 0.0;
            double high = 0.0;

            //принцип побарный анализ для входа в сделку и побарный анализ во время сделки для принятия решения о закрытии

            for (int i = ctx.TradeFromBar; i < barsCount; i++)
            {
                var longPos = sec.Positions.GetLastActiveForSignal("LE", i);

                // торговые сигналы
                if (barChange[i] > 0.7 && closeComparison[i] == 1 && weightLong < 2.9 && longPos == null)
                {
                    weightLong += 3.0;
                    close = bars[i].Close;
                    high = bars[i].High;
                    deadPrice = bars[i].High - 2 * ((bars[i].High - bars[i].Low) / 3);
                    continue;
                }
                if (bars[i].Close < close && bars[i].Close > deadPrice)
                {
                    weightLong += 0.1;
                }
                else
                {
                    weightLong = 0.0;
                    close = 0.0;
                    high = 0.0;
                    deadPrice = 1000000.0;
                }

                // фильтры              
                var filterTime = bars[i].Date.TimeOfDay >= timeStart && bars[i].Date.TimeOfDay <= timeEnd;

                // работа с позициями. (Фиктивное исполнение заявок работает только с заявками "По рынку")
                if (longPos == null)
                {
                    if (weightLong >= 3.2 && filterTime)
                    {
                        sec.Positions.BuyIfGreater(i + 1, sec.RoundShares(BALANCE / high), high, "LE");
                    }
                }
                else
                {
                    longPos.CloseAtStop(i + 0, longPos.EntryPrice * 0.99, "StopLX");
                    longPos.CloseAtProfit(i + 0, longPos.EntryPrice * 1.01, "ProfitLX");
                }
            }
            //**********************************************************************

            // пишем в лог время расчета скрипта только в режиме лабаратории
            if (!ctx.Runtime.IsAgentMode)
            {
                ctx.Log($"Время расчета скрипта: {sw.Elapsed}", MessageType.Info, true);
            }

            // если в режиме оптимизации то не выводим данные на график
            if (ctx.IsOptimization)
            {
                return;
            }

            // создание панелей
            var paneOne = ctx.CreateGraphPane("BarChange%", "BarChange%", false);
            var paneTwo = ctx.CreateGraphPane("ClosePosition", "ClosePosition", false);
            var paneThree = ctx.CreateGraphPane("CloseComparison", "CloseComparison", false);

            // отрисовка графиков
            paneOne.AddList("BarChange%", barChange, ListStyles.HISTOHRAM, ScriptColors.BlueViolet, LineStyles.SOLID, PaneSides.RIGHT);
            paneTwo.AddList("ClosePosition", closePosition, ListStyles.HISTOHRAM, ScriptColors.Gold, LineStyles.SOLID, PaneSides.RIGHT);
            paneThree.AddList("CloseComparison", closeComparison, ListStyles.HISTOHRAM, ScriptColors.Gold, LineStyles.SOLID, PaneSides.RIGHT);
        }

        #region вспомогательные методы
        private double CalcBarChange(IDataBar bar)
        {
            double change = (bar.Close - bar.Open) / bar.Open * 100.0;
            return Math.Round(change, 2);
        }

        private int CalcClosePosition(IDataBar bar)
        {
            double part = (bar.High - bar.Low) / 3.0;
            if (bar.Close > (bar.Low + 2 * part)) return 1;
            else if (bar.Close < bar.Low + part) return -1;
            else return 0;
        }

        private IList<int> CalcCloseComparison(IReadOnlyList<IDataBar> bars)
        {
            int barsCount = bars.Count;
            int[] list = new int[barsCount];

            for (int i = 1; i < barsCount; i++)
            {
                if (bars[i].Close > bars[i - 1].High) list[i] = 1;
                else if (bars[i].Close < bars[i - 1].Low) list[i] = -1;
                else list[i] = 0;
            }

            return list.ToList();
        }
        #endregion
    }
}
