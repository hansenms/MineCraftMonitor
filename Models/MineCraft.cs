using System.Collections.Generic;

namespace MineCraftMonitor.Models
{
    public class MineCraftServer
    {
        public int playersOnline { get; set; }
        public int maxPlayers { get; set; }
        public int population { get; set; }
    }

    public class MineCraftSummary
    {
        public MineCraftServer totals { get; set; }

        public List<MineCraftServer> servers { get; set; }
    }
}