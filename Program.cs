using StardewModdingAPI;
using System;
using StardewModdingAPI.Events;

namespace Starbot
{
    public class Mod : StardewModdingAPI.Mod
    {
        internal static Mod instance;
        internal static Random RNG = new Random(Guid.NewGuid().GetHashCode());
        internal static bool BotActive = false;

        public override void Entry(IModHelper helper)
        {
            instance = this;

            Helper.Events.Input.ButtonPressed += Input_ButtonPressed;
            Helper.Events.GameLoop.UpdateTicked += GameLoop_UpdateTicked;
        }

        private void GameLoop_UpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!BotActive) return;
            Brain.Update();
            if (Brain.WantsToStop)
            {
                Monitor.Log("Bot is going to stop itself to prevent further complications.", LogLevel.Warn);
                ToggleBot();
            }
        }

        private void Input_ButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            bool shifting = false;
            if (e.IsDown(SButton.LeftShift)) shifting = true;
            if (e.IsDown(SButton.RightShift)) shifting = true;

            if (e.Button == SButton.B && shifting)
            {
                if (!Context.IsWorldReady && !BotActive)
                {
                    Monitor.Log("Cannot toggle bot in current game state.", LogLevel.Warn);
                    return;
                }
                Helper.Input.Suppress(SButton.B);
                ToggleBot();
            }
        }

        private void ToggleBot()
        {
            BotActive = !BotActive;
            Monitor.Log("Toggled bot status. Bot is now " + (BotActive ? "ON." : "OFF."), LogLevel.Warn);
            if (!BotActive) Brain.ReleaseKeys();
            else Brain.Reset();
        }
    }
}