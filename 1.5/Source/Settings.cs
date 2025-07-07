using RimWorld;
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
        public List<AutoImplanterPreset> ImplanterPresets = new List<AutoImplanterPreset>();
        public List<AutoImplanterPreset> ImplanterPresetsForReading => ImplanterPresets;
        public float SurgerySpeedModifier = 1f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref ImplanterPresets, "ImplanterPresets", LookMode.Deep);
            Scribe_Values.Look(ref SurgerySpeedModifier, "SurgerySpeedModifier", 1f);
        }

    }
}
