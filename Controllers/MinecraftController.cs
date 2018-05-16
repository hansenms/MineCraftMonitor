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
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.JsonPatch;
using Newtonsoft.Json;

namespace MineCraftMonitor.Controllers
{
    [Route("api/[controller]")]
    public class MinecraftController : Controller
    {


        public MinecraftController(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // GET api/minecraft
        [HttpGet]
        public MineCraftSummary Get()
        {
            KubernetesClientConfiguration config;
            if (Configuration["ASPNETCORE_ENVIRONMENT"] == "Debug") {
                config = KubernetesClientConfiguration.BuildConfigFromConfigFile();
            } else {
                config = KubernetesClientConfiguration.InClusterConfig();
            }
            IKubernetes client = new Kubernetes(config);

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

        // GET api/minscraft/instance/{id}
        [HttpGet("instance/count")]
        public string GetInstanceCount()
        {
            KubernetesClientConfiguration config;
            if (Configuration["ASPNETCORE_ENVIRONMENT"] == "Debug") {
                config = KubernetesClientConfiguration.BuildConfigFromConfigFile();
            } else {
                config = KubernetesClientConfiguration.InClusterConfig();
            }
            IKubernetes client = new Kubernetes(config);

            var list = client.ListNamespacedPod("default");

            var output = new List<MineCraftInstance>();

            int count = 0;
            foreach (var item in list.Items)
            {
                if (item.Metadata.Name.StartsWith("minecraft-"))
                {
                    count++;
                }
            }

            return count.ToString();
        }

                // PUT api/values/5
        [HttpPut("instance/count/{count}")]
        public string Put(int count)
        {
            string output = "";
        
            if (count > 0 && count < 10) {
                KubernetesClientConfiguration config;
                if (Configuration["ASPNETCORE_ENVIRONMENT"] == "Debug") {
                    config = KubernetesClientConfiguration.BuildConfigFromConfigFile();
                } else {
                    config = KubernetesClientConfiguration.InClusterConfig();
                }
                IKubernetes client = new Kubernetes(config);

                JsonPatchDocument<V1StatefulSet> jsonDoc = new JsonPatchDocument<V1StatefulSet>();
                jsonDoc.Replace(p => p.Spec.Replicas, count);
                Console.WriteLine(JsonConvert.SerializeObject(jsonDoc));
                V1Patch patch = new V1Patch(jsonDoc);
                client.PatchNamespacedStatefulSetScale(patch,"minecraft","default");
            }

            //This is where we update the count.
            return output;
        }


        // GET api/minscraft/instance/{id}
        [HttpGet("instance")]
        public List<MineCraftInstance> GetInstance()
        {
            KubernetesClientConfiguration config;
            if (Configuration["ASPNETCORE_ENVIRONMENT"] == "Debug") {
                config = KubernetesClientConfiguration.BuildConfigFromConfigFile();
            } else {
                config = KubernetesClientConfiguration.InClusterConfig();
            }
            IKubernetes client = new Kubernetes(config);

            var list = client.ListNamespacedPod("default");

            var output = new List<MineCraftInstance>();

            foreach (var item in list.Items)
            {
                if (item.Metadata.Name.StartsWith("minecraft-"))
                {
                    MineCraftInstance instance = new MineCraftInstance();
                    instance.name = item.Metadata.Name;
                    instance.endpoints = new MineCraftEndpoint();
                    instance.endpoints.minecraft = item.Status.PodIP.ToString() + ":25565";
                    instance.endpoints.rcon = item.Status.PodIP.ToString() + ":25565";
                    instance.endpoints.monitor = item.Status.PodIP.ToString() + ":5000";
                    output.Add(instance);
                }
            }
            return output;
        }

    }
}
