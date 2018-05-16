using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using k8s;
using k8s.Models;
using System.Diagnostics;
using System.IO;
using MineCraftMonitor.Models;
using System.Text.RegularExpressions;

namespace MineCraftMonitor.Controllers
{
    [Route("api/[controller]")]
    public class MinecraftController : Controller
    {
        // GET api/minecraft
        [HttpGet]
        public MineCraftSummary Get()
        {
            var config = KubernetesClientConfiguration.InClusterConfig();
            //var config = KubernetesClientConfiguration.BuildConfigFromConfigFile();
            IKubernetes client = new Kubernetes(config);
            Console.WriteLine("Starting Request!");

            var list = client.ListNamespacedPod("default");

            MineCraftSummary summary = new MineCraftSummary();
            summary.totals = new MineCraftServerStats();
            summary.servers = new List<MineCraftServer>();

            foreach (var item in list.Items)
            {
                if (item.Metadata.Name.StartsWith("minecraft-"))
                {

                    Process process = new Process();
                    process.StartInfo.FileName = "kubectl";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.Arguments = "exec -t -c minecraft " + item.Metadata.Name + " -- ls -1 world/playerdata/";
                    process.Start();

                    // Synchronously read the standard output of the spawned process. 
                    StreamReader reader = process.StandardOutput;
                    string output = reader.ReadToEnd();
                    int numLines = output.Split('\n').Length - 1;

                    MineCraftServer srv = new MineCraftServer();
                    srv.name = item.Metadata.Name;
                    srv.stats = new MineCraftServerStats();
                    srv.stats.population = numLines;

                    process = new Process();
                    process.StartInfo.FileName = "rcon-cli";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.Arguments = "--host " + item.Status.PodIP.ToString() + " --port 25575 --password cheesesteakjimmys list";
                    process.Start();

                    // Synchronously read the standard output of the spawned process. 
                    reader = process.StandardOutput;
                    string rconout = reader.ReadToEnd();

                    //root@minemon:/app# rcon-cli --host 10.244.0.20 --port 25575 --password cheesesteakjimmys list
                    //string rconout = "There are 1/20 players online:CloudyNerd";
                    string[] numbers = Regex.Split(rconout, @"\D+");

                    srv.stats.playersOnline = int.Parse(numbers[1]);
                    srv.stats.maxPlayers = int.Parse(numbers[2]);

                    summary.servers.Add(srv);
                    summary.totals.population += srv.stats.population;
                    summary.totals.maxPlayers += srv.stats.maxPlayers;
                    summary.totals.playersOnline += srv.stats.playersOnline;
                }

                Console.WriteLine(item.Metadata.Name);
            }

            return summary;
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody]string value)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }

        private async static Task ExecInPod(IKubernetes client, V1Pod pod)
        {
            var webSocket = await client.WebSocketNamespacedPodExecAsync(pod.Metadata.Name, "default", "ls", pod.Spec.Containers[0].Name);

            var demux = new StreamDemuxer(webSocket);
            demux.Start();

            var buff = new byte[4096];
            var stream = demux.GetStream(1, 1);
            var read = stream.Read(buff, 0, 4096);
            var str = System.Text.Encoding.Default.GetString(buff);
            Console.WriteLine(str);
        }
    }
}
