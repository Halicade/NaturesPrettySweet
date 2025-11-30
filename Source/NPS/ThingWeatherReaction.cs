using System.Collections.Generic;
using Verse;

namespace TKKN_NPS;

public class ThingWeatherReaction : DefModExtension
{
    public List<TerrainDef> allowedTerrains;
    public string droughtGraphicPath;
    public string floweringGraphicPath;
    public string frostGraphicPath;
    public string frostLeaflessGraphicPath;
    public string snowGraphicPath;
    public string snowLeaflessGraphicPath;

    public Graphic droughtGraphic;
    public Graphic floweringGraphic;
    public Graphic frostGraphic;
    public Graphic frostLeaflessGraphic;
    public Graphic snowGraphic;
    public Graphic snowLeaflessGraphic;

    public bool hasGraphic;

    public bool initializeGraphics(ThingDef plant) {
        if (!droughtGraphicPath.NullOrEmpty()) {
            droughtGraphic = GraphicDatabase.Get(plant.graphicData.graphicClass, droughtGraphicPath,
                plant.graphic.Shader,
                plant.graphicData.drawSize, plant.graphicData.color,
                plant.graphicData.colorTwo);
            hasGraphic = true;
        }

        if (!floweringGraphicPath.NullOrEmpty()) {
            floweringGraphic = GraphicDatabase.Get(plant.graphicData.graphicClass, floweringGraphicPath,
                plant.graphic.Shader,
                plant.graphicData.drawSize, plant.graphicData.color,
                plant.graphicData.colorTwo);
            hasGraphic = true;
        }

        if (!frostGraphicPath.NullOrEmpty()) {
            frostGraphic = GraphicDatabase.Get(plant.graphicData.graphicClass, frostGraphicPath,
                plant.graphic.Shader,
                plant.graphicData.drawSize, plant.graphicData.color,
                plant.graphicData.colorTwo);
            hasGraphic = true;
        }

        if (!frostLeaflessGraphicPath.NullOrEmpty()) {
            frostLeaflessGraphic = GraphicDatabase.Get(plant.graphicData.graphicClass, frostLeaflessGraphicPath,
                plant.graphic.Shader,
                plant.graphicData.drawSize, plant.graphicData.color,
                plant.graphicData.colorTwo);
            hasGraphic = true;
        }

        if (!snowGraphicPath.NullOrEmpty()) {
            snowGraphic = GraphicDatabase.Get(plant.graphicData.graphicClass, snowGraphicPath,
                plant.graphic.Shader,
                plant.graphicData.drawSize, plant.graphicData.color,
                plant.graphicData.colorTwo);
            hasGraphic = true;
        }

        if (!snowLeaflessGraphicPath.NullOrEmpty()) {
            droughtGraphic = GraphicDatabase.Get(plant.graphicData.graphicClass, snowLeaflessGraphicPath,
                plant.graphic.Shader,
                plant.graphicData.drawSize, plant.graphicData.color,
                plant.graphicData.colorTwo);
            hasGraphic = true;
        }

        return hasGraphic;
    }
}