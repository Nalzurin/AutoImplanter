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

        private ThingDef race;
        public ThingDef Race => race;
        public List<ImplantRecipe> implants = [];
        public List<ImplantRecipe> implantsForReading => implants;

        public AutoImplanterPreset()
        {
        }
        public AutoImplanterPreset(int id, string label, ThingDef race = null)
        {
            this.id = id;
            this.label = label;
            if(race == null)
            {
                this.race = ThingDefOf.Human;
            }
            else
            {
                this.race = race;
            }
                AutoImplanter_Mod.instance.WriteSettings();

        }
        public void SetRace(ThingDef newRace)
        {
            race = newRace;
            implants.Clear();
        }
        public void DebugPrintAllImplants()
        {
            AutoImplanter_Mod.instance.ClearNullImplants(id);
            foreach (ImplantRecipe recipe in implants)
            {
                if (recipe.recipe == null || recipe.bodyPart == null)
                {
                    this.implants.Remove(recipe);
                    AutoImplanter_Mod.instance.WriteSettings();
                }
                else
                {
                    Log.Message(recipe.bodyPart.LabelCap + ": " + recipe.recipe.label);
                }
                
            }
        }


        public bool AddImplant(BodyPartRecord part, RecipeDef recipe)
        {
            if (part == null) { Log.Error("part null"); return false; }
            if (recipe == null) { Log.Error("recipe null"); return false; }
            if (!implants.Any((c) => { return c.recipe == recipe && c.bodyPart == part; }))
            {
                implants.Add(new ImplantRecipe(recipe, part));
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


        public void ExposeData()
        {
            Scribe_Values.Look(ref id, "id", 0);
            Scribe_Values.Look(ref label, "label");
            Scribe_Defs.Look(ref race, "race");
            Scribe_Collections.Look(ref implants, "implants", LookMode.Deep);
        }

    }
}
