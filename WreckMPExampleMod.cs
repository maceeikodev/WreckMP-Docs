using MSCLoader;
using UnityEngine;

namespace WreckMPExampleMod
{
    public class WreckMPExampleMod : Mod
    {
        public override string ID => "WreckMPExampleMod"; //Your mod ID (unique)
        public override string Name => "WreckMP example mod"; //You mod name
        public override string Author => "Maceeiko"; //Your Username
        public override string Version => "1.0"; //Version
        public override string Description => ""; //Short description of your mod

        public override void ModSetup()
        {
            SetupFunction(Setup.OnLoad, Mod_OnLoad);
        }

        public override void ModSettings()
        {
            // All settings should be created here. 
            // DO NOT put anything else here that settings or keybinds
        }

        private void Mod_OnLoad()
        {
            // Called once, when mod is loading after game is fully loaded
        }
    }
}
