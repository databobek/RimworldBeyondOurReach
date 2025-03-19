using System.Collections.Generic;
using System.Linq;
using Verse;

namespace BeyondOurReachModSettings
{
	[StaticConstructorOnStartup]
	internal static class ModSettingDefFetcher
	{
		internal static readonly List<ModSettingDef> AllModSettingsOrdered = [.. DefDatabase<ModSettingDef>.AllDefsListForReading.OrderBy(x => x.settingLabel)];
	}
}
