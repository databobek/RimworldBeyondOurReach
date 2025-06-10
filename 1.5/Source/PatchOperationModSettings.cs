using System.Xml;
using Verse;

namespace BeyondOurReachModSettings
{
#pragma warning disable CS0649
	public class PatchOperationModSettings : PatchOperationPathed
	{
		private PatchOperation enabled;
		private PatchOperation disabled;
		private string modSettingDef;

		protected override bool ApplyWorker(XmlDocument xml)
		{
			var contentEnabled = BeyondOurReachModSettings.SettingsDict.TryGetValue(modSettingDef);
			if (contentEnabled)
			{
				if (enabled != null)
				{
					return enabled.Apply(xml);
				}
			}
			else if (disabled != null)
			{
				return disabled.Apply(xml);
			}
			return false;
		}
	}
}
