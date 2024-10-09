using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;

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

        protected float OffsetHeaderY = 36f;
        public AutoImplanterPreset preset;
        public BodyPartRecord selectedPart;
        public override Vector2 InitialSize => new Vector2(Mathf.Min(Screen.width - 50, 1300), 720f);

        public override void PreOpen()
        {
            base.PreOpen();
            if (AutoImplanterPreset.presets.Count() > 0)
            {
                preset = AutoImplanterPreset.presets[0];
            }
            else
            {
                preset = new AutoImplanterPreset();
                AutoImplanterPreset.presets.Add(preset);
            }

            foreach(AutoImplanterPreset preset in AutoImplanterPreset.presets)
            {
                Log.Message(preset.Label);
                preset.DebugPrintAllImplants();
            }
       
        }
        public override void DoWindowContents(Rect inRect)
        {
            float width = (inRect.width / 3) - (ColumnMargins * 2);
            Rect rect2 = inRect;
            rect2.height = OffsetHeaderY;
            DoPresetOptions(rect2);
            // Left window (body parts)
            Rect rect3 = inRect;
            rect3.yMin = rect2.yMax;
            rect3.width = width;
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
            Widgets.DrawMenuSection(rect5);
            rect5 = rect5.ContractedBy(1f);
            if (selectedPart == null)
            {

            }
            else
            {
                List<RecipeDef> implants = AutoImplanter_Helper.ListAllImplantsForBodypart(selectedPart);
                if (implants.Count == 0)
                {

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

        }
        private void DoPresetOptions(Rect rect)
        {
            using (new TextBlock(GameFont.Medium))
            {
                Widgets.Label(rect, "Presets");
            }
        }

        private void DoEntryBodyPartImplantRow(Rect rect, RecipeDef implant, int index)
        {
            Text.Anchor = TextAnchor.MiddleCenter;
            float x = rect.x;
            float num = (rect.height - IconForBodypart.height) / 2f;

            Widgets.LabelWithIcon(new Rect(x + 5f, rect.y + num, rect.width, IconForBodypart.height), implant.LabelCap, IconForBodypart);
            if (preset.implants.Any((c) => { return c.recipe == implant && c.bodyPart == selectedPart; }))
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
                if(!preset.RemoveImplant(selectedPart, implant))
                {
                    preset.AddImplant(selectedPart, implant);
                }
               
            }
            Text.Anchor = TextAnchor.UpperLeft;

        }
        private void DoEntryBodyPartRow(Rect rect, BodyPartRecord part, int index)
        {

            Text.Anchor = TextAnchor.MiddleCenter;
            float x = rect.x;
            float num = (rect.height - IconForBodypart.height) / 2f;

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
            Text.Anchor = TextAnchor.UpperLeft;

        }
    }
}