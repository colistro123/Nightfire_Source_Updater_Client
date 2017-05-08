using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nightfire_Source_Updater_Client
{
    class Utils
    {
        static public double roundToMb(long number)
        {
            return Math.Round(((number * 1.0) / 1000 / 1024), 2);
        }
    }
}
