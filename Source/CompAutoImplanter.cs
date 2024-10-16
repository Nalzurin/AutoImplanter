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

    public class CompAutoImplanter : ThingComp//, ISuspendableThingHolder, IThingHolder, IThingHolderWithDrawnPawn, IStoreSettingsParent
    {
        public CompProperties_AutoImplanter Props => props as CompProperties_AutoImplanter;

        public bool IsContentsSuspended => true;

        public float HeldPawnDrawPos_Y => parent.DrawPos.y - 1f / 26f;

        public float HeldPawnBodyAngle => parent.Rotation.Opposite.AsAngle;

        public PawnPosture HeldPawnPosture => PawnPosture.LayingOnGroundFaceUp;

        public bool StorageTabVisible => throw new NotImplementedException();

        private static readonly Texture2D IconForBodypart = ContentFinder<Texture2D>.Get("Things/Item/Health/HealthItem");
        private ThingOwner innerContainer;
        private StorageSettings allowedNutritionSettings;

       /* public CompAutoImplanter()
        {
            innerContainer = new ThingOwner<Thing>(this);
        }*/
        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return innerContainer;
        }
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

        public StorageSettings GetStoreSettings()
        {
            return allowedNutritionSettings;
        }

        public StorageSettings GetParentStoreSettings()
        {
            throw new NotImplementedException();
        }

        public void Notify_SettingsChanged()
        {
            throw new NotImplementedException();
        }
    }
}
