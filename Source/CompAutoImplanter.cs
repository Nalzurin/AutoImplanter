using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static System.Net.Mime.MediaTypeNames;

namespace AutoImplanter
{
    public class CompProperties_AutoImplanter : CompProperties
    {
        public CompProperties_AutoImplanter()
        {
            compClass = typeof(CompAutoImplanter);
        }
    }
    [StaticConstructorOnStartup]

    public class CompAutoImplanter : ThingComp
    {
        public CompProperties_AutoImplanter Props => props as CompProperties_AutoImplanter;
        private static readonly Texture2D IconForBodypart = ContentFinder<Texture2D>.Get("Things/Item/Health/HealthItem");
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            Command_Action command_Action = new Command_Action();
            command_Action.defaultLabel = "Implanter Presets";
            command_Action.defaultDesc = "Manage implanter presets";
            command_Action.icon = IconForBodypart;
            command_Action.action = delegate
            {
                Dialog_AutoImplanterPreset dialog = new Dialog_AutoImplanterPreset();
                Find.WindowStack.Add(dialog);
            };
            command_Action.activateSound = SoundDefOf.Tick_Tiny;
            yield return command_Action;
        }
    }
}
