using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace AutoImplanter
{
    public class Dialog_LoadAutoImplanterPreset : Window
    {
        
        protected string interactButLabel = "Load".Translate();

        protected float bottomAreaHeight;

        protected Vector2 scrollPosition = Vector2.zero;

        protected string typingName = "";

        private bool focusedNameArea;

        protected string deleteTipKey = "AutoImplanterLoadMenuDeletePreset".Translate();

        protected const float EntryHeight = 40f;

        protected const float FileNameLeftMargin = 8f;

        protected const float FileNameRightMargin = 4f;

        protected const float FileInfoWidth = 94f;

        protected const float InteractButWidth = 100f;

        protected const float InteractButHeight = 36f;

        protected const float DeleteButSize = 36f;

        private static readonly Color DefaultFileTextColor = new Color(1f, 1f, 0.6f);

        protected const float NameTextFieldWidth = 400f;

        protected const float NameTextFieldHeight = 35f;

        protected const float NameTextFieldButtonSpace = 20f;

        public override Vector2 InitialSize => new Vector2(620f, 700f);

        private AutoImplanterPreset preset;

        public Dialog_LoadAutoImplanterPreset()
        {
            doCloseButton = true;
            doCloseX = true;
            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnAccept = true;
        }
        public override void PostClose()
        {
            base.PostClose();
            if (preset != null)
            {
                Find.WindowStack.Add(new Dialog_AutoImplanterPreset(preset));
            }
            else
            {
                Find.WindowStack.Add(new Dialog_AutoImplanterPreset());
            }

        }

        public override void DoWindowContents(Rect inRect)
        {
            List<AutoImplanterPreset> presets = AutoImplanter_Mod.Settings.ImplanterPresetsForReading;
            Vector2 vector = new Vector2(inRect.width - 16f, 40f);
            float y = vector.y;
            float height = (float)AutoImplanter_Mod.Settings.ImplanterPresets.Count * y;
            Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, height);
            float num = inRect.height - Window.CloseButSize.y - bottomAreaHeight - 18f;
            Rect outRect = inRect.TopPartPixels(num);
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            float num2 = 0f;
            int num3 = 0;
            for(int i = 0; i< presets.Count; i++) 
            {
                AutoImplanterPreset pres = presets[i];
                if (num2 + vector.y >= scrollPosition.y && num2 <= scrollPosition.y + outRect.height)
                {
                    Rect rect = new Rect(0f, num2, vector.x, vector.y);
                    if (num3 % 2 == 0)
                    {
                        Widgets.DrawAltRect(rect);
                    }
                    Widgets.BeginGroup(rect);
                    Rect rect2 = new Rect(rect.width - 36f, (rect.height - 36f) / 2f, 36f, 36f);
                    if (Widgets.ButtonImage(rect2, TexButton.Delete, Color.white, GenUI.SubtleMouseoverColor))
                    {
                        Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("ConfirmDelete".Translate(pres.RenamableLabel), delegate
                        {
                            AutoImplanter_Mod.Settings.ImplanterPresets.Remove(pres);
                            AutoImplanter_Mod.instance.WriteSettings();
                        }, destructive: true));
                    }
                    TooltipHandler.TipRegionByKey(rect2, deleteTipKey);
                    Text.Font = GameFont.Small;
                    Rect rect3 = new Rect(rect2.x - 100f, (rect.height - 36f) / 2f, 100f, 36f);
                    if (Widgets.ButtonText(rect3, interactButLabel))
                    {
                        preset = pres;
                        this.Close();

                    }
                    Rect rect4 = new Rect(rect3.x - 94f, 0f, 94f, rect.height);
                    GUI.color = Color.white;
                    Text.Anchor = TextAnchor.UpperLeft;
                    Rect rect5 = new Rect(8f, 0f, rect4.x - 8f - 4f, rect.height);
                    Text.Anchor = TextAnchor.MiddleLeft;
                    Text.Font = GameFont.Small;
                    Widgets.Label(rect5, $"{pres.RenamableLabel} - {pres.Race.LabelCap} ({pres.Race.defName})");
                    GUI.color = Color.white;
                    Text.Anchor = TextAnchor.UpperLeft;
                    Widgets.EndGroup();
                }
                num2 += vector.y;
                num3++;
            }
            Widgets.EndScrollView();
        }
    }
}
