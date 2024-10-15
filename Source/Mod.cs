using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace AutoImplanter
{
    [StaticConstructorOnStartup]
    public class AutoImplanter_Mod : Mod
    {
        private static AutoImplanter_Settings settings;
        public static AutoImplanter_Mod instance;


        public static AutoImplanter_Settings Settings => settings ??= instance.GetSettings<AutoImplanter_Settings>();
        public AutoImplanter_Mod(ModContentPack content) : base(content) => instance = this;

        public override string SettingsCategory()
        {
            return "Auto Implanter";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            float buffer = 70f;
            Rect rect2 = inRect;
            rect2.height = buffer;
            if (Widgets.ButtonText(rect2, "Delete Presets"))
            {
                DebugDeletePresets();
            }
        }
        public void DebugDeletePresets()
        {
            Settings.ImplanterPresets.Clear();
            WriteSettings();

        }

    }
}
