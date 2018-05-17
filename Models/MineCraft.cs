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

    public class MineCraftEndpoint
    {
        public string minecraft {get; set;}
        public string rcon { get; set; }
        public string monitor { get; set; }
    }
    public class MineCraftInstance
    {
        public string name { get; set; }
        public MineCraftEndpoint endpoints {get; set;}
    }

    public class NodeStat
    {
        public string name {get; set; }
        public int cpuCores {get; set;}
        public int cpuPercent {get; set;}
        public int memBytes {get; set;}
        public int memPercent {get; set;}

    }
    public class KubernetesTop
    {
        public int avgCpuPercent {get; set;}
        public int avgMemPercent {get; set; }
        
        public List<NodeStat> nodes { get; set; }
    }
}