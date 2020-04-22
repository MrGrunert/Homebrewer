using System.Collections.Generic;

namespace BB2
{
    //public class exportData
    //{
    //    public string viktMalt;
    //    public string viktHumle;

    //    public Dictionary<string, decimal> maltDict = new Dictionary<string, decimal>();
    //}

    public class Malt
    {
        public string Grain;
        public decimal Potential;
        public Dictionary<string, decimal> maltDict = new Dictionary<string, decimal>();
    }
}
