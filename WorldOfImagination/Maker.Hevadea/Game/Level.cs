﻿using Maker.Hevadea.Game.Entities;
using Maker.Hevadea.Game.SaveStorage;
using Maker.Hevadea.Game.Tiles;
using Maker.Hevadea.Json;
using Maker.Rise.UI;
using Maker.Rise.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;

namespace Maker.Hevadea.Game
{
    public class Level
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public Color AmbiantLight { get; set; } = Color.Blue * 0.25f;

        private byte[] Tiles;
        private Dictionary<string, object>[] TilesData;

        public List<Entity> Entities;
        public List<Entity>[,] EntitiesOnTiles;
        public Player Player;

        public Color NightColor = Color.Blue * 0.25f;
        public Color DayColor = Color.White;


        bool ItsNight = false;
        Animation dayNightTransition = new Animation { Speed = 0.003f };

        private Random Random;
        private World World;

        public Level(int w, int h)
        {
            Width = w;
            
            Height = h;
            Tiles = new byte[Width * Height];
            TilesData = new Dictionary<string, object>[Width * Height];
            Entities = new List<Entity>();
            EntitiesOnTiles = new List<Entity>[Width, Height];
            Random = new Random();

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    EntitiesOnTiles[x, y] = new List<Entity>();
                    TilesData[x + y * Width] = new Dictionary<string, object>();
                }
            }
        }

        // ENTITIES -----------------------------------------------------------

        public void AddEntity(Entity e)
        {
            if (e is Player p) { Player = p; };

            e.Removed = false;
            Entities.Add(e);

            e.Init(this, World);
            AddEntityToTile(e.GetTilePosition(), e);
        }

        public void RemoveEntity(Entity e)
        {
            Entities.Remove(e);
            RemoveEntityFromTile(e.GetTilePosition(), e);
        }

        private void AddEntityToTile(TilePosition p, Entity e)
        {
            if (p.X < 0 || p.Y < 0 || p.X >= Width || p.Y >= Height) return;
            EntitiesOnTiles[p.X, p.Y].Add(e);
        }

        private void RemoveEntityFromTile(TilePosition p, Entity e)
        {
            if (p.X < 0 || p.Y < 0 || p.X >= Width || p.Y >= Height) return;
            EntitiesOnTiles[p.X, p.Y].Remove(e);
        }

        public List<Entity> GetEntityOnTile(int tx, int ty)
        {
            if (tx < Width && ty < Height)
            {
                return EntitiesOnTiles[tx, ty];
            }
            else
            {
                return new List<Entity>();
            }
        }

        public List<Entity> GetEntitiesOnArea(EntityPosition p, int width, int height)
        {
            var result = new List<Entity>();

            var from = p.ToTilePosition();
            from.Y--;
            from.Y--;

            var to = new EntityPosition(p.X + width, p.Y + height).ToTilePosition();
            to.X++;
            to.Y++;

            for (int x = from.X; x < to.X; x++)
            {
                for (int y = from.Y; y < to.Y; y++)
                {
                    if (x < 0 || y < 0 || x >= Width || y >= Height) continue;

                    var entities = EntitiesOnTiles[x, y];

                    foreach (var i in entities)
                    {
                        if (i.IsColliding(p.X, p.Y, width, height)) { result.Add(i); }
                    }
                    
                }
            }

            return result;
        }

        // TILES --------------------------------------------------------------

        public Tile GetTile(TilePosition tPos)
        {
            return GetTile(tPos.X, tPos.Y);
        }

        public Tile GetTile(int tx, int ty)
        {
            if (tx< 0 || ty < 0 || tx>= Width || ty>= Height) return Tile.Rock;
            return Tile.Tiles[Tiles[tx + ty * Width]];
        }

        public void SetTile(int tx, int ty, byte id)
        {
            if (tx < 0 || ty < 0 || tx >= Width || ty >= Height) return;
            Tiles[tx + ty * Width] = id;
        }

        internal T GetTileData<T>(TilePosition tilePosition, string dataName, T defaultValue)
        {
            return GetTileData<T>(tilePosition.X, tilePosition.Y, dataName, defaultValue);
        }

        public T GetTileData<T>(int tx, int ty, string dataName, T defaultValue)
        {
            if (TilesData[tx + ty * Width].ContainsKey(dataName))
            {
                return (T)TilesData[tx + ty * Width][dataName];
            }

            TilesData[tx + ty * Width].Add(dataName, defaultValue);
            return defaultValue;
        }

        internal void SetTileData<T>(TilePosition tilePosition, string dataName, T value)
        {
            SetTileData<T>(tilePosition.X, tilePosition.Y, dataName, value);
        }

        public void SetTileData<T>(int tx, int ty, string dataName, T Value)
        {
            TilesData[tx + ty * Width][dataName] = Value;
        }

        // GAME LOOPS ---------------------------------------------------------

        public void Initialize(World world)
        {
            this.World = world;
        }

        public void Update(GameTime gameTime)
        {
            // Randome tick tiles.
            for (int i = 0; i < Width * Height / 50; i++)
            {
                var tx = Random.Next(Width);
                var ty = Random.Next(Height);
                GetTile(tx, ty).Update(this, tx, ty);
            }

            // Tick entities.
            for (int i = 0; i < Entities.Count; i++)
            {
                var e = Entities[i];

                var oldPosition = e.GetTilePosition();

                e.Update(gameTime);

                if (e.Removed)
                {
                    Entities.RemoveAt(i--);
                    RemoveEntityFromTile(oldPosition, e);
                }
                else
                {
                    var newPosition = e.GetTilePosition();

                    if (oldPosition != newPosition)
                    {
                        RemoveEntityFromTile(oldPosition, e);
                        AddEntityToTile(newPosition, e);
                    }
                }
            }

            // Ambiant light
            

            var time = ((World.Time % 60000) / 60000f) ;
            dayNightTransition.Update(gameTime);
            AmbiantLight = GetAmbiantLightColor(time);
        }


        private Color GetAmbiantLightColor(float time, float dayDuration = 0.5f, float nightDuration = 0.5f)
        {
            ItsNight = time > dayDuration;

            dayNightTransition.Show = time > (dayDuration - 0.05);
            

            var day = DayColor * (1f - dayNightTransition.SinLinear);
            var night = NightColor * dayNightTransition.SinLinear;

            //Console.WriteLine($"{(int)(time * 100), 3} {ItsNight} {day} {night} {dayNightTransition.SinLinear}");

            return new Color(
                day.R + night.R,
                day.G + night.G,
                day.B + night.B,
                day.A + night.A);

        }

        public void Draw(SpriteBatch sb, SpriteBatch lightSb, Camera camera, GameTime gameTime, bool showDebug, bool renderTiles = true, bool renderEntity = true)
        {
            var playerPos = Player.GetTilePosition();
            
            var distX = ((camera.GetWidth() / 2) / ConstVal.TileSize) + 4;
            var distY = ((camera.GetHeight() / 2) / ConstVal.TileSize) + 4;
            
            var beginX = Math.Max(0, playerPos.X - distX);
            var beginY = Math.Max(0, playerPos.Y - distY + 1);
            var endX = Math.Min(Width, playerPos.X + distX + 1);
            var endY = Math.Min(Height, playerPos.Y + distY + 1);

            List<Entity> EntityRenderList = new List<Entity>();

            for (int tx = beginX; tx < endX; tx++)
            {
                for (int ty = beginY; ty < endY; ty++)
                {
                    if (renderTiles) GetTile(tx, ty).Draw(sb, gameTime, this, new TilePosition(tx, ty));
                    EntityRenderList.AddRange(EntitiesOnTiles[tx, ty]);
                    if (showDebug) sb.DrawRectangle(new Rectangle(tx * ConstVal.TileSize + 1, ty * ConstVal.TileSize + 1, ConstVal.TileSize - 2, ConstVal.TileSize - 2), new Color(255,255,255));
                }
            }

            EntityRenderList.Sort((a, b) => (a.Y + a.Height).CompareTo(b.Y + b.Height));

            foreach (var e in EntityRenderList)
            {
                if (showDebug) sb.FillRectangle(e.ToRectangle(), new Color(255, 0, 0) * 0.45f);
                if (renderEntity) e.Draw(sb, gameTime);
                if (e.IsLightSource) lightSb.Draw(Ressources.img_light,new Rectangle(e.X - e.LightLevel + e.Width / 2, e.Y - e.LightLevel + e.Height / 2, e.LightLevel * 2, e.LightLevel * 2), e.LightColor);
            }

            if (camera.debugMode) sb.DrawRectangle(new Rectangle((int)camera.X - camera.GetWidth() / 2, (int)camera.Y - camera.GetHeight() / 2, camera.GetWidth(), camera.GetHeight()), Color.Red);
        }

        public static bool Save(Level level, string folderName)
        {
            var storedTile = new List<TileSaveStorage>();
            var storedEntity = new List<EntitySaveStorage>();
            var storedLevel = new LevelSaveStorage { Height = level.Height, Width = level.Width };

            for (int i = 0; i < level.Width * level.Height; i++)
            {
                storedTile.Add(new TileSaveStorage { ID = level.Tiles[i], Data = level.TilesData[i]});
            }

            foreach (var e in level.Entities)
            {
                storedEntity.Add(new EntitySaveStorage { Type = e.GetType().FullName, Data = e.ToJson() });
            }

            File.WriteAllText(folderName + "entities.json", storedEntity.ToJson());
            File.WriteAllText(folderName + "tiles.json", storedTile.ToJson());
            File.WriteAllText(folderName + "level.json", storedLevel.ToJson());

            return true;
        }

        public static Level Load(string fileName)
        {
            // TODO: level loading.
            return null;
        }
    }
}