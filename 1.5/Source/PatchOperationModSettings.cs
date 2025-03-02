using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Verse;

namespace BeyondOurReachModSettings
{
#pragma warning disable CS0649
	public class PatchOperationModSettings : PatchOperationPathed
	{
		private PatchOperation match;
		private PatchOperation nomatch;
		private ModSettingDef modSettingDef;

		protected override bool ApplyWorker(XmlDocument xml)
		{
			var contentEnabled = BeyondOurReachModSettings.SettingsDict.TryGetValue(modSettingDef);

			if (contentEnabled)
			{
				if (match != null)
				{
					return match.Apply(xml);
				}
			}
			else if (nomatch != null)
			{
				return nomatch.Apply(xml);
			}
			return true;
		}
	}
}
