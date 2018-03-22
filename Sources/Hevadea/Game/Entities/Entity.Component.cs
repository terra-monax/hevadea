﻿using Hevadea.Game.Entities.Components;
using System.Collections.Generic;
using System.Linq;

namespace Hevadea.Game.Entities
{
    public partial class Entity
    {
        private readonly List<EntityComponent> _components = new List<EntityComponent>();

        public T AddComponent<T>(T component) where T : EntityComponent
        {
            if (_components.Any(e => e == component)) return null;

            _components.Add(component);
            component.Owner = this;

            _components.Sort((a, b) => (0xff - a.Priority).CompareTo(0xff - b.Priority));

            return component;
        }

        public T GetComponent<T>() where T : EntityComponent
        {
            for (int i = 0; i < _components.Count; i++)
            {
                if (_components[i] is T component)
                {
                    return component;
                }
            }

            return null;
        }

        public bool HasComponent<T>() where T : EntityComponent
        {
            return _components.OfType<T>().Any();
        }
    }
}