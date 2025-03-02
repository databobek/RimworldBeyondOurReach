using Verse;
using RimWorld;
using UnityEngine;
using System.Linq;
using RimWorld.Planet;

namespace BeyondOurReachModSettings
{
	public class BeyondOurReachMod : Mod
	{
		private static Vector2 ScrollPos = Vector2.zero;


		public BeyondOurReachMod(ModContentPack content) : base(content) { }


		public override void DoSettingsWindowContents(Rect inRect)
		{
			// Doing it in the constructor gives 0 results as Mod gets initialized before loading defs
			foreach (ModSettingDef modSettingDef in ModSettingDefFetcher.AllModSettingsOrdered)
			{
				BeyondOurReachModSettings.SettingsDict.TryAdd(modSettingDef, true);
			}

			base.DoSettingsWindowContents(inRect);

			Rect scrollRect = inRect.ContractedBy(32f);
			scrollRect.height = inRect.height;

			Widgets.BeginScrollView(inRect, ref ScrollPos, scrollRect);
			Listing_Standard listing = new();
			listing.Begin(scrollRect);

			try
			{
				foreach (var modSetting in ModSettingDefFetcher.AllModSettingsOrdered)
				{
					var settingBool = BeyondOurReachModSettings.SettingsDict[modSetting];
					listing.CheckboxLabeled(modSetting.settingLabel, ref settingBool, modSetting.settingDesc);
					BeyondOurReachModSettings.SettingsDict[modSetting] = settingBool;
				}
			}
			finally
			{
				listing.End();
				Widgets.EndScrollView();
				WriteSettings();
			}
		}


		public override string SettingsCategory()
		{
			return Content.Name;
		}
	}
}
