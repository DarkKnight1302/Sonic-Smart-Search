using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SonicExplorerLib.Models
{
    public class RecentItems
    {
        public string fileName;
        public string path;

        public override string ToString()
        {
            return $"{fileName}_{path}";
        }
    }
}
