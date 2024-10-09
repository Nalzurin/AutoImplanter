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
