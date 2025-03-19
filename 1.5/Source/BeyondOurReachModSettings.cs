using Verse;
using System.Collections.Generic;

namespace BeyondOurReachModSettings
{
	public class BeyondOurReachModSettings : ModSettings
	{
		internal static Dictionary<string, bool> SettingsDict = [];
		private static List<string> s_;
		private static List<bool> b_;

		public override void ExposeData()
		{
			Scribe_Collections.Look(ref SettingsDict, "BeyondOurReachModSettings", LookMode.Value, LookMode.Value, ref s_, ref b_);

			if (Scribe.mode == LoadSaveMode.LoadingVars)
			{
				SettingsDict ??= [];
			}

			base.ExposeData();
		}
	}
}
