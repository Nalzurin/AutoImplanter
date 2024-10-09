using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace AutoImplanter
{
    public class AutoImplanter_Mod : Mod
    {
        public static AutoImplanter_Settings settings;

        public AutoImplanter_Mod(ModContentPack content)
            : base(content)
        {
            settings = GetSettings<AutoImplanter_Settings>();
        }

        public override string SettingsCategory()
        {
            return "Auto Implanter";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            settings.DoWindowContents(inRect);
        }
    }
}
