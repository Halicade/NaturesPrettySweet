using Verse;

namespace TKKN_NPS;

public class TerrainWeatherReactions : DefModExtension
{
    public TerrainDef floodTerrain;
    public int freezeAt;
    public freezeTerrain freezeTerrain;
    public bool holdFrost;
    public bool isSalty;
    public float temperatureAdjust;
    public TerrainDef tideTerrain;
    public int wetAt;
    public TerrainDef wetTerrain;
}

public class freezeTerrain
{
    public TerrainDef terrain;
    public int freezeAt;
}