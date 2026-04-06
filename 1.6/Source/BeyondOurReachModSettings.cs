using Verse;
using System.Collections.Generic;

namespace BeyondOurReachModSettings
{
	public class BeyondOurReachModSettings : ModSettings
	{
		internal static Dictionary<string, bool> SettingsDict = [];
		private static List<string> s_;
		private static List<bool> b_;

		internal static bool GetSettingValue(string defName, bool defaultValue = true)
		{
			SettingsDict ??= [];

			if (!SettingsDict.TryGetValue(defName, out bool enabled))
			{
				enabled = defaultValue;
				SettingsDict[defName] = enabled;
			}

			return enabled;
		}

		public override void ExposeData()
		{
			Scribe_Collections.Look(ref SettingsDict, "BeyondOurReachModSettings", LookMode.Value, LookMode.Value, ref s_, ref b_);
			SettingsDict ??= [];

			base.ExposeData();
		}
	}
}
