using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace AutoImplanter
{

    public class ITab_AutoImplanterNutritionStorage : ITab_Storage
    {
        protected override bool IsPrioritySettingVisible => false;

        public ITab_AutoImplanterNutritionStorage()
        {
            labelKey = "Nutrition";

        }
    }
}
