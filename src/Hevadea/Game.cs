﻿using Hevadea.Framework;
using Hevadea.Framework.Networking;
using Hevadea.Framework.Threading;
using Hevadea.Framework.Utils;
using Hevadea.Framework.Utils.Json;
using Hevadea.GameObjects;
using Hevadea.GameObjects.Entities;
using Hevadea.Multiplayer;
using Hevadea.Scenes.Menus;
using Hevadea.Storage;
using Hevadea.Worlds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;

namespace Hevadea
{
    public class Game
    {
        public static readonly int Unit = 16;
        public static readonly string Name = "Hevadea";
        public static readonly string Version = "0.1.0";
        public static readonly int VersionNumber = 1;

        public static string GetSaveFolder()
        {
            return Rise.Platform.GetStorageFolder() + "/Saves/";
        }


        public bool IsClient => this is RemoteGame;
        public bool IsServer => this is HostGame;

        public bool IsLocal => !IsClient && !IsServer;

        public bool IsMaster => IsLocal || IsServer; 

        Menu _currentMenu;
        LevelSpriteBatchPool _spriteBatchPool = new LevelSpriteBatchPool();

        public string SavePath { get; set; } = "./test/";

        public Camera Camera { get; set; }
        public Player MainPlayer { get; set; }
        public World World { get; set; }

        public List<Player> Players { get; } = new List<Player>();
        public PlayerInputHandler PlayerInput { get; set; }
        
        public Menu CurrentMenu { get => _currentMenu; set { CurrentMenuChange?.Invoke(_currentMenu, value); _currentMenu = value; } }

        public delegate void CurrentMenuChangeHandler(Menu oldMenu, Menu newMenu);
        public event CurrentMenuChangeHandler CurrentMenuChange;

        // --- Initialize, Update and Draw ---------------------------------- // 

        public void Initialize()
        {
            Logger.Log<Game>("Initializing...");
            World.Initialize(this);
            CurrentMenu = new MenuInGame(this);

            if (MainPlayer.Removed)
            {
                if (MainPlayer.X == 0f && MainPlayer.Y == 0f)
                    World.SpawnPlayer(MainPlayer);
                else
                    World.GetLevel(MainPlayer.LastLevel).AddEntity(MainPlayer);
            }

            PlayerInput = new PlayerInputHandler(MainPlayer);
            Camera = new Camera(MainPlayer);
            Camera.JumpToFocusEntity();
        }

        public void Draw(GameTime gameTime)
        {
            Camera.FocusEntity.Level.Draw(_spriteBatchPool, gameTime);
        }

        public void Update(GameTime gameTime)
        {
            Camera.Update(gameTime);
            PlayerInput.Update(gameTime);

            World.DayNightCycle.UpdateTime(gameTime.ElapsedGameTime.TotalSeconds);
            MainPlayer.Level.Update(gameTime);

        }

        // --- Path generator ----------------------------------------------- // 

        public string GetSavePath()
            => $"{SavePath}/";

        public string GetLevelSavePath(Level level)
            => $"{SavePath}/{level.Name}/";

        public string GetLevelMinimapSavePath(Level level)
            => $"{SavePath}/{level.Name}/minimap.png";

        public string GetLevelMinimapDataPath(Level level)
            => $"{SavePath}/{level.Name}/minimap.json";


        // --- Save and load ------------------------------------------------ // 

        public static Game Load(string saveFolder, ProgressRepporter progressRepporter)
        {
            Game game = new Game();
            game.SavePath = saveFolder;

            progressRepporter.RepportStatus("Loading world...");

            string path = game.GetSavePath();

            WorldStorage worldStorage = File.ReadAllText(path + "world.json").FromJson<WorldStorage>();
            World world = World.Load(worldStorage);
            Entity player = EntityFactory.PLAYER.Construct().Load(File.ReadAllText(path + "player.json").FromJson<EntityStorage>());


            foreach (var levelName in worldStorage.Levels)
            {
                world.Levels.Add(LoadLevel(game, levelName, progressRepporter));
            }

            game.World = world;
            game.MainPlayer = (Player)player;

            return game;
        }

        public static Level LoadLevel(Game game, string levelName, ProgressRepporter progressRepporter )
        {
            string levelPath = $"{game.GetSavePath()}{levelName}/";
            Level level = Level.Load(File.ReadAllText(levelPath + "level.json").FromJson<LevelStorage>());

            progressRepporter.RepportStatus($"Loading level {level.Name}...");
            for (int x = 0; x < level.Chunks.GetLength(0); x++)
            {
                for (int y = 0; y < level.Chunks.GetLength(1); y++)
                {
                    level.Chunks[x, y] = Chunk.Load(File.ReadAllText(levelPath + $"r{x}-{y}.json").FromJson<ChunkStorage>());
                    progressRepporter.Report((x * level.Chunks.GetLength(1) + y) / (float)level.Chunks.Length);
                }
            }

            level.Minimap.Waypoints = File.ReadAllText(levelPath + "minimap.json").FromJson<List<MinimapWaypoint>>();

            var task = new AsyncTask(() =>
            {
                var fs = new FileStream(levelPath + "minimap.png", FileMode.Open);
                level.Minimap.Texture = Texture2D.FromStream(Rise.MonoGame.GraphicsDevice, fs);
                fs.Close();
            });

            Rise.AsyncTasks.Enqueue(task);

            while (!task.Done)
            {
                // XXX: Hack to fix the soft lock when loading the world.
                System.Threading.Thread.Sleep(10);
            }

            return level;
        }

        public void Save(string savePath, ProgressRepporter progressRepporter)
        {
            SavePath = savePath;

            progressRepporter.RepportStatus("Saving world...");

            var levelsName = new List<string>();

            Directory.CreateDirectory(SavePath);

            foreach (var level in World.Levels)
            {
                SaveLevel(level, progressRepporter);
            }


            File.WriteAllText(GetSavePath() + "world.json", World.Save().ToJson());
            File.WriteAllText(GetSavePath() + "player.json", MainPlayer.Save().ToJson());
        }

        private void SaveLevel(Level level, ProgressRepporter progressRepporter)
        {
            progressRepporter.RepportStatus($"Saving {level.Name}...");
            string path = GetLevelSavePath(level);
            Directory.CreateDirectory(path);

            File.WriteAllText(path + "level.json", level.Save().ToJson());

            foreach (var chunk in level.Chunks)
            {
                progressRepporter.Report((chunk.X * level.Chunks.GetLength(1) + chunk.Y) / (float)level.Chunks.Length);
                File.WriteAllText(path + $"r{chunk.X}-{chunk.Y}.json", chunk.Save().ToJson());
            }

            File.WriteAllText(path + "minimap.json", level.Minimap.Waypoints.ToJson());

            var task = new AsyncTask(() =>
            {
                var fs = new FileStream(path + "minimap.png", FileMode.OpenOrCreate);
                level.Minimap.Texture.SaveAsPng(fs, level.Width, level.Height);
                fs.Close();
            });

            progressRepporter.RepportStatus($"Saving {level.Name} minimap...");
            progressRepporter.Report(1f);
            Rise.AsyncTasks.Enqueue(task);

            while (!task.Done)
            {
                // XXX: Hack to fix the soft lock when saving the world.
                System.Threading.Thread.Sleep(10);
            }
        }
    }
}