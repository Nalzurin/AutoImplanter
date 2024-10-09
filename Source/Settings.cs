using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace AutoImplanter
{
    public class AutoImplanter_Settings : ModSettings
    {
        public static List<AutoImplanterPreset> ImplanterPresets = [];

        public override void ExposeData()
        {

            Scribe_Collections.Look(ref ImplanterPresets, "ImplanterPresets", LookMode.Deep);
            base.ExposeData();
        }
        public void DebugDeletePresets()
        {
            AutoImplanter_Settings.ImplanterPresets = [];
            LoadedModManager.GetMod(typeof(AutoImplanter_Mod)).WriteSettings();
        }
        public void DoWindowContents(Rect inRect)
        {
            float buffer = 70f;
            Rect rect2 = inRect;
            rect2.height = buffer;
            if(Widgets.ButtonText(rect2, "Delete Presets"))
            {
                ImplanterPresets = [];
                Write();
            }
            Rect rect = inRect;
            rect.y = buffer;
            rect.height -= buffer / 2;
            Dialog_AutoImplanterPreset dialog = new Dialog_AutoImplanterPreset();
            dialog.DoWindowContents(rect);
        }
    }
}
