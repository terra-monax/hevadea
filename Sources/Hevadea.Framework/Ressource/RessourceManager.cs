﻿using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Hevadea.Framework.Ressource
{
    public class RessourceManager
    {
        private Dictionary<string, SpriteFont> FontCache = new Dictionary<string, SpriteFont>();
        private Dictionary<string, Texture2D> TextureCache = new Dictionary<string, Texture2D>();

        public SoundEffect GetSoundEffect(string name)
        {
            return Rise.MonoGame.Content.Load<SoundEffect>($"Sounds/{name}");
        }

        public SpriteFont GetSpriteFont(string name)
        {
            if (!FontCache.ContainsKey(name))
            {
                FontCache.Add(name, Rise.MonoGame.Content.Load<SpriteFont>($"Fonts/{name}"));
            }

            return FontCache[name];
        }

        public Texture2D GetIcon(string name)
        {
            if (!TextureCache.ContainsKey("icon:" + name))
            {
                TextureCache.Add("icon:" + name, Rise.MonoGame.Content.Load<Texture2D>($"Icons/{name}"));
            }

            return TextureCache["icon:" + name];
        }

        public Texture2D GetImage(string name)
        {
            if (!TextureCache.ContainsKey("img:" + name))
            {
                TextureCache.Add("img:" + name, Rise.MonoGame.Content.Load<Texture2D>($"Images/{name}"));
            }

            return TextureCache["img:" + name];
        }
    }
}