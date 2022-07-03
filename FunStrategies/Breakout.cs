using System;
using System.Diagnostics;
using TSLab.Script;
using TSLab.Script.Handlers;
using TSLab.Script.Helpers;
using TSLab.Script.Optimization;

namespace MyLib
{
    public class Breakout : IExternalScript
    {
        // Параметры оптимизации
        public OptimProperty HighPeriod = new(20, 10, 100, 5);
        public OptimProperty LowPeriod = new(20, 10, 100, 5);

        // Метод обработки, запускается при пересчете скрипта
        public virtual void Execute(IContext ctx, ISecurity sec)
        {
            //запускаем секундомер
            var sw = Stopwatch.StartNew();

            // Вычисляем максимумы и минимумы
            // Используем GetData для кеширования данных и ускорения оптимизации
            var high = ctx.GetData("Highest", new[] { HighPeriod.ToString() },
                () => Series.Highest(sec.GetHighPrices(ctx), HighPeriod));
            var low = ctx.GetData("Lowest", new[] { LowPeriod.ToString() },
                () => Series.Lowest(sec.GetLowPrices(ctx), LowPeriod));

            // Если последняя свеча до конца не сформировалась, ее не нужно использовать в цикле торговли
            var barsCount = sec.Bars.Count;
            if (!ctx.IsLastBarUsed)
            {
                barsCount--;
            }

            // Торговля
            for (int i = ctx.TradeFromBar; i < barsCount; i++)
            {
                // Получаем активные позиции
                var posLong = sec.Positions.GetLastActiveForSignal("LE", i);
                var posShort = sec.Positions.GetLastActiveForSignal("SE", i);

                if (posLong == null)
                {
                    // Если нет активной длинной позиции, выдаем условный ордер на создание новой позиции
                    sec.Positions.BuyIfGreater(i + 1, 1, high[i], "LE");
                }
                else
                {
                    // Если есть длинная позиция, то ставим стоп
                    posLong.CloseAtStop(i + 1, low[i], "LX");
                }


                if (posShort == null)
                {
                    // Если нет активной короткой позиции, выдаем условный ордер на создание новой позиции
                    sec.Positions.SellIfLess(i + 1, 1, low[i], "SE");
                }
                else
                {
                    // Если есть короткая позиция, то ставим стоп
                    posShort.CloseAtStop(i + 1, high[i], "SX");
                }
            }

            //данные секундомера только в режиме лабаратории
            if (!ctx.Runtime.IsAgentMode)
            {
                ctx.Log($"Time: {sw.Elapsed}", MessageType.Info, true);
            }

            // Если идет процесс оптимизации, то графики рисовать не нужно, это замедляет работу
            if (ctx.IsOptimization)
            {
                return;
            }

            // Отрисовка графиков
            ctx.First.AddList(string.Format("High({0})", HighPeriod), high, ListStyles.LINE, ScriptColors.Green,
                LineStyles.SOLID, PaneSides.RIGHT);
            ctx.First.AddList(string.Format("Low({0})", LowPeriod), low, ListStyles.LINE, ScriptColors.Red,
                LineStyles.SOLID, PaneSides.RIGHT);
        }
    }
}