using Config.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlientRecord
{
    public interface IConfig
    {
        [Option(DefaultValue = "")]
        string SaveTo { get; set; }

        [Option(DefaultValue = 48000)]
        int Rate { get; }

        [Option(DefaultValue = 16)]
        int Bit { get; }

        [Option(DefaultValue = 2)]
        int Channels { get; }

        string PartOfMicName { get; set; }
    }
}
