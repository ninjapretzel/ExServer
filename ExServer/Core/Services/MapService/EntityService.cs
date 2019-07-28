#if UNITY_2017 || UNITY_2018 || UNITY_2019 || UNITY_2020
#define UNITY
#endif

using Ex.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
#if !UNITY
using MongoDB.Bson.Serialization.Attributes;
#else
using UnityEngine;
#endif

namespace Ex {
	/// <summary> Service which manages the creation and tracking of entities. 
	/// Entities are automatically created for connecting clients and removed for disconnecting ones.  </summary>
	public class EntityService : Service {

#if !UNITY
		[BsonIgnoreExtraElements]
		public class UserEntityInfo : DBEntry {

			public string map { get; set; }
			public Vector3 position { get; set; }
			public Vector4 rotation { get; set; }

		}
		
		public void InitializeEntityInfo(Guid userID) {
			
			Log.Info($"Initializing EntityInfo for {userID}");
			UserEntityInfo info = new UserEntityInfo();
			info.position = Vector3.zero;
			info.rotation = new Vector4(0, 0, 0, 1);
			info.map = "Limbo";
			info.guid = userID;
			
			GetService<DBService>().Save(info);
			Log.Info($"Saved EntityInfo for {userID}");

			var check = GetService<DBService>().Get<UserEntityInfo>(userID);
			Log.Info($"Retrieved saved info? {check}");

			
		}
#endif
		/// <summary> Current set of entities. </summary>
		private ConcurrentDictionary<Guid, Entity> entities;
		/// <summary> Components for entities </summary>
		public ConcurrentDictionary<Type, ConditionalWeakTable<Entity, Comp>> components;
		
		/// <summary> Gets an entity by ID, or null if none exist. </summary>
		/// <param name="id"> ID of entity to try and get </param>
		/// <returns> Entity for ID, or null if no entity exists for that ID </returns>
		public Entity this[Guid id] {
			get { 
				Entity e;
				if (entities.TryGetValue(id, out e)) { return e; }
				return null;
			}
		}
		
		/// <summary> Creates a new entity, and returns the reference to it. </summary>
		/// <returns> Reference to the newly created entity </returns>
		public Entity CreateEntity() {
			Entity entity = new Entity(this);
			entities[entity.guid] = entity;
			return entity;
		}

		/// <summary> Revokes an entity by ID </summary>
		/// <param name="guid"> ID of entity to revoke </param>
		/// <returns> True if Entity existed prior and was removed, false otherwise. </returns>
		public bool Revoke(Guid guid) {
			if (entities.ContainsKey(guid)) {
				Entity __;
				entities.TryRemove(guid, out __);
				return true;
			}
			return false;
		}

		public override void OnEnable() {
			entities = new ConcurrentDictionary<Guid, Entity>();
			components = new ConcurrentDictionary<Type, ConditionalWeakTable<Entity, Comp>>();
#if !UNITY
			GetService<LoginService>().initializer += InitializeEntityInfo;
#endif
		}
		public override void OnDisable() {
			entities = null;
			components = null;
#if !UNITY
			GetService<LoginService>().initializer -= InitializeEntityInfo;
#endif

		}

		public override void OnConnected(Client client) {
			if (isMaster) {
				Entity entity = new Entity(this, client.id);
				entities[client.id] = entity;
				Log.Info($"OnConnected for {client.id}, entity created");
			}
		}

#if !UNITY
		public override void OnDisconnected(Client client) {
			Entity entity;
			entity = entities[client.id];
			TRS trs = GetComponent<TRS>(entity);
			OnMap onMap = GetComponent<OnMap>(entity);

			var db = GetService<DBService>();
			var loginService = GetService<LoginService>();

			LoginService.Session? session = loginService.GetLogin(client);
			Credentials creds;
			if (session.HasValue) {
				creds = session.Value.credentials;
				Log.Verbose($"Getting entity for client {client.identity}, id={creds.userId}/{creds.username}");

				var info = db.Get<UserEntityInfo>(creds.userId);

				if (info == null) {
					info = new UserEntityInfo();
					if (trs != null) {
						info.position = trs.position;
						info.rotation = trs.position;
					}
					if (onMap != null) {
						info.map = onMap.mapId;
					}
				}
				
				db.Save(info);

			} else {
				
				Log.Verbose($"No login session for {client.identity}, skipping saving entity data.");
			}

			entities.TryRemove(client.id, out entity);
		}


		/// <summary> Called when a login occurs. </summary>
		/// <param name="succ"></param>
		public void On(LoginService.LoginSuccess_Server succ) {
			if (!isMaster) { return; }

			Client client = succ.client;
			Guid clientId = client.id;
			Log.Info($"{nameof(EntityService)}: Got LoginSuccess for {succ.client.identity} !");
			var user = GetService<LoginService>().GetLogin(client);
			Guid userId = user.HasValue ? user.Value.credentials.userId : Guid.Empty;
			string username = user.HasValue ? user.Value.credentials.username : "[NoUser]";

			var db = GetService<DBService>();
			var info = db.Get<UserEntityInfo>(userId);
			var trs = AddComponent<TRS>(clientId);
			Log.Info($"user {clientId} -> { username } / {userId }, {info}, {trs}");

			if (info != null) {

				trs.position = info.position;
				trs.rotation = info.rotation;
				trs.scale = Vector3.one;
				GetService<MapService>().EnterMap(client, info.map);
			
			} else {


				
			}

		}
#endif

		/// <summary> Gets the ConditionalWeakTable for a given entity type. </summary>
		/// <typeparam name="T"> Generic type of table to get </typeparam>
		/// <returns> ConditionalWeakTable mapping entities to Components of type T </returns>
		private ConditionalWeakTable<Entity, Comp> GetTable<T>() {
			Type type = typeof(T);
			var table = !components.ContainsKey(type) 
				? (components[type] = new ConditionalWeakTable<Entity, Comp>()) 
				: components[type];
			return table;
		}

		/// <summary> Adds a component of type T for the given entity. </summary>
		/// <typeparam name="T"> Generic type of Component to add </typeparam>
		/// <param name="entity"> Entity to add Component to </param>
		/// <returns> Newly created component </returns>
		public T AddComponent<T>(Entity entity) where T : Comp {
			if (entity == null) { return null; }
			var table = GetTable<T>();
			
			Comp check;
			if (table.TryGetValue(entity, out check)) { 
				throw new InvalidOperationException($"Entity {entity.guid} already has a component of type {typeof(T)}!"); 
			}

			T component = Activator.CreateInstance<T>();
			component.Bind(entity);
			table.Add(entity, component);

			return component;
		}
		/// <summary> Adds a component of type T for the given entity. </summary>
		/// <typeparam name="T"> Generic type of Component to add </typeparam>
		/// <param name="id"> ID of Entity to add Component to </param>
		/// <returns> Newly created component </returns>
		public T AddComponent<T>(Guid id) where T : Comp { return AddComponent<T>(this[id]); }

		/// <summary> Gets a component for the given entity </summary>
		/// <typeparam name="T"> Generic type of Component to get </typeparam>
		/// <param name="entity"> Entity to get Componment from </param>
		/// <returns> Component of type T if it exists on entity, otherwise null. </returns>
		public T GetComponent<T>(Entity entity) where T : Comp {
			if (entity == null) { return null; }
			var table = GetTable<T>();

			Comp c;
			if (table.TryGetValue(entity, out c)) { return (T) c; }

			return null;
		}
		/// <summary> Gets a component for the given entity </summary>
		/// <typeparam name="T"> Generic type of Component to get </typeparam>
		/// <param name="id"> ID of Entity to get Componment from </param>
		/// <returns> Component of type T if it exists on entity, otherwise null. </returns>
		public T GetComponent<T>(Guid id) where T : Comp { return GetComponent<T>(this[id]); }

		/// <summary> Removes a component from the given entity  </summary>
		/// <typeparam name="T"> Generic type of Component to remove </typeparam>
		/// <param name="entity"> Entity to remove component from </param>
		/// <returns> True if component existed prior  and was removed, false otherwise. </returns>
		public bool RemoveComponent<T>(Entity entity) where T : Comp {
			if (entity == null) { return false; }
			var table = GetTable<T>();

			Comp c;
			if (table.TryGetValue(entity, out c)) { c.Invalidate(); }

			return table.Remove(entity);
		}
		/// <summary> Removes a component from the given entity  </summary>
		/// <typeparam name="T"> Generic type of Component to remove </typeparam>
		/// <param name="id"> ID of Entity to remove component from </param>
		/// <returns> True if component existed prior  and was removed, false otherwise. </returns>
		public bool RemoveComponent<T>(Guid id) where T : Comp { return RemoveComponent<T>(this[id]); }

		/// <summary> Get a list of all entities that have the given component. </summary>
		/// <typeparam name="T"> Component type to search for </typeparam>
		/// <param name="lim"> List of Guids to check </param>
		/// <returns> A list of Entities for the given guids that exist and have the given component associated with them </returns>
		public List<Entity> GetEntities<T>(IEnumerable<Guid> lim = null) where T : Comp {
			if (lim == null) { lim = entities.Keys; }
			List<Entity> ents = new List<Entity>();
			
			var table = GetTable<Entity>();
			Comp it;
			foreach (var guid in lim) {
				Entity e = this[guid];
				if (e != null && table.TryGetValue(e, out it)) {
					ents.Add(e);
				}
			}

			return ents;
		}

		/// <summary> Get a list of all entities that have the given components. </summary>
		/// <typeparam name="T1"> Component type to search for </typeparam>
		/// <typeparam name="T2"> Component type to search for </typeparam>
		/// <param name="lim"> List of Guids to check </param>
		/// <returns> A list of Entities for the given guids that exist and have the given components associated with them </returns>
		public List<Entity> GetEntities<T1, T2>(IEnumerable<Guid> lim = null) where T1 : Comp where T2 : Comp {
			if (lim == null) { lim = entities.Keys; }
			List<Entity> ents = new List<Entity>();

			var table1 = GetTable<T1>();
			var table2 = GetTable<T2>();
			Comp it1, it2;
			foreach (var guid in lim) {
				Entity e = this[guid];
				if (e != null
					&& table1.TryGetValue(e, out it1) 
					&& table2.TryGetValue(e, out it2)) {
					ents.Add(e);
				}
			}

			return ents;
		}

		/// <summary> Get a list of all entities that have the given components. </summary>
		/// <typeparam name="T1"> Component type to search for </typeparam>
		/// <typeparam name="T2"> Component type to search for </typeparam>
		/// <typeparam name="T3"> Component type to search for </typeparam>
		/// <param name="lim"> List of Guids to check </param>
		/// <returns> A list of Entities for the given guids that exist and have the given components associated with them </returns>
		public List<Entity> GetEntities<T1, T2, T3>(IEnumerable<Guid> lim = null) where T1 : Comp where T2 : Comp where T3 : Comp {
			if (lim == null) { lim = entities.Keys; }
			List<Entity> ents = new List<Entity>();

			var table1 = GetTable<T1>();
			var table2 = GetTable<T2>();
			var table3 = GetTable<T3>();
			Comp it1, it2, it3;
			foreach (var guid in lim) {
				Entity e = this[guid];
				if (e != null 
					&& table1.TryGetValue(e, out it1)
					&& table2.TryGetValue(e, out it2)
					&& table3.TryGetValue(e, out it3)) {
					ents.Add(e);
				}
			}

			return ents;
		}

	}

	/// <summary> An Entity is just a name/id, used to look up Components that are attached </summary>
	public class Entity {
		/// <summary> ID of this entity </summary>
		public Guid guid { get; private set; }
		/// <summary> EntityService this entity belongs to </summary>
		public EntityService service { get; private set; }
		/// <summary> Constructor for creating a new Entity identity </summary>
		/// <remarks> Internal, not intended to be used outside of EntityService. </remarks>
		internal Entity(EntityService service) { 
			this.service = service;
			guid = Guid.NewGuid(); 
		}

		/// <summary> Constructor for wrapping an existing ID with an Entity </summary>
		/// <remarks> Internal, not intended to be used outside of EntityService. </remarks>
		internal Entity(EntityService service, Guid id) {
			this.service = service;
			guid = id;
		}
		
		/// <summary> Adds a component to this entity. </summary>
		/// <typeparam name="T"> Generic type of component to add </typeparam>
		/// <returns> Component of type T that was added </returns>
		public T AddComponent<T>() where T : Comp { return service.AddComponent<T>(this); }
		/// <summary> Gets another component associated with this entity </summary>
		/// <typeparam name="T"> Generic type of component to get </typeparam>
		/// <returns> Component of type T that is on this entity, or null if none exists </returns>
		public T GetComponent<T>() where T : Comp { return service.GetComponent<T>(this); }
		/// <summary> Removes a component associated with this entity. </summary>
		/// <typeparam name="T"> Generic type of component to remove </typeparam>
		/// <returns> True if a component was removed, otherwise false. </returns>
		public bool RemoveComponent<T>() where T : Comp { return service.RemoveComponent<T>(this); }

		/// <summary> Coercion from Entity to Guid, since they are the same information. </summary>
		public static implicit operator Guid(Entity e) { return e.guid; }
	}

	/// <summary> Empty base class for components. Simply stores some data for entities. </summary>
	public abstract class Comp {

		/// <summary> GUID of bound entity, if bound. </summary>
		private Guid? _entityId;

		/// <summary> Service of bound entity, if bound. </summary>
		private EntityService service;

		/// <summary> Is this component on a master server? </summary>
		public bool isMaster { get { return service.server.isMaster; } }

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
		public void Bind(Entity entity) {
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

	}


	/// <summary> Base class for systems. </summary>
	public abstract class Sys { 
		/// <summary> Connected EntityService </summary>
		public EntityService service { get; private set; }

		/// <summary> Binds this system to an EntityService </summary>
		/// <param name="service"> Service to bind to </param>
		/// <param
		public void Bind(EntityService service, Type[] types, Delegate callback) {
			if (service != null) {
				throw new InvalidOperationException($"System of {GetType()} has already been bound.");
			}
			this.service = service;
		}
		
	}


	/// <summary> Component that places an entity on a map. </summary>
	public class OnMap : Comp {
		public string mapId;
	}

	/// <summary> Component that gives entity physical location </summary>
	public class TRS : Comp {
		public Vector3 position;
		public Vector4 rotation;
		public Vector3 scale;
	}
	public class Moveable : Comp {
		public Vector3 velocity;
		public Vector3 angVelocity;
	}

	public class Sphere : Comp {
		public float radius;
	}




}
