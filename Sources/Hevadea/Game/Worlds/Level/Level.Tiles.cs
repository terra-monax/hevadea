﻿using Hevadea.Game.Registry;
using Hevadea.Game.Tiles;
using Microsoft.Xna.Framework;

namespace Hevadea.Game.Worlds
{
    public partial class Level
    {
        public bool IsAll(Tile tile, Rectangle rectangle)
        {
            var result = true;

            var beginX = rectangle.X / ConstVal.TileSize - 1;
            var beginY = rectangle.Y / ConstVal.TileSize - 1;

            var endX = (rectangle.X + rectangle.Width) / ConstVal.TileSize + 1;
            var endY = (rectangle.Y + rectangle.Height) / ConstVal.TileSize + 1;


            for (var x = beginX; x < endX; x++)
            for (var y = beginY; y < endY; y++)
            {
                if (x < 0 || y < 0 || x >= Width || y >= Height) continue;
                result &= GetTile(x, y) == tile;
            }

            return result;
        }
        
        public bool IsAll<T>(Rectangle rectangle) where T: TileTag
        {

            var beginX = rectangle.X / ConstVal.TileSize;
            var beginY = rectangle.Y / ConstVal.TileSize;

            var endX = (rectangle.X + rectangle.Width) / ConstVal.TileSize;
            var endY = (rectangle.Y + rectangle.Height) / ConstVal.TileSize;


            bool result =  GetTile(beginX, beginY).HasTag<T>();;
            for (var x = beginX; x <= endX; x++)
            for (var y = beginY; y <= endY; y++)
            {
                if (x < 0 || y < 0 || x >= Width || y >= Height) continue;
                result &= GetTile(x, y).HasTag<T>();
            }

            return result;
        }

        public Tile GetTile(TilePosition tPos)
        {
            return GetTile(tPos.X, tPos.Y);
        }

        public Tile GetTile(int tx, int ty)
        {
            if (tx < 0 || ty < 0 || tx >= Width || ty >= Height) return TILES.WATER;
            return TILES.ById[_tiles[tx + ty * Width]];
        }

        public bool SetTile(TilePosition pos, Tile tile)
        {
            return SetTile(pos.X, pos.Y, tile.Id);
        }

        public bool SetTile(int tx, int ty, Tile tile)
        {
            return SetTile(tx, ty, tile.Id);
        }

        public bool SetTile(int tx, int ty, int id)
        {
            if (tx < 0 || ty < 0 || tx >= Width || ty >= Height) return false;
            _tiles[tx + ty * Width] = id;
            return true;
        }

        public void ClearTileData(TilePosition tilePosition) => ClearTileData(tilePosition.X, tilePosition.Y);

        public void ClearTileData(int tx, int ty)
        {
            _tilesData[tx + ty * Width].Clear();
        }

        public T GetTileData<T>(TilePosition tilePosition, string dataName, T defaultValue)
        {
            return GetTileData(tilePosition.X, tilePosition.Y, dataName, defaultValue);
        }

        public T GetTileData<T>(int tx, int ty, string dataName, T defaultValue)
        {
            if (_tilesData[tx + ty * Width].ContainsKey(dataName)) return (T) _tilesData[tx + ty * Width][dataName];

            _tilesData[tx + ty * Width].Add(dataName, defaultValue);
            return defaultValue;
        }

        internal void SetTileData<T>(TilePosition tilePosition, string dataName, T value)
        {
            SetTileData(tilePosition.X, tilePosition.Y, dataName, value);
        }

        public void SetTileData<T>(int tx, int ty, string dataName, T value)
        {
            _tilesData[tx + ty * Width][dataName] = value;
        }   
    }
}