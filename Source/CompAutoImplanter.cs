using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Noise;
using Verse.Sound;
using static System.Net.Mime.MediaTypeNames;
using static UnityEngine.Random;

namespace AutoImplanter
{

    public enum AutoImplanterState
    {
        Inactive,
        WaitingForIngredients,
        WaitingForOccupant,
        Occupied,
        Halted
    }

    [StaticConstructorOnStartup]
    public class Building_AutoImplanter : Building_Enterable, IThingHolderWithDrawnPawn, IThingHolder
    {
        private bool initScanner;

        private int fabricationTicksLeft;
        private int deathTicksLeft;

        private Effecter effectStart;

        private Effecter effectHusk;

        private bool debugDisableNeedForIngredients;

        private Mote workingMote;

        private Sustainer sustainerWorking;

        private Effecter progressBarEffecter;

        public static readonly Texture2D CancelLoadingIcon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel");


        //private static readonly Texture2D IconForBodypart = ContentFinder<Texture2D>.Get("Things/Item/Health/HealthItem");
        public static readonly Texture2D InsertPersonIcon = ContentFinder<Texture2D>.Get("UI/Icons/AutoImplanter_InsertPawn");
        public static readonly Texture2D ManagePresets = ContentFinder<Texture2D>.Get("UI/Icons/AutoImplanter_MakePreset");
        public static readonly Texture2D SelectPreset = ContentFinder<Texture2D>.Get("UI/Icons/AutoImplanter_PresetPicker");
        public static readonly Texture2D StartAutoImplant = ContentFinder<Texture2D>.Get("UI/Icons/AutoImplanter_StartAutoImplant");
        private static Dictionary<Rot4, ThingDef> MotePerRotation;

        private static Dictionary<Rot4, ThingDef> MotePerRotationRip;

        private static readonly Dictionary<Rot4, Vector3> HuskEffectOffsets = new Dictionary<Rot4, Vector3>
    {
        {
            Rot4.North,
            new Vector3(0f, 0f, 0.47f)
        },
        {
            Rot4.South,
            new Vector3(0f, 0f, -0.3f)
        },
        {
            Rot4.East,
            new Vector3(0.4f, 0f, -0.025f)
        },
        {
            Rot4.West,
            new Vector3(-0.4f, 0f, -0.025f)
        }
    };
        private const int TicksToDie = 6000;
        private const float ProgressBarOffsetZ = -0.8f;

        public CachedTexture InitScannerIcon = new CachedTexture("UI/Icons/SubcoreScannerStart");

        public float HeldPawnDrawPos_Y => DrawPos.y + 1f / 26f;

        public float HeldPawnBodyAngle => base.Rotation.AsAngle;

        public PawnPosture HeldPawnPosture => PawnPosture.LayingOnGroundFaceUp;

        public bool PowerOn => this.TryGetComp<CompPowerTrader>().PowerOn;

        public override Vector3 PawnDrawOffset => Vector3.zero;
        private AutoImplanterPreset preset;
        private List<IngredientCount> ingredients;
        public Pawn Occupant
        {
            get
            {
                for (int i = 0; i < innerContainer.Count; i++)
                {
                    if (innerContainer[i] is Pawn result)
                    {
                        return result;
                    }
                }
                return null;
            }
        }

        public bool AllRequiredIngredientsLoaded
        {

            get
            {
                if (!debugDisableNeedForIngredients)
                {
                    for (int i = 0; i < ingredients.Count(); i++)
                    {
                        if (GetRequiredCountOf(ingredients[i].FixedIngredient) > 0)
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
        }

        public AutoImplanterState State
        {
            get
            {
                if (Occupant != null && !PowerOn)
                {
                    return AutoImplanterState.Halted;
                }
                if (!initScanner || !PowerOn)
                {
                    return AutoImplanterState.Inactive;
                }
                if (!AllRequiredIngredientsLoaded)
                {
                    return AutoImplanterState.WaitingForIngredients;
                }
                if (Occupant == null)
                {
                    return AutoImplanterState.WaitingForOccupant;
                }

                return AutoImplanterState.Occupied;
            }
        }


        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (ingredients != null)
            {
                for (int i = 0; i < ingredients.Count(); i++)
                {
                    ingredients[i].ResolveReferences();
                }
            }

        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            progressBarEffecter?.Cleanup();
            progressBarEffecter = null;
            effectHusk?.Cleanup();
            effectHusk = null;
            effectStart?.Cleanup();
            effectStart = null;
            if (Occupant != null)
            {
                KillOccupant();
            }
            base.DeSpawn(mode);
        }

        public int GetRequiredCountOf(ThingDef thingDef)
        {
            for (int i = 0; i < ingredients.Count(); i++)
            {
                if (ingredients[i].FixedIngredient == thingDef)
                {
                    int num = innerContainer.TotalStackCountOfDef(ingredients[i].FixedIngredient);
                    return (int)ingredients[i].GetBaseCount() - num;
                }
            }
            return 0;
        }


        public override AcceptanceReport CanAcceptPawn(Pawn selPawn)
        {
            if (!selPawn.IsColonist && !selPawn.IsSlaveOfColony && !selPawn.IsPrisonerOfColony)
            {
                return false;
            }
            if (selectedPawn != null && selectedPawn != selPawn)
            {
                return false;
            }
            if (!PowerOn)
            {
                return "CannotUseNoPower".Translate();
            }
            if (State != AutoImplanterState.WaitingForOccupant)
            {
                switch (State)
                {
                    case AutoImplanterState.Inactive:
                        return "SubcoreScannerNotInit".Translate();
                    case AutoImplanterState.WaitingForIngredients:
                        {
                            StringBuilder stringBuilder = new StringBuilder("SubcoreScannerRequiresIngredients".Translate() + ": ");
                            bool flag = false;
                            for (int i = 0; i < ingredients.Count(); i++)
                            {
                                IngredientCount ingredientCount = ingredients[i];
                                int num = innerContainer.TotalStackCountOfDef(ingredientCount.FixedIngredient);
                                int num2 = (int)ingredientCount.GetBaseCount();
                                if (num < num2)
                                {
                                    if (flag)
                                    {
                                        stringBuilder.Append(", ");
                                    }
                                    stringBuilder.Append($"{ingredientCount.FixedIngredient.LabelCap} x{num2 - num}");
                                    flag = true;
                                }
                            }
                            return stringBuilder.ToString();
                        }
                    case AutoImplanterState.Occupied:
                        return "SubcoreScannerOccupied".Translate();
                }
            }
            else
            {
                if (selPawn.IsQuestLodger())
                {
                    return "CryptosleepCasketGuestsNotAllowed".Translate();
                }
                if (selPawn.DevelopmentalStage.Baby())
                {
                    return "SubcoreScannerBabyNotAllowed".Translate();
                }
            }
            return true;
        }

        public override void TryAcceptPawn(Pawn pawn)
        {
            if ((bool)CanAcceptPawn(pawn))
            {
                bool num = pawn.DeSpawnOrDeselect();
                if (pawn.holdingOwner != null)
                {
                    pawn.holdingOwner.TryTransferToContainer(pawn, innerContainer);
                }
                else
                {
                    innerContainer.TryAdd(pawn);
                }
                if (num)
                {
                    Find.Selector.Select(pawn, playSound: false, forceDesignatorDeselect: false);
                }
                fabricationTicksLeft = (int)preset.GetWorkRequired();
                deathTicksLeft = TicksToDie; 
            }
        }

        public bool CanAcceptIngredient(Thing thing)
        {
            return GetRequiredCountOf(thing.def) > 0;
        }

        public void CancelProcess()
        {
            KillOccupant();
            EjectItems();
            EjectContents();
        }
        public void EjectItems()
        {
            for (int num = innerContainer.Count - 1; num >= 0; num--)
            {
                if (!(innerContainer[num] is Pawn) || !(innerContainer[num] is Corpse))
                {
                    innerContainer.TryDrop(innerContainer[num], InteractionCell, base.Map, ThingPlaceMode.Near, 1, out var _);
                }
            }
        }
        public void EjectContents()
        {
            Pawn occupant = Occupant;
            if (occupant == null)
            {
                innerContainer.TryDropAll(InteractionCell, base.Map, ThingPlaceMode.Near);
            }
            else
            {

                occupant.health?.AddHediff(HediffDefOf.Anesthetic);
                AutoImplanter_Helper.applyImplantPreset(preset, occupant);
                for (int num = innerContainer.Count - 1; num >= 0; num--)
                {
                    if (innerContainer[num] is Pawn || innerContainer[num] is Corpse)
                    {
                        innerContainer.TryDrop(innerContainer[num], InteractionCell, base.Map, ThingPlaceMode.Near, 1, out var _);
                    }
                }
                innerContainer.ClearAndDestroyContents();
            }
            selectedPawn = null;
            initScanner = false;
        }

        private void KillOccupant()
        {
            Pawn occupant = Occupant;
            DamageInfo dinfo = new DamageInfo(DamageDefOf.SurgicalCut, 9999f, 999f, -1f, null, occupant.health.hediffSet.GetBrain());
            dinfo.SetIgnoreInstantKillProtection(ignore: true);
            dinfo.SetAllowDamagePropagation(val: false);
            occupant.forceNoDeathNotification = true;
            occupant.TakeDamage(dinfo);
            occupant.forceNoDeathNotification = false;
            //ThoughtUtility.GiveThoughtsForPawnExecuted(occupant, null, PawnExecutionKind.Ripscanned);
            //Messages.Message("MessagePawnKilledRipscanner".Translate(occupant.Named("PAWN")), occupant, MessageTypeDefOf.NegativeHealthEvent);
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
        {
            foreach (FloatMenuOption floatMenuOption in base.GetFloatMenuOptions(selPawn))
            {
                yield return floatMenuOption;
            }
            if (!selPawn.CanReach(this, PathEndMode.InteractionCell, Danger.Deadly))
            {
                yield return new FloatMenuOption("CannotEnterBuilding".Translate(this) + ": " + "NoPath".Translate().CapitalizeFirst(), null);
                yield break;
            }
            AcceptanceReport acceptanceReport = CanAcceptPawn(selPawn);
            if (acceptanceReport.Accepted)
            {
                yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("EnterBuilding".Translate(this), delegate
                {

                    Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("ConfirmAutoImplantation".Translate(selPawn.Named("PAWN")), delegate
                    {
                        SelectPawn(selPawn);
                    }, destructive: true));

                }), selPawn, this);
            }
            else if (!acceptanceReport.Reason.NullOrEmpty())
            {
                yield return new FloatMenuOption("CannotEnterBuilding".Translate(this) + ": " + acceptanceReport.Reason.CapitalizeFirst(), null);
            }
        }

        public static bool WasLoadingCancelled(Thing thing)
        {
            if (thing is Building_AutoImplanter { initScanner: false })
            {
                return true;
            }
            return false;
        }

        public override void DynamicDrawPhaseAt(DrawPhase phase, Vector3 drawLoc, bool flip = false)
        {
            base.DynamicDrawPhaseAt(phase, drawLoc, flip);
            Occupant?.Drawer.renderer.DynamicDrawPhaseAt(phase, drawLoc, null, neverAimWeapon: true);
        }

        public override void Tick()
        {
            //Log.Message("Ticking");
            base.Tick();
            if (MotePerRotation == null)
            {
                MotePerRotation = new Dictionary<Rot4, ThingDef>
            {
                {
                    Rot4.South,
                    ThingDefOf.SoftScannerGlow_South
                },
                {
                    Rot4.East,
                    ThingDefOf.SoftScannerGlow_East
                },
                {
                    Rot4.West,
                    ThingDefOf.SoftScannerGlow_West
                },
                {
                    Rot4.North,
                    ThingDefOf.SoftScannerGlow_North
                }
            };
                MotePerRotationRip = new Dictionary<Rot4, ThingDef>
            {
                {
                    Rot4.South,
                    ThingDefOf.RipScannerGlow_South
                },
                {
                    Rot4.East,
                    ThingDefOf.RipScannerGlow_East
                },
                {
                    Rot4.West,
                    ThingDefOf.RipScannerGlow_West
                },
                {
                    Rot4.North,
                    ThingDefOf.RipScannerGlow_North
                }
            };
            }
            AutoImplanterState state = State;
            //Log.Message(state.ToString());
            if (state == AutoImplanterState.Occupied)
            {
                deathTicksLeft = TicksToDie;
                fabricationTicksLeft--;
                if (fabricationTicksLeft <= 0)
                {

                    Messages.Message("AutoImplanterFinish".Translate(Occupant.Named("PAWN")), Occupant, MessageTypeDefOf.PositiveEvent);
                    EjectContents();
                    if (def.building.subcoreScannerComplete != null)
                    {
                        def.building.subcoreScannerComplete.PlayOneShot(this);
                    }
                }
                if (workingMote == null || workingMote.Destroyed)
                {
                    workingMote = MoteMaker.MakeAttachedOverlay(this, MotePerRotation[base.Rotation], Vector3.zero);
                }
                workingMote.Maintain();
                if (progressBarEffecter == null)
                {
                    progressBarEffecter = EffecterDefOf.ProgressBar.Spawn();
                }
                progressBarEffecter.EffectTick(this, TargetInfo.Invalid);
                MoteProgressBar mote = ((SubEffecter_ProgressBar)progressBarEffecter.children[0]).mote;
                mote.progress = 1f - (float)fabricationTicksLeft / (float)def.building.subcoreScannerTicks;
                mote.offsetZ = -0.8f;
                if (def.building.subcoreScannerWorking != null)
                {
                    if (sustainerWorking == null || sustainerWorking.Ended)
                    {
                        sustainerWorking = def.building.subcoreScannerWorking.TrySpawnSustainer(SoundInfo.InMap(this, MaintenanceType.PerTick));
                    }
                    else
                    {
                        sustainerWorking.Maintain();
                    }
                }
            }
            else
            {
                effectHusk?.Cleanup();
                effectHusk = null;
                progressBarEffecter?.Cleanup();
                progressBarEffecter = null;
            }
            if (state == AutoImplanterState.Occupied)
            {
                if (def.building.subcoreScannerStartEffect != null)
                {
                    if (effectStart == null)
                    {
                        effectStart = def.building.subcoreScannerStartEffect.Spawn();
                        effectStart.Trigger(this, new TargetInfo(InteractionCell, base.Map));
                    }
                    effectStart.EffectTick(this, new TargetInfo(InteractionCell, base.Map));
                }
            }
            else
            {
                effectStart?.Cleanup();
                effectStart = null;
            }
            if(state == AutoImplanterState.Halted)
            {
                deathTicksLeft--;
                if(deathTicksLeft == 0)
                {
                    Messages.Message("AutoImplanterDeath".Translate(Occupant.Named("PAWN")), Occupant, MessageTypeDefOf.NegativeHealthEvent);
                    CancelProcess();
                }
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }

            //Manage Presets
            Command_Action command_ActionManagePresets = new Command_Action();
            command_ActionManagePresets.defaultLabel = "ImplanterManagePresetsLabel".Translate();
            command_ActionManagePresets.defaultDesc = "ImplanterManagePresetsDesc".Translate();
            command_ActionManagePresets.icon = ManagePresets;
            command_ActionManagePresets.action = delegate
            {
                Dialog_AutoImplanterPreset dialog = new Dialog_AutoImplanterPreset();
                Find.WindowStack.Add(dialog);
            };
            command_ActionManagePresets.activateSound = SoundDefOf.Tick_Tiny;
            yield return command_ActionManagePresets;

            //Select Preset
            Command_Action command_ActionPreset = new Command_Action();
            command_ActionPreset.defaultLabel = "SelectImplanterPresetLabel".Translate();
            command_ActionPreset.defaultDesc = "SelectImplanterPresetDesc".Translate();
            command_ActionPreset.icon = SelectPreset;
            command_ActionPreset.action = delegate
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>();
                IReadOnlyList<AutoImplanterPreset> allAutoImplanterPresets = AutoImplanter_Mod.Settings.ImplanterPresets;
                if (allAutoImplanterPresets == null && !allAutoImplanterPresets.Any())
                {
                    list.Add(new FloatMenuOption("NoAutoImplanterPresets".Translate(), null));
                }
                for (int j = 0; j < allAutoImplanterPresets.Count; j++)
                {
                    AutoImplanterPreset _preset = allAutoImplanterPresets[j];
                    list.Add(new FloatMenuOption(_preset.RenamableLabel, delegate
                    {
                        preset = _preset;
                        ingredients = preset.RequiredIngredients().ToList();
                    }, (Thing)null, Color.white));
                }
                Find.WindowStack.Add(new FloatMenu(list));
            };
            if (initScanner)
            {
                StringBuilder stringBuilder2 = new StringBuilder("AutoImplanterProcessStarted".Translate() + ":\n");
                command_ActionPreset.Disable(stringBuilder2.ToString());
            }
            yield return command_ActionPreset;
            if (preset != null)
            {
                if (!initScanner)
                {
                    Command_Action command_Action = new Command_Action();
                    command_Action.defaultLabel = "AutoImplanterStart".Translate();
                    StringBuilder stringBuilder = new StringBuilder();
                    stringBuilder.Append("AutoImplanterOperates".Translate() + " " + preset.label + ".");
                    stringBuilder.Append("\n\n");
                    stringBuilder.Append("DurationHours".Translate() + ": " + ((int)preset.GetWorkRequired()).ToStringTicksToPeriod());
                    stringBuilder.Append("\n\n");
                    string text = ingredients.Select((IngredientCount i) => i.Summary).ToCommaList(useAnd: true);
                    stringBuilder.Append("AutoImplanterStartDesc".Translate(def.label, text));
                    command_Action.defaultDesc = stringBuilder.ToString();
                    command_Action.icon = StartAutoImplant;
                    command_Action.action = delegate
                    {
                        initScanner = true;
                    };
                    command_Action.activateSound = SoundDefOf.Tick_Tiny;
                    yield return command_Action;
                }
                else if (base.SelectedPawn == null)
                {
                    Command_Action command_Action2 = new Command_Action();
                    command_Action2.defaultLabel = "InsertPerson".Translate() + "...";
                    command_Action2.defaultDesc = "InsertPersonSubcoreScannerDesc".Translate(def.label);
                    command_Action2.icon = InsertPersonIcon;
                    command_Action2.action = delegate
                    {
                        List<FloatMenuOption> list = new List<FloatMenuOption>();
                        IReadOnlyList<Pawn> allPawnsSpawned = base.Map.mapPawns.AllPawnsSpawned;
                        for (int j = 0; j < allPawnsSpawned.Count; j++)
                        {
                            Pawn pawn = allPawnsSpawned[j];
                            AcceptanceReport acceptanceReport = CanAcceptPawn(pawn);
                            if (!acceptanceReport.Accepted)
                            {
                                if (!acceptanceReport.Reason.NullOrEmpty())
                                {
                                    list.Add(new FloatMenuOption(pawn.LabelShortCap + ": " + acceptanceReport.Reason, null, pawn, Color.white));
                                }
                            }
                            else
                            {
                                list.Add(new FloatMenuOption(pawn.LabelShortCap, delegate
                                {
                                    SelectPawn(pawn);
                                }, pawn, Color.white));
                            }
                        }
                        if (!list.Any())
                        {
                            list.Add(new FloatMenuOption("NoExtractablePawns".Translate(), null));
                        }
                        Find.WindowStack.Add(new FloatMenu(list));
                    };
                    if (!PowerOn)
                    {
                        command_Action2.Disable("NoPower".Translate().CapitalizeFirst());
                    }
                    else if (State == AutoImplanterState.WaitingForIngredients)
                    {
                        StringBuilder stringBuilder2 = new StringBuilder("SubcoreScannerWaitingForIngredientsDesc".Translate().CapitalizeFirst() + ":\n");
                        AppendIngredientsList(stringBuilder2);
                        command_Action2.Disable(stringBuilder2.ToString());
                    }
                    yield return command_Action2;
                }
                if (initScanner)
                {
                    Command_Action command_Action3 = new Command_Action();
                    command_Action3.defaultLabel = ((State == AutoImplanterState.Occupied) ? "CommandCancelAutoImplanter".Translate() : "CommandCancelLoad".Translate());
                    command_Action3.defaultDesc = ((State == AutoImplanterState.Occupied) ? "CommandCancelAutoImplanterDesc".Translate() : "CommandCancelLoadDesc".Translate());
                    command_Action3.icon = CancelLoadingIcon;
                    command_Action3.action = delegate
                    {
                        if (State == AutoImplanterState.Occupied)
                        {
                            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("ConfirmCancelAutoImplanter".Translate(Occupant.Named("PAWN")), CancelProcess, destructive: true));
                        }
                        else
                        {
                            EjectItems();
                            EjectContents();
                        }
                    };
                    command_Action3.activateSound = SoundDefOf.Designate_Cancel;
                    yield return command_Action3;
                }
                if (!DebugSettings.ShowDevGizmos)
                {
                    yield break;
                }
                if (State == AutoImplanterState.Occupied)
                {
                    Command_Action command_Action4 = new Command_Action();
                    command_Action4.defaultLabel = "DEV: Complete";
                    command_Action4.action = delegate
                    {
                        fabricationTicksLeft = 0;
                    };
                    yield return command_Action4;
                }
                Command_Action command_Action5 = new Command_Action();
                command_Action5.defaultLabel = (debugDisableNeedForIngredients ? "DEV: Enable Ingredients" : "DEV: Disable Ingredients");
                command_Action5.action = delegate
                {
                    debugDisableNeedForIngredients = !debugDisableNeedForIngredients;
                };
                yield return command_Action5;
            }

        }

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(base.GetInspectString());
            switch (State)
            {
                case AutoImplanterState.WaitingForIngredients:
                    stringBuilder.AppendLineIfNotEmpty();
                    stringBuilder.Append("SubcoreScannerWaitingForIngredients".Translate());
                    AppendIngredientsList(stringBuilder);
                    break;
                case AutoImplanterState.WaitingForOccupant:
                    stringBuilder.AppendLineIfNotEmpty();
                    stringBuilder.Append("SubcoreScannerWaitingForOccupant".Translate());
                    break;
                case AutoImplanterState.Occupied:
                    stringBuilder.AppendLineIfNotEmpty();
                    stringBuilder.Append("SubcoreScannerCompletesIn".Translate() + ": " + fabricationTicksLeft.ToStringTicksToPeriod());
                    break;
                case AutoImplanterState.Halted:
                    stringBuilder.AppendLineIfNotEmpty();
                    stringBuilder.Append("SubcoreScannerHalted".Translate() + ": " + deathTicksLeft.ToStringTicksToPeriod());
                    break;

            }
            return stringBuilder.ToString();
        }

        private void AppendIngredientsList(StringBuilder sb)
        {
            for (int i = 0; i < ingredients.Count; i++)
            {
                IngredientCount ingredientCount = ingredients[i];
                int num = innerContainer.TotalStackCountOfDef(ingredientCount.FixedIngredient);
                int num2 = (int)ingredientCount.GetBaseCount();
                sb.AppendInNewLine($" - {ingredientCount.FixedIngredient.LabelCap} {num} / {num2}");
            }
        }

        public override void ExposeData()
        {

            base.ExposeData();
            Scribe_Values.Look(ref initScanner, "initScanner", defaultValue: false);
            Scribe_Deep.Look(ref preset, "preset");
            Scribe_Collections.Look(ref ingredients, "ingredients", LookMode.Deep);
            Scribe_Values.Look(ref fabricationTicksLeft, "fabricationTicksLeft", 0);
            Scribe_Values.Look(ref deathTicksLeft, "deathTicksLeft", 0);
        }
    }
}
