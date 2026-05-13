using Verse;

namespace BORChemfuelPoweredDeepdrill
{
	public class Workgiver_FuelRodDeepdrill : ChemfuelGeneratorDeepdrill.WorkGiver_ChemfuelGeneratorDeepDrill
	{
		public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(Defofs.BOR_CPD_FuelRodDeepdrill);
	}
}
