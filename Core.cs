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
    public static class Core
    {
        public static bool WantsToStop = false;
        internal static InputSimulator Inputter = new InputSimulator();

        private static bool MovingDown = false;
        private static bool MovingLeft = false;
        private static bool MovingRight = false;
        private static bool MovingUp = false;

        public static bool ShouldBeMoving { get { return MovingDown || MovingLeft || MovingRight || MovingUp; } }
        private static bool IsStuck = false;
        private static int LastTileX = -3, LastTileY = -3;
        private static string LastLocationName = null;
        private static int UpdatesSinceCoordinateChange = 0;

        //Routing
        public static bool IsRouting = false;
        private static int RoutingDestinationX = -3, RoutingDestinationY = -3;
        private static bool HasRoutingDestination { get { return RoutingDestinationX != -3 && RoutingDestinationY != -3; } }
        private static List<string> Route = null;

        public static void RouteTo(string targetMap, int targetX = -3, int targetY = -3)
        {
            if (Game1.player.currentLocation.Name != targetMap)
            {
                Mod.instance.Monitor.Log("Routing to: " + targetMap + (targetY == -1 ? targetX + ", " + targetY : ""), LogLevel.Info);
                //calculate a route to the destination
                var route = Routing.GetRoute(targetMap);
                if (route == null || route.Count < 2)
                {
                    Mod.instance.Monitor.Log("Routing failed: no route!", LogLevel.Alert);
                    return;
                } /*else
                {
                    //debug, print route:
                    string routeInfo = "Route: ";
                    foreach (string s in route) routeInfo += s + ", ";
                    Mod.instance.Monitor.Log(routeInfo.Substring(0, routeInfo.Length - 2), LogLevel.Warn);
                }*/

                //route.RemoveAt(0); //remove the current map from the list

                //set the bot's route
                IsRouting = true;
                RoutingDestinationX = targetX;
                RoutingDestinationY = targetY;
                Route = route;
            } else if(targetX != -3 && targetY != -3)
            {
                RoutingDestinationX = targetX;
                RoutingDestinationY = targetY;
                PathfindTo(targetX, targetY);
            }
        }

        private static void ClearRoutingDestination()
        {
            RoutingDestinationX = -3;
            RoutingDestinationY = -3;
        }

        //call on location change
        private static void AdvanceRoute()
        {
            if (!IsRouting) return;
            Mod.instance.Monitor.Log("Advancing route...", LogLevel.Info);
            Route.RemoveAt(0); //remove the current map from the list
            if (Route.Count == 0)
            {
                //route complete
                IsRouting = false;
                Route = null;
                if(HasRoutingDestination)
                {
                    //pathfind to final destination coordinates
                    PathfindTo(RoutingDestinationX, RoutingDestinationY);
                }
            } else
            {
                //pathfind to the next map
                foreach (var w in Game1.player.currentLocation.warps)
                {
                    if (w.TargetName == Route[0])
                    {
                        PathfindTo(w.X, w.Y);
                        break;
                    }
                }
                /*  todo, doors
                    foreach (var w in Game1.player.currentLocation.doors.Values)
                    {
                        if (w == CurrentWorldRoute[0])
                        {
                            routing = true;
                            MoveToCoordinate(w.X, w.Y);
                        }
                    }
                */
            }
        }

        //Pathfinding
        private static bool IsPathfinding = false;
        private static int PathfindingDestinationX = -3, PathfindingDestinationY = -3;
        private static List<Tuple<int,int>> Path = null;

        private static void PathfindTo(int x, int y)
        {
            Mod.instance.Monitor.Log("Pathfinding to: " + x + ", " + y, LogLevel.Info);
            var path = Pathfinder.Pathfinder.FindPath(Game1.player.currentLocation, Game1.player.getTileX(), Game1.player.getTileY(), x, y);
            if (path == null)
            {
                Mod.instance.Monitor.Log("Pathfinding failed: no path!", LogLevel.Alert);
                return;
            }

            //set the bot's path
            IsPathfinding = true;
            PathfindingDestinationX = x;
            PathfindingDestinationY = y;
            Path = path;
        }

        private static void ClearPathfindingDestination()
        {
            PathfindingDestinationX = -3;
            PathfindingDestinationY = -3;
        }

        private static void AdvancePath()
        {
            if (!IsPathfinding) return;
            if (Path.Count == 0)
            {
                Path = null;
                IsPathfinding = false;
                ClearMoveTarget();
                Mod.instance.Monitor.Log("Pathfinding complete.", LogLevel.Alert);
                return;
            }
            var next = Path[0];
            Path.RemoveAt(0);
            MoveTo(next.Item1, next.Item2);
        }

        //Movement
        private static bool HasMoveTarget { get { return MoveTargetX != -3 && MoveTargetY != -3; } }
        private static int MoveTargetX = -3;
        private static int MoveTargetY = -3;

        private static void MoveTo(int x, int y)
        {
            MoveTargetX = x;
            MoveTargetY = y;
        }

        private static void ClearMoveTarget()
        {
            MoveTargetX = -3;
            MoveTargetY = -3;
        }

        private static int StopX = 0; //a delay on releasing keys so the animation isnt janky from spamming the button on and off
        private static int StopY = 0;
        private static readonly int StopDelay = 8;
        private static void AdvanceMove()
        {
            int px = Game1.player.getTileX();
            int py = Game1.player.getTileY();
            if (px < MoveTargetX) StartMovingRight();
            else if (px > MoveTargetX) StartMovingLeft();
            else
            {
                if (StopX > StopDelay)
                {
                    StopX = 0;
                    StopMovingRight();
                    StopMovingLeft();
                }
                else StopX++;
            }

            if (py < MoveTargetY) StartMovingDown();
            else if (py > MoveTargetY) StartMovingUp();
            else
            {
                if (StopY > StopDelay)
                {
                    StopY = 0;
                    StopMovingDown();
                    StopMovingUp();
                }
                else StopY++;
            }
            if(px == MoveTargetX && py == MoveTargetY)
            {
                ClearMoveTarget();
            }
        }

        public static void Reset()
        {
            ReleaseKeys();
            Routing.Reset();
            ClearMoveTarget();
            ClearPathfindingDestination();
            IsPathfinding = false;
            Path = null;
            IsRouting = false;
            ClearRoutingDestination();
            Route = null;
            LastTileX = -3;
            LastTileY = -3;
            LastLocationName = null;

            IsStuck = false;
            UpdatesSinceCoordinateChange = 0;
            WantsToStop = false;

            MovingDown = false;
            MovingLeft = false;
            MovingRight = false;
            MovingUp = false;

            RouteTo("Beach", 38, 6); //for testing
        }

        public static void Update()
        {
            //only update navigation while navigation is possible
            if (Context.CanPlayerMove)
            {
                //cache player position
                int px = Game1.player.getTileX();
                int py = Game1.player.getTileY();

                //for now, if stuck let's just shut it down
                if (IsStuck)
                {
                    WantsToStop = true;
                    return;
                }

                //on logical location change
                if(Game1.currentLocation.Name != LastLocationName)
                {
                    LastLocationName = Game1.currentLocation.Name;
                    ClearMoveTarget();
                    ReleaseKeys();
                    if(IsRouting) AdvanceRoute();
                }

                //if we don't have a move target, check the path for one
                if (!HasMoveTarget && IsPathfinding)
                {
                    //is pathfinding complete?
                    if(px == PathfindingDestinationX && py == PathfindingDestinationY)
                    {
                        IsPathfinding = false;
                        Path = null;
                        ClearPathfindingDestination();
                        
                        //check for route destination and announce
                        if(HasRoutingDestination && px == RoutingDestinationX && py == RoutingDestinationY)
                        {
                            Mod.instance.Monitor.Log("Routing complete.", LogLevel.Alert);
                            ClearMoveTarget();
                            ClearPathfindingDestination();
                            ClearRoutingDestination();
                            ReleaseKeys();
                        }
                    } else
                    {
                        //move to the next node in the path
                        AdvancePath();
                    }
                }

                if (HasMoveTarget)
                {
                    AdvanceMove();
                }


                //stuck detection
                if (ShouldBeMoving)
                {
                    //on logical tile change
                    if (px != LastTileX || py != LastTileY)
                    {
                        LastTileX = px;
                        LastTileY = py;
                        UpdatesSinceCoordinateChange = 0;
                    }
                    UpdatesSinceCoordinateChange++;
                    if (!IsStuck && UpdatesSinceCoordinateChange > 60 * 5) OnStuck();
                }
            }
        }

        private static void OnStuck()
        {
            IsStuck = true;
            Mod.instance.Monitor.Log("Bot is stuck.", LogLevel.Info);
            UpdatesSinceCoordinateChange = 0;
            ReleaseKeys();
        }

        private static void StartMovingDown()
        {
            if (MovingUp) StopMovingUp();
            if(!MovingDown) KeyDown(Keys.S);
            MovingDown = true;
        }

        private static void StopMovingDown()
        {
            if(MovingDown) KeyUp(Keys.S);
            MovingDown = false;
        }

        private static void StartMovingLeft()
        {
            if (MovingRight) StopMovingRight();
            if (!MovingLeft) KeyDown(Keys.A);
            MovingLeft = true;
        }

        private static void StopMovingLeft()
        {
            if (MovingLeft) KeyUp(Keys.A);
            MovingLeft = false;
        }

        private static void StartMovingUp()
        {
            if (MovingDown) StopMovingDown();
            if (!MovingUp) KeyDown(Keys.W);
            MovingUp = true;
        }

        private static void StopMovingUp()
        {
            if (MovingUp) KeyUp(Keys.W);
            MovingUp = false;
        }

        private static void StartMovingRight()
        {
            if (MovingLeft) StopMovingLeft();
            if (!MovingRight) KeyDown(Keys.D);
            MovingRight = true;
        }

        private static void StopMovingRight()
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

        private static void KeyDown(Keys key)
        {
            Inputter.Keyboard.KeyDown((WindowsInput.Native.VirtualKeyCode) key);
        }

        private static void KeyUp(Keys key)
        {
            Inputter.Keyboard.KeyUp((WindowsInput.Native.VirtualKeyCode) key);
        }
    }
}
