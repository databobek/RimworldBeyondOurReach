using Verse;
using UnityEngine;
using System.Collections.Generic;

namespace BeyondOurReachModSettings
{
	public class BeyondOurReachMod : Mod
	{
		private static Vector2 ScrollPos = Vector2.zero;
		private readonly BeyondOurReachModSettings settings;
		List<ModSettingDef> cachedDefs;


		public BeyondOurReachMod(ModContentPack content) : base(content)
		{
			this.settings ??= GetSettings<BeyondOurReachModSettings>();
		}


		public override void DoSettingsWindowContents(Rect inRect)
		{
			if (cachedDefs is null)
			{
				cachedDefs = [];
				foreach (ModSettingDef def in DefDatabase<ModSettingDef>.AllDefsListForReading)
				{
					cachedDefs.Add(def);
					BeyondOurReachModSettings.SettingsDict.TryAdd(def.defName, true);
				}
			}

			base.DoSettingsWindowContents(inRect);

			Rect scrollRect = inRect.ContractedBy(32f);
			scrollRect.height = inRect.height;

			Widgets.BeginScrollView(inRect, ref ScrollPos, scrollRect);
			Listing_Standard listing = new();
			listing.Begin(scrollRect);

			try
			{
				foreach (var modSetting in cachedDefs)
				{
					var settingBool = BeyondOurReachModSettings.SettingsDict[modSetting.defName];

					bool hasExcludingMod = modSetting.excludingModPackageID is not null;
					bool hasDependingMod = modSetting.dependingModPackageID is not null;
					bool isDependingModActive = hasDependingMod && ModsConfig.IsActive(modSetting.dependingModPackageID);
					bool isExcludingModInactive = hasExcludingMod && !ModsConfig.IsActive(modSetting.excludingModPackageID);

					if (isDependingModActive || !hasDependingMod && !hasExcludingMod || isExcludingModInactive)
					{
						EnabledOption(listing, modSetting, settingBool);
					}
					else
					{
						DisabledOption(listing, modSetting);
					}
				}
			}
			finally
			{
				listing.End();
				Widgets.EndScrollView();
			}
		}


		private static void EnabledOption(Listing_Standard listing, ModSettingDef modSetting, bool settingBool)
		{
			listing.CheckboxLabeled(modSetting.settingLabel, ref settingBool, modSetting.settingDesc);
			BeyondOurReachModSettings.SettingsDict[modSetting.defName] = settingBool;
		}

		private static void DisabledOption(Listing_Standard listing, ModSettingDef modSetting)
		{
			using (new TextBlock(Color.grey))
			{
				listing.Label(modSetting.settingLabel);
			}
		}


		public override string SettingsCategory()
		{
			return Content.Name;
		}
	}
}
