﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MetadataExtractor;
using Viewer.Data.Storage;

namespace Viewer.Data
{
    /// <summary>
    /// Class which implements this interface is responsible for maintaining a consistent state 
    /// of entities throughout the application.
    /// This type is thread safe.
    /// </summary>
    public interface IEntityManager
    {
        /// <summary>
        /// Load it from an attribute storage.
        /// </summary>
        /// <param name="path">Path to an entity</param>
        /// <returns>Loaded entity</returns>
        IEntity GetEntity(string path);

        /// <summary>
        /// Add the entity to the modified list.
        /// If an entity with the same path is in the list already, it will be replaced iff <paramref name="replace"/> is true.
        /// </summary>
        /// <param name="entity">Entity to set</param>
        /// <param name="replace">Replace entity in the modified list if true.</param>
        void SetEntity(IEntity entity, bool replace);

        /// <summary>
        /// Move <paramref name="entity"/> to <paramref name="newPath"/>
        /// in the storage and in the modified list.
        /// This will set entity path to <paramref name="newPath"/> if the move was successful.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="newPath"></param>
        void MoveEntity(IEntity entity, string newPath);

        /// <summary>
        /// Remove entity in the storage and in the modified list.
        /// </summary>
        /// <param name="entity">Entity to remove</param>
        void RemoveEntity(IEntity entity);

        /// <summary>
        /// Get a snapshot of unsaved modified entities.
        /// All entities in the snapshot are copies.
        /// </summary>
        /// <returns>Snapshot of modified entities</returns>
        IReadOnlyList<IEntity> GetModified();
    }
    
    [Export(typeof(IEntityManager))]
    public class EntityManager : IEntityManager
    {
        private readonly ConcurrentDictionary<string, IEntity> _modified = new ConcurrentDictionary<string, IEntity>();
        private readonly IAttributeStorage _storage;

        [ImportingConstructor]
        public EntityManager(IAttributeStorage storage)
        {
            _storage = storage;
        }

        public IEntity GetEntity(string path)
        {
            return _storage.Load(path);
        }

        public void SetEntity(IEntity entity, bool replace)
        {
            var path = entity.Path;
            var clone = entity.Clone();
            if (replace)
            {
                _modified[path] = clone;
            }
            else
            {
                _modified.TryAdd(path, clone);
            }
        }

        public void MoveEntity(IEntity entity, string newPath)
        {
            _storage.Move(entity, newPath);

            var oldPath = entity.Path;
            if (_modified.TryGetValue(oldPath, out var modifiedEntity))
            {
                _modified.TryRemove(oldPath, out var oldEntity);
                _modified[newPath] = modifiedEntity;
            }
            entity.ChangePath(newPath);
        }

        public void RemoveEntity(IEntity entity)
        {
            _storage.Remove(entity);

            _modified.TryRemove(entity.Path, out var _);
        }

        public IReadOnlyList<IEntity> GetModified()
        {
            var snapshot = _modified.Values.ToArray();
            _modified.Clear();
            return snapshot;
        }
    }
}