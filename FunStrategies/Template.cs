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
    public class Template : IExternalScript
    {
        // параметры для оптимизатора
        public IntOptimProperty PeriodFast = new(9, 5, 15, 1);
        public IntOptimProperty PeriodSlow = new(26, 20, 40, 1);

        public void Execute(IContext ctx, ISecurity sec)
        {
            // замер времени работы скрипта: старт
            var sw = Stopwatch.StartNew();

            // расчет коммисии на бинансе за сделку в одну сторону: мин 0,0002 макс 0,0004
            sec.Commission = (pos, price, shares, isEntry, isPart) => price * shares * 0.0004;

            // наборы создаем через кэш
            var maFast = ctx.GetData("FastMA", new[] { PeriodFast.ToString() }, () => Series.SMA(sec.ClosePrices, PeriodFast));
            var maSlow = ctx.GetData("SlowMA", new[] { PeriodSlow.ToString() }, () => Series.SMA(sec.ClosePrices, PeriodSlow));

            // получить ClosePrices, OpenPrices, HighPrices, LowPrices, Volumes до цикла
            var volumes = sec.Volumes;

            // по возможности создаём необходимые объекты до цикла
            var bars = sec.Bars;
            // для фильтра сделок по времени
            var timeStart = new TimeSpan(0, 0, 0);
            var timeEnd = new TimeSpan(23, 59, 59);

            //**********************************************************************
            // торговый цикл
            var barsCount = sec.Bars.Count;
            if (!ctx.IsLastBarUsed)
            {
                barsCount--;
            }
            for (int i = ctx.TradeFromBar; i < barsCount; i++)
            {
                // торговые сигналы


                // фильтры                
                var filterTime = bars[i].Date.TimeOfDay >= timeStart && bars[i].Date.TimeOfDay <= timeEnd;

                // работа с позициями. (Фиктивное исполнение заявок работает только с заявками "По рынку")

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
            var volPane = ctx.CreateGraphPane("Volume", "Volume", false);

            // отрисовка графиков
            volPane.AddList("Volume", volumes, ListStyles.HISTOHRAM, ScriptColors.BlueViolet, LineStyles.SOLID, PaneSides.RIGHT);
            ctx.First.AddList(string.Format($"MAFast_{PeriodFast}"), maFast, ListStyles.LINE, ScriptColors.Magenta, LineStyles.SOLID, PaneSides.RIGHT);
            ctx.First.AddList(string.Format($"MASlow_{PeriodSlow}"), maSlow, ListStyles.LINE, ScriptColors.Silver, LineStyles.SOLID, PaneSides.RIGHT);
        }
    }
}
