using RimWorld;
using System.Text.RegularExpressions;
using Verse;

namespace AutoImplanter
{
    public class Dialog_RenameAutoImplanterPreset : Dialog_Rename<AutoImplanterPreset>
    {
        private static readonly Regex ValidNameRegex = new Regex("^[\\p{L}0-9 '\\-]*$");

        public Dialog_RenameAutoImplanterPreset(AutoImplanterPreset preset)
            : base(preset)
        {
        }

        protected override AcceptanceReport NameIsValid(string name)
        {
            AcceptanceReport result = base.NameIsValid(name);
            if (!result.Accepted)
            {
                return result;
            }
            if (!ValidNameRegex.IsMatch(name))
            {
                return "InvalidName".Translate();
            }
            return true;
        }
        protected override void OnRenamed(string name)
        {
            base.OnRenamed(name);
            AutoImplanter_Mod.instance.WriteSettings();
        }


    }
}
