using System.Xml;
using Verse;

namespace BeyondOurReachModSettings
{
	public class PatchOperationModSettings : PatchOperationPathed
	{
#pragma warning disable CS0649
		private PatchOperation enabled;
		private PatchOperation disabled;
		private string modSettingDef;
#pragma warning restore

		protected override bool ApplyWorker(XmlDocument xml)
		{
			if (modSettingDef.NullOrEmpty())
			{
				return true;
			}

			bool contentEnabled = BeyondOurReachModSettings.GetSettingValue(modSettingDef);
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
			return true;
		}
	}
}
