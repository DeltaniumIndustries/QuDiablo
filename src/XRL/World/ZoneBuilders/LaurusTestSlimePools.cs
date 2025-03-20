// Decompiled with JetBrains decompiler
// Type: XRL.World.ZoneBuilders.SlimePools
// Assembly: Assembly-CSharp, Version=2.0.209.44, Culture=neutral, PublicKeyToken=null
// MVID: BA3BEED9-964F-45DE-99ED-0720870557CB
// Assembly location: C:\Program Files (x86)\Steam\steamapps\common\Caves of Qud\CoQ_Data\Managed\Assembly-CSharp.dll
// XML documentation location: C:\Program Files (x86)\Steam\steamapps\common\Caves of Qud\CoQ_Data\Managed\Assembly-CSharp.xml

using System.Collections.Generic;
using XRL.Core;
using XRL.World.ZoneBuilders.Utility;

#nullable disable
namespace XRL.World.ZoneBuilders
{
  public class LaurusTestSlimePools
  {
    public bool BuildZone(Zone Z)
    {
      List<NoiseMapNode> ExtraNodes = new List<NoiseMapNode>();
      foreach (ZoneConnection zoneConnection in XRLCore.Core.Game.ZoneManager.GetZoneConnections(Z.ZoneID))
        ExtraNodes.Add(new NoiseMapNode(zoneConnection.X, zoneConnection.Y));
      NoiseMap noiseMap = new NoiseMap(80, 25, 10, 3, 3, 6, 20, 20, 4, 3, 0, 1, ExtraNodes);
      int num = -1;
      for (int key = 0; key < noiseMap.nAreas; ++key)
      {
        if (noiseMap.AreaNodes[key].Count > num)
          num = noiseMap.AreaNodes[key].Count;
      }
      for (int y = 0; y < Z.Height; ++y)
      {
        for (int x = 0; x < Z.Width; ++x)
        {
          if (noiseMap.Noise[x, y] > 1 && Z.GetCell(x, y).IsEmpty())
            Z.GetCell(x, y).AddObject("SlimePuddle");
        }
      }
      return true;
    }
  }
}
