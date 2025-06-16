using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RearrangeBookmarks
{
    public class RailOption(string name)
    {
        public string Name { get; set; } = name;
        public Alignment Alignment { get; set; } = Alignment.Left;
        public float OffsetBottom { get; set; }
        public float OffsetTop { get; set; }
        public float LimitLeft { get; set; } = 0.5f;
        public float LimitRight { get; set; } = 0.5f;
        public float LimitTop { get; set; }
        public bool StackEmpty { get; set; } = true;
    }
}
