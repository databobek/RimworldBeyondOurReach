using System.Collections.Generic;
using Verse;

namespace BeyondOurReach;

// The buildings BOR's work givers look for, so a job search never has to sweep every building on the map.
// They register on spawn whoever owns them, so the work givers still check the faction.
public class MapComponent_WorkBuildings : MapComponent
{
    public readonly List<CompForge> forges = new();
    public readonly List<CompDarkAltar> altars = new();
    public readonly List<Building_TenebralPod> pods = new();
    public readonly List<CompMeditationSpot> meditationSpots = new();
    public readonly List<CompBORDeepDrill> drills = new();

    public MapComponent_WorkBuildings(Map map) : base(map)
    {
    }

    public static MapComponent_WorkBuildings Of(Map map) => map.GetComponent<MapComponent_WorkBuildings>();
}
