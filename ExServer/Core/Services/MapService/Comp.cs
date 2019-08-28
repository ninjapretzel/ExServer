#if UNITY_2017 || UNITY_2018 || UNITY_2019 || UNITY_2020
#define UNITY
#endif

#if !UNITY
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
#else
using UnityEngine;
#endif

using System;
using System.Runtime.CompilerServices;

namespace Ex {
	/// <summary> Empty base class for components. Simply stores some data for entities. </summary>
	/// <remarks> Instances of these are stored in <see cref="ConditionalWeakTable{TKey, TValue}"/>s, and references to them should not be held onto long term. </remarks>
	public abstract class Comp {

		/// <summary> GUID of bound entity, if bound. </summary>
		private Guid? _entityId;

		/// <summary> Service of bound entity, if bound. </summary>
		private EntityService service;

		/// <summary> Is this component on a master server? </summary>
		public bool isMaster { get { return service.server.isMaster; } }

		/// <summary> Send component data to all subscribers. </summary>
		public void Send() {
			service.SendComponent(this);
		}

		/// <summary> Dynamic lookup of attached entity. </summary>
		private Entity entity {
			get {
				if (!_entityId.HasValue || service == null) {
					throw new InvalidOperationException($"Component of type {GetType()} has already been removed, and is invalid. Please don't persist references to Entity or Component");
				}
				return service[_entityId.Value];
			}
		}

		/// <summary> Called when a component is removed to discard references. </summary>
		internal void Invalidate() {
			_entityId = null;
			service = null;
		}
		/// <summary> Binds this component to an entity. </summary>
		/// <param name="entity"> Entity to bind to </param>
		internal void Bind(Entity entity) {
			if (_entityId.HasValue || service != null) {
				throw new InvalidOperationException($"Component of {GetType()} is already bound to {_entityId.Value}.");
			}
			_entityId = entity;
			service = entity.service;
		}

		/// <summary> ID of entity </summary>
		public Guid entityId { get { return _entityId.HasValue ? _entityId.Value : Guid.Empty; } }

		/// <summary> If this component is bound to an entity, associates another component with that entity. </summary>
		/// <typeparam name="T"> Generic type of Component to add </typeparam>
		/// <returns> Component of type T added to Entity </returns>
		public T AddComponent<T>() where T : Comp { return entity.AddComponent<T>(); }
		/// <summary> If this component is bound to an entity, gets another component associated with that entity. </summary>
		/// <typeparam name="T"> Generic type of Component to get  </typeparam>
		/// <returns> Component of type T on the same Entity, or null. </returns>
		public T GetComponent<T>() where T : Comp { return entity.GetComponent<T>(); }
		/// <summary> If this component is bound to an entity, removes another component associated with that entity. </summary>
		/// <typeparam name="T"> Generic type of Component to remove </typeparam>
		/// <returns> True if a component was removed, otherwise false. </returns>
		public bool RemoveComponent<T>() where T : Comp { return entity.RemoveComponent<T>(); }

		#if !UNITY 
		public void LoadFromDB(BsonDocument bdoc) {

		}


		#endif

	}

}
