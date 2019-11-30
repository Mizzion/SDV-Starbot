using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starbot
{

    public static class Routing
    {
        private static Dictionary<string, HashSet<string>> MapConnections = new Dictionary<string, HashSet<string>>();

        public static void Reset()
        {
            MapConnections.Clear();
            //let's learn about the world. warps and doors for now.
            foreach (var gl in Game1.locations)
            {
                string key = gl.NameOrUniqueName;
                if (!string.IsNullOrWhiteSpace(key) && !gl.isTemp())
                {
                    if (gl.warps != null && gl.warps.Count > 0)
                    {
                        //Mod.instance.Monitor.Log("Learning about " + key, LogLevel.Alert);
                        MapConnections[key] = new HashSet<string>();
                        foreach (var w in gl.warps) MapConnections[key].Add(w.TargetName);
                        //foreach (var d in gl.doors.Values) MapConnections[key].Add(d);
                        //foreach (var s in MapConnections[key]) Mod.instance.Monitor.Log("It connects to " + s, LogLevel.Warn);
                    }
                }
            }
        }

        public static List<string> GetRoute(string destination)
        {
            return GetRoute(Game1.player.currentLocation.Name, destination);
        }

        public static List<string> GetRoute(string start, string destination)
        {
            var result = SearchRoute(start, destination);
            if (result != null) result.Add(destination);
            return result;
        }

        private static List<string> SearchRoute(string step, string target, List<string> route = null, List<string> blacklist = null)
        {
            if (route == null) route = new List<string>();
            if (blacklist == null) blacklist = new List<string>();
            List<string> route2 = new List<string>(route);
            route2.Add(step);
            foreach (string s in MapConnections[step])
            {
                if (route.Contains(s) || blacklist.Contains(s)) continue;
                if (s == target)
                {
                    return route2;
                }
                List<string> result = SearchRoute(s, target, route2, blacklist);
                if (result != null) return result;
            }
            blacklist.Add(step);
            return null;
        }
    }
}
