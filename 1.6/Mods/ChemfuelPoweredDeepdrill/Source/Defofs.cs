using RimWorld;
using Verse;

namespace BORChemfuelPoweredDeepdrill
{
	[DefOf]
	public static class Defofs
	{
		static Defofs()
		{
			DefOfHelper.EnsureInitializedInCtor(typeof(Defofs));
		}

		public static ThingDef BOR_CPD_FuelRodDeepdrill;
		public static ThingDef BOR_CPD_QuasarNeutroniumDeepdrill;
	}
}
