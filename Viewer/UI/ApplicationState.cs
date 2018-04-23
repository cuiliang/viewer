﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data;

namespace Viewer.UI
{
    public class QueryEventArgs : EventArgs
    {
        public Query Query { get; }

        public QueryEventArgs(Query query)
        {
            Query = query;
        }
    }

    public class EntityEventArgs : EventArgs
    {
        /// <summary>
        /// Loaded entities
        /// </summary>
        public IEntityManager Entities { get; }

        /// <summary>
        /// Index of selected entity
        /// </summary>
        public int Index { get; }

        public EntityEventArgs(IEntityManager entities, int index)
        {
            Entities = entities;
            Index = index;
        }
    }

    public interface IApplicationState
    {
        /// <summary>
        /// Event called when user tries to execute a query.
        /// </summary>
        event EventHandler<QueryEventArgs> QueryExecuted;

        /// <summary>
        /// Event called when user tries to open an entity (i.e. to open presentation)
        /// </summary>
        event EventHandler<EntityEventArgs> EntityOpened;

        /// <summary>
        /// Open entity 
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="index"></param>
        void OpenEntity(IEntityManager entities, int index);

        /// <summary>
        /// Execute a query
        /// </summary>
        /// <param name="query">Query to execute</param>
        void ExecuteQuery(Query query);
    }

    [Export(typeof(IApplicationState))]
    public class ApplicationState : IApplicationState
    {
        public event EventHandler<QueryEventArgs> QueryExecuted;
        public event EventHandler<EntityEventArgs> EntityOpened;

        public void OpenEntity(IEntityManager entities, int index)
        {
            EntityOpened?.Invoke(this, new EntityEventArgs(entities, index));
        }

        public void ExecuteQuery(Query query)
        {
            QueryExecuted?.Invoke(this, new QueryEventArgs(query));
        }
    }
}