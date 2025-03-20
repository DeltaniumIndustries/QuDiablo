using XRL;
using XRL.World;
using XRL.World.ZoneBuilders;
using System.Text;

public class LaurusRuins : ZoneBuilderSandbox
{
    public int RuinLevel = 100;
    public string ZonesWide = "1d3";
    public string ZonesHigh = "1d2";

    public bool BuildZone(Zone Z)
    {
        ZoneManager zoneManager = The.ZoneManager;
        var buildingZoneTemplate = InitializeBuildingZoneTemplate(Z, zoneManager);

        ApplySemanticTagsToZone(Z, zoneManager);

        for (int i = 0; i < Z.Height; i++)
        {
            for (int j = 0; j < Z.Width; j++)
            {
                ProcessCell(Z.GetCell(j, i), buildingZoneTemplate.Template.Map[j, i]);
            }
        }

        Z.DebugDrawSemantics();

        bool bUnderground = Z.GetZoneZ() > 10;
        buildingZoneTemplate.BuildZone(Z, bUnderground);

        if (RuinLevel > 0)
        {
            new Ruiner().RuinZone(Z, RuinLevel, bUnderground, 100000, 100000);
        }

        Z.RebuildReachableMap();
        return true;
    }

    private BuildingZoneTemplate InitializeBuildingZoneTemplate(Zone Z, ZoneManager zoneManager)
    {
        var buildingZoneTemplate = zoneManager.GetZoneColumnProperty(Z.ZoneID, "Builder.Ruins.BuildingZoneTemplate") as BuildingZoneTemplate;

        if (buildingZoneTemplate == null)
        {
            buildingZoneTemplate = new BuildingZoneTemplate();
            buildingZoneTemplate.New(Z.Width, Z.Height, ZonesWide.RollCached(), ZonesHigh.RollCached());
            zoneManager.SetZoneColumnProperty(Z.ZoneID, "Builder.Ruins.BuildingZoneTemplate", buildingZoneTemplate);

            string semanticTags = GenerateSemanticTags(Z);
            zoneManager.SetZoneColumnProperty(Z.ZoneID, "Builder.Ruins.SemanticTags", semanticTags);
        }

        return buildingZoneTemplate;
    }

    private string GenerateSemanticTags(Zone Z)
    {
        string terrainBlueprint = Z.GetTerrainObject()?.Blueprint;
        string populationName = (terrainBlueprint == "TerrainRuins" || terrainBlueprint == "TerrainBaroqueRuins")
            ? "WorldRuinsSemantics"
            : "DefaultRuinsSemantics";

        var semanticTags = new StringBuilder();
        foreach (var item in PopulationManager.Generate(populationName))
        {
            if (semanticTags.Length > 0) semanticTags.Append(",");
            semanticTags.Append(item.Blueprint);
        }

        return semanticTags.Length > 0 ? semanticTags.ToString() : "*Default";
    }

    private void ApplySemanticTagsToZone(Zone Z, ZoneManager zoneManager)
    {
        string[] semanticArray = zoneManager.GetZoneColumnProperty(Z.ZoneID, "Builder.Ruins.SemanticTags", "*Default").ToString().Split(',');

        foreach (string semanticTag in semanticArray)
        {
            string populationName = semanticTag + "Replace";

            if (PopulationManager.HasTable(populationName))
            {
                string blueprint = PopulationManager.RollOneFrom(populationName).Blueprint;
                if (blueprint != "*None")
                {
                    Z.AddSemanticTag(blueprint == "Extra" ? "Extra" + semanticTag : semanticTag);
                }
            }
            else
            {
                Z.AddSemanticTag(semanticTag);
            }
        }
    }

    private void ProcessCell(Cell cell, BuildingTemplateTile tileType)
    {
        switch (tileType)
        {
            case BuildingTemplateTile.Inside:
                cell.AddSemanticTag("Inside");
                cell.AddSemanticTag("Room");
                cell.AddSemanticTag("Perimeter");
                break;
            case BuildingTemplateTile.Wall:
                cell.AddSemanticTag("Wall");
                cell.AddSemanticTag("Inner");
                cell.AddSemanticTag("Perimeter");
                break;
            case BuildingTemplateTile.Door:
                cell.AddSemanticTag("Door");
                cell.AddSemanticTag("Perimeter");
                break;
            case BuildingTemplateTile.OutsideWall:
                cell.AddSemanticTag("Wall");
                cell.AddSemanticTag("Outer");
                break;
            case BuildingTemplateTile.StairsUp:
            case BuildingTemplateTile.StairsDown:
                cell.AddSemanticTag("Connection");
                cell.AddSemanticTag("Stairs");
                cell.AddSemanticTag("Up");
                break;
            case BuildingTemplateTile.Outside:
                cell.AddSemanticTag("Outside");
                break;
            case BuildingTemplateTile.Void:
                cell.AddSemanticTag("Inside");
                cell.AddSemanticTag("Isolated");
                break;
        }
    }
}
