﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Cloudy
{
    abstract class DomainObject
    {
        Dictionary<Type, Action<DomainEvent>> handlers = new Dictionary<Type, Action<DomainEvent>>();
        List<DomainEvent>? events;
        List<DomainEvent>? history;

        /// <summary>
        /// Gets the identifier for the domain object.
        /// </summary>
        protected abstract string Id { get; }

        // This is basically a sort of memento pattern to get the 
        // observable state changes for the object but represented 
        // as a list of events.
        [System.Text.Json.Serialization.JsonIgnore]
        [JsonIgnore]
        public IReadOnlyList<DomainEvent> Events => events ??= new List<DomainEvent>();

        /// <summary>
        /// When the domain object is loaded from history, provides access to 
        /// all its past events.
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        [JsonIgnore]
        public IReadOnlyList<DomainEvent> History => history ??= new List<DomainEvent>();

        /// <summary>
        /// Whether the domain object was created in a readonly manner, meaning 
        /// that events cannot be produced from it.
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        [JsonIgnore]
        public bool IsReadOnly { get; protected set; } = true;

        /// <summary>
        /// Version of the domain object when it was originally loaded. Enables 
        /// optimistic concurrency checks.
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        [JsonIgnore]
        public int Version { get; internal set; }

        /// <summary>
        /// Accepts the pending events emitted by the domain object, and moves them to 
        /// the <see cref="History"/>, so that it looks as if it was freshly rehidrated 
        /// from it.
        /// </summary>
        public void AcceptEvents()
        {
            history ??= new List<DomainEvent>();
            history.AddRange(events ??= new List<DomainEvent>());
            events.Clear();
        }

        /// <summary>
        /// Registers a domain event handler.
        /// </summary>
        protected void Handles<T>(Action<T> handler) where T : DomainEvent => handlers.Add(typeof(T), e => handler((T)e));

        /// <summary>
        /// Raises and applies a new event of the specified type to the domain object.
        /// See <see cref="Raise{T}(T)"/>.
        /// </summary>
        protected void Raise<T>() where T : DomainEvent, new() => Raise(new T());

        /// <summary>
        /// Raises and applies an event to the domain object.
        /// The domain object should register handlers for relevant 
        /// domain events by calling <see cref="Handles{T}"/>.
        /// The handlers can perform the actual state changes to the 
        /// domain object.
        /// </summary>
        /// <remarks>
        /// This call also bounds a <see cref="DomainEvent"/> with its 
        /// source object by setting the set-once property <see cref="IEventMetadata.Subject"/> 
        /// on the event.
        /// </remarks>
        protected void Raise<T>(T e) where T : DomainEvent
        {
            if (IsReadOnly)
                throw new NotSupportedException();
            
            events ??= new List<DomainEvent>();

            // NOTE: we don't fail for generated events that don't have a handler 
            // because those just mean they are events important to the domain, but 
            // that don't cause state changes to the current domain object.
            if (handlers.TryGetValue(e.GetType(), out var handler))
                handler(e);

            ((IEventMetadata)e).Subject = Id;

            events.Add(e);
        }

        /// <summary>
        /// Loads the domain object by applying its historic events.
        /// </summary>
        /// <remarks>
        /// This method cannot be a constructor because derived classes need 
        /// to first set up their <see cref="Handles{T}(Action{T})"/> registrations 
        /// before we can apply the history.
        /// </remarks>
        protected void Load(IEnumerable<DomainEvent> history)
        {
            IsReadOnly = false;
            foreach (var e in history)
            {
                if (handlers.TryGetValue(e.GetType(), out var handler))
                    handler(e);

                this.history ??= new List<DomainEvent>();
                this.history.Add(e);
            }
        }
    }
}
