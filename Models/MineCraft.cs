using System.Collections.Generic;

namespace MineCraftMonitor.Models
{
    public class MineCraftServerStats
    {
        public int playersOnline { get; set; }
        public int maxPlayers { get; set; }
        public int population { get; set; }
    }

    public class MineCraftServer
    {
        public string name {get; set;}
        public MineCraftServerStats stats { get; set; }
    }

    public class MineCraftSummary
    {
        public MineCraftServerStats totals { get; set; }

        public List<MineCraftServer> servers { get; set; }
    }
}