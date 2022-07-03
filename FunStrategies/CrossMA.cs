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
    public class CrossMA : IExternalScript
    {
        public IntOptimProperty PeriodFast = new(10, 5, 15, 1);
        public IntOptimProperty PeriodSlow = new(30, 20, 40, 1);

        public void Execute(IContext ctx, ISecurity sec)
        {

            //расчет коммисии в одну сторону мин 0,0002 макс 0,0004
            sec.Commission = (pos, price, shares, isEntry, isPart) => price * shares * 0.0004;

            //наборы
            var maFast = ctx.GetData("FastMA", new[] { PeriodFast.ToString() }, () => Series.SMA(sec.ClosePrices, PeriodFast));
            var maSlow = ctx.GetData("SlowMA", new[] { PeriodSlow.ToString() }, () => Series.SMA(sec.ClosePrices, PeriodSlow));
            var range = sec.Bars.Select(bar => bar.High - bar.Low).ToList();           
            

            var trail = new TrailStop()
            {
                StopLoss = 1,
                TrailEnable = 2,
                TrailLoss = 2
            };

            //торговый цикл
            var barsCount = sec.Bars.Count;
            if (!ctx.IsLastBarUsed)
            {
                barsCount--;
            }

            //оптимизация расчета скрипта
            var bars = sec.Bars;
            var timeStart = new TimeSpan(2, 0, 0);
            var timeFinish = new TimeSpan(9, 0, 0);

            for (int i = ctx.TradeFromBar; i < barsCount; i++)
            {
                //торговая логика
                var signalBuy = maFast[i] > maSlow[i] && maFast[i - 1] < maSlow[i - 1];

                //фильтр по времени бара
                var filterTime = bars[i].Date.TimeOfDay >= timeStart && bars[i].Date.TimeOfDay <= timeFinish;

                //работа с позициями
                var longPos = sec.Positions.GetLastActiveForSignal("LE", i);
                if(longPos == null)
                {
                    if (signalBuy)
                    {
                        sec.Positions.BuyAtMarket(i + 1, 1, "LE", "FictitiousBuy", PositionExecution.Fictitious);
                    }
                }
                else
                {
                    var stop = trail.Execute(longPos, i);
                    longPos.CloseAtStop(i + 1, stop, "LX", "FictitiousClose");
                }
            }


            if (ctx.IsOptimization)
            {
                return;
            }

            //создание панелей
            var volPane = ctx.CreateGraphPane("Volume", "Volume", false);
            var rangePane = ctx.CreateGraphPane("Range", "Range", false);
            var controlPane = ctx.CreateControlPane("Control", "Control", "Control Pane");

            var balance = controlPane.AddTextElement("Balance", true, 10d, 10d, 200d, 20d, AlphaColors.Aqua, "test");
            //прорисовка графиков
            ctx.First.AddList(string.Format($"MAFast_{PeriodFast}"), maFast, ListStyles.LINE, ScriptColors.Magenta, LineStyles.SOLID, PaneSides.RIGHT);
            ctx.First.AddList(string.Format($"MASlow_{PeriodSlow}"), maSlow, ListStyles.LINE, ScriptColors.Silver, LineStyles.SOLID, PaneSides.RIGHT);
            volPane.AddList("Volume", sec.Volumes, ListStyles.HISTOHRAM, ScriptColors.BlueViolet, LineStyles.SOLID, PaneSides.RIGHT);
            rangePane.AddList("Range", range, ListStyles.HISTOHRAM, ScriptColors.Gold, LineStyles.SOLID, PaneSides.RIGHT);
        }
    }
}
