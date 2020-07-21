﻿using System.Linq;
using System.Collections.Generic;
using BruTile;
using Mapsui.Fetcher;
using Mapsui.Geometries.Utilities;

namespace MapsuiTest
{
    public class CustomFetchStrategy : IDataFetchStrategy
    {
        private readonly int _maxLevelsUp;

        public CustomFetchStrategy(int maxLevelsUp = int.MaxValue)
        {
            _maxLevelsUp = maxLevelsUp;
        }

        public IList<TileInfo> Get(ITileSchema schema, Extent extent, int level)
        {
            var tileInfos = new List<TileInfo>();
            // Iterating through all levels from current to zero. If lower levels are
            // not available the renderer can fall back on higher level tiles. 
            var resolution = schema.Resolutions[level].UnitsPerPixel;
            var levels = schema.Resolutions.Where(k => k.Value.UnitsPerPixel >= resolution).OrderBy(x => x.Value.UnitsPerPixel).ToList();

            var counter = 0;
            foreach (var l in levels)
            {
                if (counter > _maxLevelsUp) break;

                var tileInfosForLevel = schema.GetTileInfos(extent, l.Key).OrderBy(
                    t => Algorithms.Distance(extent.CenterX, extent.CenterY, t.Extent.CenterX, t.Extent.CenterY));

                tileInfos.AddRange(tileInfosForLevel);
                counter++;
            }

            return tileInfos;
        }
    }

}
