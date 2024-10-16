using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
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
            Listing_Standard listing_Standard = new Listing_Standard();
            listing_Standard.Begin(inRect);
            listing_Standard.Label($"Surgery Speed Modifier (70% means the surgery takes 70% of original time): {Settings.SurgerySpeedModifier * 100}%");
            Settings.SurgerySpeedModifier = (float)Math.Round((double)listing_Standard.Slider(Settings.SurgerySpeedModifier, 0f, 2f), 2);
            if (listing_Standard.ButtonText("Delete Presets"))
            {
                DebugDeletePresets();
            }
            listing_Standard.End();
        }
        public void DebugDeletePresets()
        {
            AutoImplanter_Mod.Settings.ImplanterPresets.Clear();
            WriteSettings();

        }

    }
}
