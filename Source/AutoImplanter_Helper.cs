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
    public static class AutoImplanter_Helper
    {
        public static bool isImplantCompatible(AutoImplanterPreset preset, BodyPartRecord part, RecipeDef recipe)
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
                //Log.Message("Has, returning false.");
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
        public static List<RecipeDef> ListAllImplantsForBodypart(BodyPartRecord part)
        {
            if (part == null)
            {
                Log.Message("Part is null");
                return null;
            }
            if (part.def == null)
            {
                Log.Message("Part def is null");
                return null;
            }
            return DefDatabase<RecipeDef>.AllDefs.Where((c) => { return Predicate(c, part); }).ToList();

        }
        private static bool Predicate(RecipeDef c, BodyPartRecord part)
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
            return c.appliedOnFixedBodyParts.Contains(part.def) && c.recipeUsers.Contains(ThingDefOf.Human) && (c.addsHediff.hediffClass == typeof(Hediff_Implant) || c.addsHediff.hediffClass == typeof(Hediff_AddedPart));

        }
    }
}
