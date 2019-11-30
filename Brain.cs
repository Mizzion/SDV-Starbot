using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowsInput;

namespace Starbot
{
    public static class Brain
    {
        public static bool WantsToStop = false;
        internal static InputSimulator Inputter = new InputSimulator();
        public static int TileX = -1;
        public static int TileY = -1;
        public static string LocationName = null;

        private static bool MovingDown = false;
        private static bool MovingLeft = false;
        private static bool MovingRight = false;
        private static bool MovingUp = false;
        public static bool ShouldBeMoving { get { return MovingDown || MovingLeft || MovingRight || MovingUp; } }
        private static bool Stuck = false;
        private static int UpdatesSinceCoordinateChange = 0;

        private static List<string> CurrentWorldRoute = null;
        private static int MoveTargetTileX = -3;
        private static int MoveTargetTileY = -3;

        private static Dictionary<string, HashSet<string>> MapConnections = new Dictionary<string, HashSet<string>>();

        public static void Reset()
        {
            Stuck = false;
            UpdatesSinceCoordinateChange = 0;
            MovingDown = false;
            MovingLeft = false;
            MovingRight = false;
            MovingUp = false;
            LocationName = Game1.player.currentLocation.Name;
            TileX = -1;
            TileY = -1;
            WantsToStop = false;        
            CurrentWorldRoute = null;
            MoveTargetTileX = -3;
            MoveTargetTileY = -3;

            LearnMapConnections();
            NavigateToMap("Beach"); //for testing
        }

        public static void NavigateToMap(string target)
        {
            Mod.instance.Monitor.Log("Navigating to map: " + target, LogLevel.Alert);
            var route = RouteFind(target);
            if(route == null || route.Count == 0)
            {
                Mod.instance.Monitor.Log("Navigation failed: no route!", LogLevel.Alert);
                return;
            }
            route.RemoveAt(0); //remove the current map from the list
            if(route.Count == 0)
            {
                OnNavigationComplete();
                return;
            }
            CurrentWorldRoute = route;
            OnLocationChange(LocationName);
        }

        public static void OnNavigationComplete()
        {
            Mod.instance.Monitor.Log("Navigation to " + LocationName + " complete.", LogLevel.Alert);
            CurrentWorldRoute = null;
            MoveTargetTileX = -3;
            MoveTargetTileY = -3;
            ReleaseKeys();
        }

        public static void MoveToCoordinate(int x, int y)
        {
            Mod.instance.Monitor.Log("Moving to: " + x + ", " + y, LogLevel.Info);
            var path = Pathfinder.Pathfinder.FindPath(Game1.player.currentLocation, Game1.player.getTileX(), Game1.player.getTileY(), x, y);
            foreach(var p in path)
            {
                Mod.instance.Monitor.Log("Path node: " + p.Item1 + ", " + p.Item2);
            }
            //MoveTargetTileX = x;
            //MoveTargetTileY = y;
        }

        public static void OnLocationChange(string oldLocation)
        {
            Mod.instance.Monitor.Log("Bot location changed: " + LocationName, LogLevel.Info);
            
            //navigation update
            if(CurrentWorldRoute != null && CurrentWorldRoute.Count > 0)
            {
                if (LocationName == CurrentWorldRoute[0]) CurrentWorldRoute.RemoveAt(0);
                if (CurrentWorldRoute.Count > 0)
                {
                    //look for the next warp and MoveToCoordinate it
                    //todo: pathfinding
                    bool routing = false;
                    foreach (var w in Game1.player.currentLocation.warps)
                    {
                        if (w.TargetName == CurrentWorldRoute[0])
                        {
                            routing = true;
                            MoveToCoordinate(w.X, w.Y);
                        }
                    }
                    /*
                    if (!routing)
                    {
                        foreach (var w in Game1.player.currentLocation.doors.Values)
                        {
                            if (w == CurrentWorldRoute[0])
                            {
                                routing = true;
                                MoveToCoordinate(w.X, w.Y);
                            }
                        }
                    }
                    */
                } else
                {
                    OnNavigationComplete();
                }
            }
        }

        public static void OnCoordinateChange(int oldX, int oldY)
        {
            Mod.instance.Monitor.Log("Bot coordinate changed: " + TileX + ", " + TileY, LogLevel.Info);
            if((MoveTargetTileX > -3 || MoveTargetTileY > -3) && (TileX == MoveTargetTileX && TileY == MoveTargetTileY))
            {
                MoveTargetTileX = -3;
                MoveTargetTileY = -3;
                Mod.instance.Monitor.Log("Bot move completed!", LogLevel.Info);
            }
        }

        private static void LearnMapConnections()
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
                        foreach(var w in gl.warps) MapConnections[key].Add(w.TargetName);
                        foreach(var d in gl.doors.Values) MapConnections[key].Add(d);
                        //foreach (var s in MapConnections[key]) Mod.instance.Monitor.Log("   It connects to " + s, LogLevel.Warn);
                    }
                }
            }
        }

        private static List<string> RouteFind(string destination)
        {
            return RouteFind(LocationName, destination);
        }

        private static List<string> RouteFind(string start, string destination)
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
            foreach(string s in MapConnections[step])
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

        public static void Update()
        {
            if (Stuck) WantsToStop = true; //temp

            if (Context.CanPlayerMove)
            {
                if (MoveTargetTileX > -3 || MoveTargetTileY > -3)
                {
                    //should be moving
                    if (TileX < MoveTargetTileX) StartMovingRight();
                    else if (TileX > MoveTargetTileX) StartMovingLeft();
                    else
                    {
                        StopMovingRight();
                        StopMovingLeft();
                    }

                    if (TileY < MoveTargetTileY) StartMovingDown();
                    else if (TileY > MoveTargetTileY) StartMovingUp();
                    else
                    {
                        StopMovingDown();
                        StopMovingUp();
                    }
                }
            }
            //check x/y/location coordinates for changes
            int newX = Game1.player.getTileX();
            int newY = Game1.player.getTileY();
            string newLocation = Game1.player.currentLocation.Name;
            if(newLocation != LocationName)
            {
                string oldLocation = LocationName;
                LocationName = newLocation;
                OnLocationChange(oldLocation);
            }
            if (newX != TileX || newY != TileY)
            {
                int oldX = TileX;
                int oldY = TileY;
                TileX = newX;
                TileY = newY;
                OnCoordinateChange(oldX, oldY);
                UpdatesSinceCoordinateChange = 0;
            }
            else if(ShouldBeMoving) UpdatesSinceCoordinateChange++;

            if (ShouldBeMoving && !Stuck && UpdatesSinceCoordinateChange > 60 * 5) OnStuck();
        }

        public static void OnStuck()
        {
            Stuck = true;
            Mod.instance.Monitor.Log("Bot is stuck.", LogLevel.Info);
            UpdatesSinceCoordinateChange = 0;
            ReleaseKeys();
        }

        public static void StartMovingDown()
        {
            if (MovingUp) StopMovingUp();
            if(!MovingDown) KeyDown(Keys.S);
            MovingDown = true;
        }

        public static void StopMovingDown()
        {
            if(MovingDown) KeyUp(Keys.S);
            MovingDown = false;
        }

        public static void StartMovingLeft()
        {
            if (MovingRight) StopMovingRight();
            if (!MovingLeft) KeyDown(Keys.A);
            MovingLeft = true;
        }

        public static void StopMovingLeft()
        {
            if (MovingLeft) KeyUp(Keys.A);
            MovingLeft = false;
        }

        public static void StartMovingUp()
        {
            if (MovingDown) StopMovingDown();
            if (!MovingUp) KeyDown(Keys.W);
            MovingUp = true;
        }

        public static void StopMovingUp()
        {
            if (MovingUp) KeyUp(Keys.W);
            MovingUp = false;
        }

        public static void StartMovingRight()
        {
            if (MovingLeft) StopMovingLeft();
            if (!MovingRight) KeyDown(Keys.D);
            MovingRight = true;
        }

        public static void StopMovingRight()
        {
            if (MovingRight) KeyUp(Keys.D);
            MovingRight = false;
        }

        public static void ReleaseKeys()
        {
            StopMovingDown();
            StopMovingLeft();
            StopMovingRight();
            StopMovingUp();
        }

        public static void KeyDown(Keys key)
        {
            Inputter.Keyboard.KeyDown((WindowsInput.Native.VirtualKeyCode) key);
        }

        public static void KeyUp(Keys key)
        {
            Inputter.Keyboard.KeyUp((WindowsInput.Native.VirtualKeyCode) key);
        }
    }
}
