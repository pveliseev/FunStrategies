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
    public class Test : IExternalScript
    {
        public void Execute(IContext ctx, ISecurity sec)
        {
            var range = sec.Bars.Select(bar => (bar.High - bar.Low) / bar.Open * 100).ToList();

            double min = Math.Round(range.Min(), 2);
            double max = Math.Round(range.Max(), 2);

            int count = Convert.ToInt32(Math.Ceiling(max));
            int[] histohram = new int[count];



            for (int i = 0; i < count; i++)
            {
                for (int j = 0; j < range.Count; j++)
                {
                    if (range[j] < (i + 1) && range[j] >= i)
                    {
                        histohram[i] += 1;
                    }
                }
            }

            int sum = histohram.Sum();

            StringBuilder sb = new();
            sb.AppendLine($"мин {min}%; макс {max}%");
            for (int i = 0; i < count; i++)
            {
                sb.AppendLine($"[{i}-{i+1}] => {histohram[i]}");
            }
            sb.Append($"Sum: {sum.ToString()}");

            var rangePane = ctx.CreateGraphPane("Range,%", "Range,%", false);
            rangePane.AddList("Range", range, ListStyles.HISTOHRAM, ScriptColors.Gold, LineStyles.SOLID, PaneSides.RIGHT);

            var cp = ctx.CreateControlPane("Histohram", "Histohram", "Histohram");
            cp.AddTextElement("hg", true, 10.0, 10.0, 150, 700, AlphaColors.AliceBlue, sb.ToString());

            //ctx.Log($"Диапазон свечи: мин {min}%; макс {max}%", MessageType.Info, true);
            //ctx.Log(sb.ToString(), MessageType.Info, true);
        }
    }
}
