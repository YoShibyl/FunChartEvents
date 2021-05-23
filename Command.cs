using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FunChartEvents
{
    struct ChartCommand
    {
        public string Command;
        public uint Tick;
        public string Parameter;
        public bool NoNewLine;
        public string OriginalCommand;
        public float TimeInMs;
        public float Resolution;

        public override string ToString()
        {
            return "{\n" +
                $"\tCommand:{this.Command},\n" +
                $"\tTick:{this.Tick},\n" +
                $"\tParameter:{this.Parameter},\n" +
                "}";
        }
    }
}