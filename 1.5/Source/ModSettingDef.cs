using Verse;

namespace BeyondOurReachModSettings
{
#pragma warning disable CA1051
	public class ModSettingDef : Def
	{
		public string settingLabel;
		public string settingDesc;
		public string dependingModPackageID;
		public string excludingModPackageID;
	}
}
