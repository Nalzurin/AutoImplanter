using KTrie;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace AutoImplanter
{
    [StaticConstructorOnStartup]
    public class ImplantRecipe : IExposable
    {
        public RecipeDef recipe;
        public BodyPartRecord bodyPart;
        public ImplantRecipe() { }
        public ImplantRecipe(RecipeDef _recipe, BodyPartRecord _bodyPart)
        {
            recipe = _recipe;
            bodyPart = _bodyPart;
        }
        public void ExposeData()
        {
            Scribe_Defs.Look(ref recipe, "ImplantRecipe");
            Scribe_BodyParts.Look(ref bodyPart, "ImplantBodyPart");
        }
    }
    [StaticConstructorOnStartup]
    public class AutoImplanterPreset : IExposable, IRenameable
    {
        public int id;
        public string label;

        public string RenamableLabel
        {
            get
            {
                return label;
            }
            set
            {
                label = value;
            }
        }

        public string BaseLabel => "New Preset";
        public string InspectLabel => RenamableLabel;
        public List<ImplantRecipe> implants = [];
        public float totalWorkAmount;
        public float currentWorkAmountDone;

        public AutoImplanterPreset()
        {
        }
        public AutoImplanterPreset(int id, string label)
        {
            this.id = id;
            this.label = label;
            AutoImplanter_Mod.instance.WriteSettings();

        }
        public void DebugPrintAllImplants()
        {
            foreach (ImplantRecipe recipe in implants)
            {
                Log.Message(recipe.bodyPart.LabelCap + ": " + recipe.recipe.label);
            }
        }


        public bool AddImplant(BodyPartRecord part, RecipeDef recipe)
        {
            if (part == null) { Log.Error("part null"); return false; }
            if (recipe == null) { Log.Error("recipe null"); return false; }
            if (!implants.Any((c) => { return c.recipe == recipe && c.bodyPart == part; }))
            {
                implants.Add(new ImplantRecipe(recipe, part));
                totalWorkAmount = GetWorkRequired();
                AutoImplanter_Mod.instance.WriteSettings();
                return true;
            }
            else
            {
                Log.Error("Implant recipe already exists");
                return false;
            }

        }

        public bool RemoveImplant(BodyPartRecord part, RecipeDef recipe)
        {
            if (part == null) { Log.Error("part null"); return false; }
            if (recipe == null) { Log.Error("recipe null"); return false; }
            if (implants.Any((c) => { return c.recipe == recipe && c.bodyPart == part; }))
            {
                implants.RemoveWhere((c) => { return c.recipe == recipe && c.bodyPart == part; });
                totalWorkAmount = GetWorkRequired();
                AutoImplanter_Mod.instance.WriteSettings();
                return true;
            }
            else
            {
                //Log.Error("No such implant recipe exists");
                return false;
            }
        }

        public float GetWorkRequired()
        {
            float workRequired = 0f;
            foreach (ImplantRecipe implant in implants)
            {
                workRequired += implant.recipe.workAmount;

            }
            workRequired *= AutoImplanter_Mod.Settings.SurgerySpeedModifier;
            return workRequired;
        }
        public IEnumerable<IngredientCount> RequiredIngredients()
        {
            Dictionary<ThingDef, int> dictionary = new Dictionary<ThingDef, int>
            {
                { ThingDefOf.MedicineIndustrial, 1 }
            };
            foreach (ImplantRecipe implant in implants)
            {
                dictionary[ThingDefOf.MedicineIndustrial] += 1;
                foreach (IngredientCount ingr in implant.recipe.ingredients)
                {
                    if (ingr.filter.AllowedThingDefs.All((c) => { return c.thingCategories.Contains(ThingCategoryDefOf.Medicine); }))
                    {
                        continue;
                    }
                    if (dictionary.ContainsKey(ingr.filter.AllowedThingDefs.First()))
                    {
                        dictionary[ingr.filter.AllowedThingDefs.First()] += (int)ingr.GetBaseCount();
                    }
                    else
                    {
                        dictionary[ingr.filter.AllowedThingDefs.First()] = (int)ingr.GetBaseCount();
                    }
                }


            }
            List<IngredientCount> list = new List<IngredientCount>();
            foreach (KeyValuePair<ThingDef, int> item2 in dictionary)
            {
                list.Add(new ThingDefCountClass(item2.Key, item2.Value).ToIngredientCount());
            }
            return list;
        }


        public void ApplyOn(Pawn pawn)
        {
            foreach (ImplantRecipe implant in implants)
            {
                if (implant.recipe.workerClass == typeof(Recipe_InstallImplant))
                {

                    pawn.health.AddHediff(implant.recipe.addsHediff, implant.bodyPart);
                }
                if (implant.recipe.workerClass == typeof(Recipe_InstallArtificialBodyPart))
                {
                    pawn.health.RestorePart(implant.bodyPart);
                    pawn.health.AddHediff(implant.recipe.addsHediff, implant.bodyPart);
                }
            }
        }
        public void DoWork(float workAmount, out bool workDone)
        {
            currentWorkAmountDone += workAmount;
            if (currentWorkAmountDone >= totalWorkAmount)
            {
                workDone = true;
            }
            else
            {
                workDone = false;
            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref id, "id", 0);
            Scribe_Values.Look(ref label, "label");
            Scribe_Values.Look(ref totalWorkAmount, "totalWorkAmount", 0f);
            Scribe_Values.Look(ref currentWorkAmountDone, "currentWorkAmountDone", 0f);
            Scribe_Collections.Look(ref implants, "implants", LookMode.Deep);
        }

    }
}
