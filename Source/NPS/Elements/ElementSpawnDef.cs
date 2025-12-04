using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TKKN_NPS;

public class ElementSpawnDef : Def
{
    public List<BiomeDef> allowedBiomes=[];
    //public bool allowOnWater;
    public List<BiomeDef> forbiddenBiomes=[];
    /*
    public List<string> terrainValidationAllowed;
    public List<string> terrainValidationDisallowed;*/
    public ThingDef thingDef;
    public float commonality;
}