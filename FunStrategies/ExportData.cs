using System;
using System.Collections.Generic;
using System.Linq;
using TSLab.Script;
using TSLab.Script.Handlers;
using System.IO;

namespace FunStrategies
{
    public class ExportData : IExternalScript
    {
        public void Execute(IContext ctx, ISecurity sec)
        {
            if (!ctx.Runtime.IsAgentMode)
            {
                var bars = sec.Bars;

                //путь к папке пользователя
                var docFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments, Environment.SpecialFolderOption.Create);
                var path = Path.Combine(docFolder, $"{sec.Symbol}_{sec.Interval}{sec.IntervalBase}.txt");

                using (StreamWriter sr = new(path, false))
                {
                    foreach (var bar in bars)
                    {
                        sr.WriteLine(bar);
                    }
                }
            }
        }
    }
}
