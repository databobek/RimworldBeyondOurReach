<<<<<<< HEAD
﻿using RimWorld;
using System.Collections.Generic;
using Verse;
=======
﻿using Verse;
>>>>>>> b058a259e3ce136fafaedc332d9e0c7dc9f61558

namespace BORChemfuelPoweredDeepdrill
{
	public class Workgiver_FuelRodDeepdrill : ChemfuelGeneratorDeepdrill.WorkGiver_ChemfuelGeneratorDeepDrill
	{
		public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(Defofs.BOR_CPD_FuelRodDeepdrill);
<<<<<<< HEAD


		public override bool ShouldSkip(Pawn pawn, bool forced = false)
		{
			List<Building> allBuildingsColonist = pawn.Map.listerBuildings.allBuildingsColonist;
			for (int i = 0; i < allBuildingsColonist.Count; i++)
			{
				Building building = allBuildingsColonist[i];
				if (building.def == Defofs.BOR_CPD_FuelRodDeepdrill)
				{
					CompPowerTrader comp = building.GetComp<CompPowerPlant>();
					if ((comp == null || comp.PowerOutput > 0) && building.Map.designationManager.DesignationOn(building, DesignationDefOf.Uninstall) == null)
					{
						return false;
					}
				}
			}
			return true;
		}
=======
>>>>>>> b058a259e3ce136fafaedc332d9e0c7dc9f61558
	}
}
