using KTrie;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace AutoImplanter
{
    [StaticConstructorOnStartup]
    public static class AutoImplanter_Helper
    {
        public static List<ThingDef> races = DefDatabase<ThingDef>.AllDefsListForReading.Where(c => c.race?.intelligence == Intelligence.Humanlike && !c.IsCorpse && !c.defName.ToLower().Contains("creepjoiner")).ToList();
        public static void applyImplantPreset(AutoImplanterPreset preset, Pawn pawn, List<Thing> ingredients = null)
        {

            foreach(ImplantRecipe recipe in preset.implants)
            {
                recipe.recipe.Worker.ApplyOnPawn(pawn, recipe.bodyPart, null, ingredients.Where(c=>recipe.recipe.IsIngredient(c.def)).ToList(), null);
            }
        }
        public static bool isImplantCompatible(AutoImplanterPreset preset, BodyPartRecord part, RecipeDef recipe, out RecipeDef incompatibility)
        {
            if (preset.implants.Any((c) => { return c.recipe == recipe && c.bodyPart == part; }))
            {
                //Log.Message("Same recipe skipping");
                incompatibility = null;
                return true;
            }
            //Log.Message("Checking if recipe has incompatibility tags with any selected");
            RecipeDef incomp = null;
            foreach(ImplantRecipe implantRecipe in preset.implants)
            {
                if(implantRecipe.recipe.addsHediff.tags != null && recipe.incompatibleWithHediffTags!= null)
                {
                    foreach (string tag in implantRecipe.recipe.addsHediff.tags)
                    {
                        if (recipe.incompatibleWithHediffTags.Contains(tag))
                        {
                            incompatibility = implantRecipe.recipe;
                            return false;
                        }
                    }
                }
               
            }

            BodyPartRecord part1 = part;
            //Log.Message("Checking if parent body parts of the part are aritifical");
            // check if parent body parts are artificial
            while (part1.parent != null)
            {
                if (preset.implants.Any((c) =>
                {
                    incomp = c.recipe;
                    return c.bodyPart == part1 && c.recipe.workerClass == typeof(Recipe_InstallArtificialBodyPart);
                }))
                {
                    incompatibility = incomp;
                    return false;
                }
                part1 = part1.parent;
            }

            //Log.Message("Checking if any selected recipes target parts that will be replaced with the recipe");

            if (recipe.workerClass == typeof(Recipe_InstallArtificialBodyPart))
            {
                //Log.Message("Recipe's worker class is
                //
                //
                //aritifical bodypart");

                foreach (ImplantRecipe item in preset.implants)
                {
                    //Log.Message(item.bodyPart.Label);
                    part1 = item.bodyPart;
                    while (part1.parent != null)
                    {
                        if (part1 == part)
                        {
                            incompatibility = item.recipe;
                            return false;
                        }
                        part1 = part1.parent;
                    }
                }
            }

            incompatibility = null;
            return true;
        }
        public static List<RecipeDef> ListAllImplantsForBodypart(BodyPartRecord part, ThingDef race)
        {
            if (part == null)
            {
                //Log.Message("Part is null");
                return null;
            }
            if (part.def == null)
            {
                //Log.Message("Part def is null");
                return null;
            }
            if(race == null)
            {
                return null;
            }
            return DefDatabase<RecipeDef>.AllDefs.Where((c) => { return Predicate(c, part, race); }).ToList();

        }
        private static bool Predicate(RecipeDef c, BodyPartRecord part, ThingDef race)
        {
            if(c == null)
            {
                return false;
            }
            if(c.appliedOnFixedBodyParts == null)
            {
                return false;
            }
            if(c.recipeUsers == null)
            {
                return false;
            }
            if(c.addsHediff == null)
            {
                return false;
            }
            if(c.mutantPrerequisite != null)
            {
                return false;
            }

            return c.appliedOnFixedBodyParts.Contains(part.def) && c.recipeUsers.Contains(race) && (c.addsHediff.hediffClass == typeof(Hediff_Implant) || c.addsHediff.hediffClass == typeof(Hediff_AddedPart));

        }
        public static List<BodyPartRecord> GetRaceBodyParts(ThingDef race)
        {
            if(race == null)
            {
                return null;
            }
            return race.race.body.AllParts.Where((c) => { return ListAllImplantsForBodypart(c, race).Count > 0; }).ToList();
        }
    }
}
