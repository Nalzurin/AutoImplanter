using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using static HarmonyLib.Code;
using static UnityEngine.GraphicsBuffer;

namespace AutoImplanter
{
    [StaticConstructorOnStartup]

    public class Dialog_AutoImplanterPreset : Window
    {
        private Vector2 scrollPositionLeft;
        private Vector2 scrollPositionMiddle;
        private Vector2 scrollPositionRight;
        private static readonly Texture2D IconForBodypart = ContentFinder<Texture2D>.Get("Things/Item/Health/HealthItem");

        private const float ColumnMargins = 8f;
        private const float EntryRowHeight = 70f;

        protected float OffsetHeaderY = 72f;
        public AutoImplanterPreset preset;
        public BodyPartRecord selectedPart;
        public override Vector2 InitialSize => new Vector2(Mathf.Min(Screen.width - 50, 1300), 720f);

        public override void PreOpen()
        {
            base.PreOpen();
            if (AutoImplanter_Mod.Settings.ImplanterPresets.Count > 0)
            {
                preset = AutoImplanter_Mod.Settings.ImplanterPresets[0];
            }
            else
            {
                preset = new AutoImplanterPreset();
                AutoImplanter_Mod.Settings.ImplanterPresets.Add(preset);
            }

            foreach (AutoImplanterPreset preset in AutoImplanter_Mod.Settings.ImplanterPresets)
            {
                Log.Message(preset.label);
                preset.DebugPrintAllImplants();
            }

        }
        public override void DoWindowContents(Rect inRect)
        {
            float width = (inRect.width / 3) - (ColumnMargins * 0.75f);
            Rect rect2 = inRect;
            rect2.height = OffsetHeaderY;
            DoPresetOptions(rect2);
            // Left window (body parts)
            Rect rect3 = inRect;
            rect3.yMin = rect2.yMax;
            rect3.width = width * 0.8f;
            rect3.xMin = ColumnMargins;
            Widgets.DrawMenuSection(rect3);
            rect3 = rect3.ContractedBy(1f);
            float height = PawnKindDefOf.Colonist.race.race.body.AllParts.Count * EntryRowHeight;
            Rect viewRect = new Rect(0f, 0f, rect3.width, height);
            Widgets.AdjustRectsForScrollView(inRect, ref rect3, ref viewRect);
            Widgets.BeginScrollView(rect3, ref scrollPositionLeft, viewRect);
            for (int i = 0; i < PawnKindDefOf.Colonist.race.race.body.AllParts.Count; i++)
            {
                Rect rect4 = new Rect(0f, (float)i * EntryRowHeight, viewRect.width, EntryRowHeight);
                DoEntryBodyPartRow(rect4, PawnKindDefOf.Colonist.race.race.body.AllParts[i], i);
            }
            Widgets.EndScrollView();
            // Middle window (Implants/Prosthetics)
            Rect rect5 = inRect;
            rect5.yMin = rect2.yMax;
            rect5.xMin = rect3.xMax + ColumnMargins;
            rect5.width = width * 1.1f;
            Widgets.DrawMenuSection(rect5);
            rect5 = rect5.ContractedBy(1f);
            if (selectedPart == null)
            {
                using (new TextBlock(GameFont.Medium))
                {
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(rect5, "Body part not chosen");
                    Text.Anchor = TextAnchor.UpperLeft;
                }
            }
            else
            {
                List<RecipeDef> implants = AutoImplanter_Helper.ListAllImplantsForBodypart(selectedPart);
                if (implants.Count == 0)
                {
                    using (new TextBlock(GameFont.Medium))
                    {
                        Text.Anchor = TextAnchor.MiddleCenter;
                        Widgets.Label(rect5, "No implants");
                        Text.Anchor = TextAnchor.UpperLeft;
                    }
                }
                else
                {
                    float height1 = implants.Count * EntryRowHeight;
                    Rect viewRect1 = new Rect(0f, 0f, rect5.width, height1);
                    Widgets.AdjustRectsForScrollView(inRect, ref rect5, ref viewRect1);
                    Widgets.BeginScrollView(rect5, ref scrollPositionMiddle, viewRect);
                    for (int i = 0; i < implants.Count; i++)
                    {
                        Rect rect6 = new Rect(0f, (float)i * EntryRowHeight, viewRect1.width, EntryRowHeight);
                        DoEntryBodyPartImplantRow(rect6, implants[i], i);
                    }
                    Widgets.EndScrollView();
                }

            }
            // Right window (Selected implants/prosthetics)
            Rect rect7 = inRect;
            rect7.yMin = rect2.yMax;
            rect7.xMin = rect5.xMax + ColumnMargins;
            rect7.width = width * 1.1f;
            rect7.yMax = rect5.yMax * 0.85f;
            Widgets.DrawMenuSection(rect7);
            rect7 = rect7.ContractedBy(1f);
            if (preset.implants.Count == 0)
            {
                using (new TextBlock(GameFont.Medium))
                {
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(rect7, "No implants selected");
                    Text.Anchor = TextAnchor.UpperLeft;
                }
            }
            else
            {

                float height2 = preset.implants.Count * EntryRowHeight;
                Rect viewRect2 = new Rect(0f, 0f, rect7.width, height2);
                Widgets.AdjustRectsForScrollView(inRect, ref rect7, ref viewRect2);
                Widgets.BeginScrollView(rect7, ref scrollPositionRight, viewRect);
                List<ImplantRecipe> implants = preset.implants;
                implants.Sort((c1, c2) => c1.bodyPart.Label.CompareTo(c2.bodyPart.Label));
                for (int i = 0; i < implants.Count; i++)
                {
                    Rect rect8 = new Rect(0f, (float)i * EntryRowHeight, viewRect2.width, EntryRowHeight);
                    DoEntrySelectedImplant(rect8, implants[i], i);
                }
                Widgets.EndScrollView();
            }
            //Bottom right Window (display work amount and medicine cost)
            Rect rect9 = inRect;
            rect9.yMin = rect7.yMax + ColumnMargins;
            rect9.xMin = rect5.xMax + ColumnMargins;
            rect9.width = width * 1.1f;
            Widgets.DrawMenuSection(rect9);
            rect9 = rect9.ContractedBy(1f);

            using (new TextBlock(GameFont.Medium))
            {
                Text.Anchor = TextAnchor.MiddleLeft;
                Rect rect10 = new Rect(rect9.xMin + rect9.width * 0.25f, rect9.yMin + rect9.height * 0.1f, rect9.width * 0.75f, rect9.height * 0.4f);
                Widgets.Label(rect10, $"Work Amount: {preset.GetWorkRequired()}");
                Rect rect11 = new Rect(rect9.xMin + rect9.width * 0.25f, rect10.yMax, rect9.width * 0.75f, rect9.height * 0.4f);
                Widgets.Label(rect11, $"Medicine Cost: {preset.implants.Count + 1}");
                Text.Anchor = TextAnchor.UpperLeft;
            }

        }
        private void DoPresetOptions(Rect rect)
        {

            Widgets.Label(rect, "Presets");
/*            if (preset != null)
            {
                string text = preset.label;
                using (new TextBlock(GameFont.Medium))
                {
                    Widgets.LabelEllipses(new Rect(rect.xMin + rect.width * 0.1f, rect.yMin + rect.height * 0.1f, rect.width * 0.75f, rect.height * 0.8f), text);
                }
                float width = rect.width * 0.75f / 3;
                Rect rect1 = new Rect(rect.xMax * 0.8f, rect.yMin + rect.height * 0.1f, width, rect.height * 0.8f);
                Rect rect2 = new Rect(rect1.xMax, rect.yMin + rect.height * 0.1f, width, rect.height * 0.8f);
                Rect rect3 = new Rect(rect2.xMax, rect.yMin + rect.height * 0.1f, width, rect.height * 0.8f);
                TooltipHandler.TipRegionByKey(rect1, "DeletePolicyTip");
                TooltipHandler.TipRegionByKey(rect2, "DuplicatePolicyTip");
                TooltipHandler.TipRegionByKey(rect3, "RenamePolicyTip");
                if (Widgets.ButtonImage(rect1, TexUI.DismissTex))
                {
                    *//*TaggedString taggedString = "DeletePolicyConfirm".Translate(preset.label);
                    TaggedString taggedString2 = "DeletePolicyConfirmButton".Translate();*//*
                    Find.WindowStack.Add(new Dialog_Confirm("Delete Preset", "Are you certain you want to delete this preset", DeletePreset));
                }
                if (Widgets.ButtonImage(rect2, TexUI.CopyTex))
                {
                    *//*AutoImplanterPreset val = new AutoImplanterPreset();
                    val.CopyFrom(SelectedPolicy);
                    SelectedPolicy = val;*//*
                }
                if (Widgets.ButtonImage(rect3, TexUI.RenameTex))
                {
                    Find.WindowStack.Add(new Dialog_RenameAutoImplanterPreset(preset));
                }
                return;
            }*/
        }

        private void DeletePreset()
        {
            AutoImplanter_Mod.Settings.ImplanterPresets.RemoveWhere((c) => { return c.id == preset.id; });
            preset = AutoImplanter_Mod.Settings.ImplanterPresets.First();
        }
        private void DoEntrySelectedImplant(Rect rect, ImplantRecipe implant, int index)
        {
            Text.Anchor = TextAnchor.MiddleCenter;
            float x = rect.x;
            float num = (rect.height - IconForBodypart.height) / 2f;
            using (new TextBlock(GameFont.Medium))
            {
                Widgets.LabelWithIcon(new Rect(x + 5f, rect.y + num, rect.width, IconForBodypart.height), $"{implant.bodyPart.LabelCap}: {implant.recipe.LabelCap}", IconForBodypart);
                if (index % 2 == 1)
                {
                    Widgets.DrawLightHighlight(rect);
                }


            }
            Text.Anchor = TextAnchor.UpperLeft;
        }
        private void DoEntryBodyPartImplantRow(Rect rect, RecipeDef implant, int index)
        {
            Text.Anchor = TextAnchor.MiddleCenter;
            float x = rect.x;
            float num = (rect.height - IconForBodypart.height) / 2f;

            using (new TextBlock(GameFont.Medium))
            {
                Widgets.LabelWithIcon(new Rect(x + 5f, rect.y + num, rect.width, IconForBodypart.height), implant.LabelCap, IconForBodypart);
                if (preset.implants.Any((c) => { return c.recipe == implant && c.bodyPart == selectedPart; }))
                {
                    Widgets.DrawHighlightSelected(rect);
                }
                else if (!isImplantCompatible(selectedPart, implant))
                {
                    Widgets.DrawOptionUnselected(rect);

                }

                else if (Mouse.IsOver(rect))
                {

                    Widgets.DrawHighlight(rect);
                }
                else if (index % 2 == 1)
                {
                    Widgets.DrawLightHighlight(rect);
                }
                if (Widgets.ButtonInvisible(rect))
                {
                    if (isImplantCompatible(selectedPart, implant))
                    {
                        if (!preset.RemoveImplant(selectedPart, implant))
                        {
                            preset.AddImplant(selectedPart, implant);
                        }
                    }



                }

            }
            Text.Anchor = TextAnchor.UpperLeft;
        }
        private void DoEntryBodyPartRow(Rect rect, BodyPartRecord part, int index)
        {

            Text.Anchor = TextAnchor.MiddleCenter;
            float x = rect.x;
            float num = (rect.height - IconForBodypart.height) / 2f;
            using (new TextBlock(GameFont.Medium))
            {
                Widgets.LabelWithIcon(new Rect(x + 5f, rect.y + num, rect.width, IconForBodypart.height), part.LabelCap, IconForBodypart);
                if (selectedPart == part)
                {
                    Widgets.DrawHighlightSelected(rect);
                }
                else if (Mouse.IsOver(rect))
                {
                    Widgets.DrawHighlight(rect);
                }
                else if (index % 2 == 1)
                {
                    Widgets.DrawLightHighlight(rect);
                }
                if (Widgets.ButtonInvisible(rect))
                {
                    selectedPart = part;
                }

            }
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private bool isImplantCompatible(BodyPartRecord part, RecipeDef recipe)
        {
            if (preset.implants.Any((c) => { return c.recipe == recipe && c.bodyPart == part; }))
            {
                //Log.Message("Same recipe skipping");
                return true;
            }
            //Log.Message("Checking if recipe has incompatibility tags with any selected");
            if (recipe.incompatibleWithHediffTags != null && preset.implants.Any((c) =>
            {
                return c.recipe.addsHediff.tags != null ? c.recipe.addsHediff.tags.Any(c => recipe.incompatibleWithHediffTags.Any(x => c == x)) : false;
            }))

            {
                Log.Message("Has, returning false.");
                return false;
            }

            BodyPartRecord part1 = part;
            //Log.Message("Checking if parent body parts of the part are aritifical");
            // check if parent body parts are artificial
            while (part1.parent != null)
            {
                if (preset.implants.Any((c) =>
                {
                    return c.bodyPart == part1 && c.recipe.workerClass == typeof(Recipe_InstallArtificialBodyPart);
                }))
                {
                    return false;
                }
                part1 = part1.parent;
            }

            //Log.Message("Checking if any selected recipes target parts that will be replaced with the recipe");

            if (recipe.workerClass == typeof(Recipe_InstallArtificialBodyPart))
            {
                //Log.Message("Recipe's worker class is installing aritifical bodypart");

                foreach (ImplantRecipe item in preset.implants)
                {
                    //Log.Message(item.bodyPart.Label);
                    part1 = item.bodyPart;
                    while (part1.parent != null)
                    {
                        if (part1 == part)
                        {
                            return false;
                        }
                        part1 = part1.parent;
                    }
                }
            }


            return true;
        }

    }
}