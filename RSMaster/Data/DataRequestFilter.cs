using System.Collections.Generic;

namespace RSMaster
{
    public class DataRequestFilter
    {
        public string OrderBy { get; set; } = "DESC";
        public string OrderColumn { get; set; } = "Id";
        public int Limit { get; set; } = 1000;
        public Dictionary<string, object> Conditions { get; set; }
    }
}
