﻿using System;
using Maker.Hevadea.Game.Entities;
using Maker.Hevadea.Game.Items;
using Maker.Hevadea.Game.Registry;
using Maker.Rise;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Maker.Hevadea.Game.Entities.Component;

namespace Maker.Hevadea.Game.Tiles
{
    public static class Tags
    {
        #region interaction

        /// <summary>
        /// Allow the tile to be break by entities.
        /// </summary>
        public class Breakable : TileTag
        {
            public Tile ReplacementTile { get; set; } = TILES.VOID;

            public void Break(TilePosition position, Level level)
            {
                level.SetTile(position, ReplacementTile);
                AttachedTile.Tag<Droppable>()?.Drop(position, level);
            }
        }

        /// <summary>
        /// Allow the tile to get damages from entities.
        /// </summary>
        public class Damage : TileTag
        {
            public Tile ReplacementTile { get; set; } = TILES.VOID;
            public float MaxDamages { get; set; } = 5f;

            public void Hurt(float damages, TilePosition position, Level level)
            {
                var dmg = level.GetTileData(position, "damages", 0f) + damages;
                if (dmg > MaxDamages)
                {
                    level.SetTile(position, ReplacementTile);
                    AttachedTile.Tag<Droppable>()?.Drop(position, level);
                }
                else
                {
                    level.SetTileData(position, "damages", dmg);
                }
            }

        }

        /// <summary>
        /// Allow entities to interacte width the tile.
        /// </summary>
        public class Interactable : TileTag
        {
        }

        /// <summary>
        /// Alow the tile to loot item wen damaged or break.
        /// </summary>
        public class Droppable : TileTag
        {
            public List<(Item Item, int Min, int Max)> Items { get; set; } = new List<(Item, int, int)>();

            public Droppable() { }
            public Droppable(params (Item Item, int Min, int Max)[] items)
            {
                foreach (var item in items)
                {
                    Items.Add(item);
                }
            }

            public void Drop(TilePosition position, Level level)
            {
                foreach (var d in Items) d.Item.Drop(level, position, Engine.Random.Next(d.Min, d.Max));
            }
        }
        #endregion

        #region Physic

        /// <summary>
        /// Make the tile solide, entity connot pass througt.
        /// </summary>
        public class Solide : TileTag
        {
            public virtual bool CanPassThrought(Entity entity)
            {
                return false;
            }
        }

        public class Liquide : Solide
        {
            public override bool CanPassThrought(Entity entity)
            {
                return entity.Components.Has<Swim>();
            }
        }
        
        /// <summary>
        /// Allow to set te movement speed on the tile.
        /// </summary>
        public class Ground : TileTag
        {
            public float MoveSpeed { get; set; } = 1f;
            public virtual void SteppedOn(Entity entity, TilePosition position) { }
        }
        #endregion

        #region Behavior

        /// <summary>
        /// The tile spread.
        /// ex: Grass, Water
        /// </summary>
        public class Spread : TileTag, IUpdatableTag
        {

            public List<Tile> SpreadTo { get; set; } = new List<Tile>();
            public int SpreadChance { get; set; } = 10;

            public void Update(Tile tile, TilePosition position, Dictionary<string, object> data, Level level, GameTime gameTime)
            {
                if (Engine.Random.Next(SpreadChance) == 0)
                {
                    var d = (Direction)Engine.Random.Next(0, 4);
                    var p = d.ToPoint();

                    if (SpreadTo.Contains(level.GetTile(position.X + p.X, position.Y + p.Y))) level.SetTile(position.X + p.X, position.Y + p.Y, AttachedTile);
                }
            }
        }
        #endregion
    }
}
