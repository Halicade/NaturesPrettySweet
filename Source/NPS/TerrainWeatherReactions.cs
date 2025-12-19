using Verse;

namespace TKKN_NPS;

public class TerrainWeatherReactions : DefModExtension
{
    public TerrainDef dryTerrain; //perm fix for wet soils getting bugged
    public TerrainDef floodTerrain;
    public int freezeAt;
    public TerrainDef freezeTerrain;
    public bool holdFrost;
    public bool isSalty;
    public float temperatureAdjust;
    public TerrainDef tideTerrain;
    public int wetAt;
    public TerrainDef wetTerrain;
}