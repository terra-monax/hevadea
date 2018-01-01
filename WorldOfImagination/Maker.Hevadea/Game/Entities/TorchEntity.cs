﻿using Maker.Rise.Ressource;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Maker.Hevadea.Game.Entities
{
    public class TorchEntity : Entity
    {
        Sprite sprite;
        public TorchEntity()
        {
            Height = 2;
            Width = 2;

            IsLightSource = true;
            LightColor = Color.White;
            LightLevel = 72;

            sprite = new Sprite(Ressources.tile_entities, 0, new Point(16, 16));
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            sprite.DrawSubSprite(spriteBatch, new Vector2(X - 7, Y - 14), new Point(1, 1), Color.White);
        }

    }
}
