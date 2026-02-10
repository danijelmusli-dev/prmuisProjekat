using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MrezeProjekat.Dashboards
{
    public abstract class Dashboard
    {
        protected Layout _root;
        protected readonly List<string> _logs = new List<string>();

        public static string[] nodeColors = new[]
        {
            "#E6E6FA", // lavender
            "#FFE1FF", // thistle1
            "#FFBBFF", // plum1
            "#FF83FA", // orchid1
            "#EE82EE", // violet
            "#FFB6C1", // lightpink1
            "#B0E0E6", // powderblue
            "#87CEFF", // skyblue1
            "#BBFFFF", // paleturquoise1
            "#FFE4E1", // mistyrose1
            "#FFDAB9", // peachpuff
            "#FFE7BA", // wheat1
            "#DCDCDC", // gainsboro
            "#F5F5F5"  // whitesmoke
        };

        public Layout Root => this._root;

        // apstract methods for inheritance
        public abstract void AddClient(string text);
        public abstract void AddServer(string text);
        public abstract void AddNetwork(string text);
        
        public abstract void RefreshPanels();

    }
}
