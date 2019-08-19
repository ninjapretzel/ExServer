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
		/// <summary> Subscriptions, server only. </summary>
		public ConcurrentDictionary<Client, ConcurrentSet<Guid>> subscriptions;
		/// <summary> Subscribers, server only. </summary>
		public ConcurrentDictionary<Guid, ConcurrentSet<Client>> subscribers;
		/// <summary> Types of components </summary>
		private ConcurrentDictionary<string, Type> componentTypes;

		/// <summary> Local entity ID used for the client to ask for movement. </summary>
		public Guid? localGuid = null;


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
			if (isMaster) {
				Entity entity = new Entity(this);
				entities[entity.guid] = entity;
				subscribers[entity.guid] = new ConcurrentSet<Client>();
				return entity;
			}
			return null;
		}

		/// <summary> Revokes an entity by ID </summary>
		/// <param name="guid"> ID of entity to revoke </param>
		/// <returns> True if Entity existed prior and was removed, false otherwise. </returns>
		public bool Revoke(Guid guid) {
			if (entities.ContainsKey(guid)) {
				{ Entity _; entities.TryRemove(guid, out _); }
				ConcurrentSet<Client> subs;

				
				if (isMaster && subscribers.TryRemove(guid, out subs)) {
					foreach (var sub in subs) {
						subscriptions[sub].Remove(guid);
					}
				}
				
				return true;
			}
			return false;
		}

		public override void OnEnable() {
			entities = new ConcurrentDictionary<Guid, Entity>();
			components = new ConcurrentDictionary<Type, ConditionalWeakTable<Entity, Comp>>();
			componentTypes = new ConcurrentDictionary<string, Type>();

			if (isMaster) {
				subscriptions = new ConcurrentDictionary<Client, ConcurrentSet<Guid>>();
				subscribers = new ConcurrentDictionary<Guid, ConcurrentSet<Client>>();
#if !UNITY
				GetService<LoginService>().initializer += InitializeEntityInfo;
#endif
			}
		}
		public override void OnDisable() {
			entities = null;
			components = null;
			componentTypes = null;
			if (isMaster) {
#if !UNITY
				var login = GetService<LoginService>();
				if (login != null) {
					login.initializer -= InitializeEntityInfo;
				}
				/// Should have already sent disconnect messages to connected clients 
				subscriptions.Clear();
				subscribers.Clear();
#endif
			}

		}

		/// <summary> Server -> Client RPC. Requests that the client spawn a new entity </summary>
		/// <param name="msg"> RPC Info. </param>
		public void SpawnEntity(RPCMessage msg) {
			/// noop on server
			if (isMaster) { return; }

			Guid id;
			if (Guid.TryParse(msg[0], out id)) {
				entities[id] = new Entity(this);
			}

			bool islocalEntity = msg.numArgs > 1 && msg[1] == "local";
			if (islocalEntity) {
				localGuid = id;
			}
		}

		/// <summary> Server -> Client RPC. Requests that the client despawn an existing entity. </summary>
		/// <param name="msg"></param>
		public void DespawnEntity(RPCMessage msg) {
			/// noop on server
			if (isMaster) { return; }

			Guid id;
			if (Guid.TryParse(msg[0], out id)) { Revoke(id); }
		}


		private Type GetCompType(string name) {
			if (componentTypes.ContainsKey(name)) { return componentTypes[name]; }
			Type t = Type.GetType(name);
			if (t != null) {
				if (typeof(Comp).IsAssignableFrom(t)) {
					componentTypes[name] = t;
					return t;
				}

				Log.Warning($"Type {name} does not inherit from {typeof(Comp)}.");
				componentTypes[name] = null;

			}
			Log.Warning($"No Type {name} could be found");
			componentTypes[name] = null;
			return null;
		}

		/// <summary> Server -> Client RPC. Requests the client add a component to an entity </summary>
		/// <param name="info"> RPC Info </param>
		public void AddComp(RPCMessage msg) {
			/// noop on server
			if (isMaster) { return; }

			Guid id;
			if (Guid.TryParse(msg[0], out id)) {
				string typeName = msg[1];
				Type type = GetCompType(typeName);
				AddComponent(id, type);
			}
		}

		/// <summary> Server -> Client RPC. Requests the client set information into a component. </summary>
		/// <param name="msg"></param>
		public void SetComponentInfo(RPCMessage msg) {
			/// noop on server
			if (isMaster) { return; }
			/// I really don't know if this is the right kind of approach 
			/// Idea would be to shoot name/value pairs over the RPC info
			/// and reflect the info into the entities
			Guid id;
			if (Guid.TryParse(msg[0], out id)) {
				string typeName = msg[1];
				Type type = GetCompType(typeName);
				Comp component = GetComponent(id, type);
				if (component != null) {
					for (int i = 2; i < msg.numArgs; i += 2) {
						string field = msg[i];
						string data = msg[i+1];
					}
				}

			}
		}

		public void Subscribe(Client client, Guid id) {
			
			var subsA = subscribers[id];
			var subsB = subscriptions[client];
			
			if (!subsA.Contains(client) && !subsB.Contains(id)) {
				subsA.Add(client);
				subsB.Add(id);
				if (id == client.id) {
					client.Call(SpawnEntity, id, "local");
				} else { 
					client.Call(SpawnEntity, id);
				}
			}


		}

		public override void OnConnected(Client client) {
			if (isMaster) {
				Entity entity = new Entity(this, client.id);
				entities[client.id] = entity;
				Log.Info($"OnConnected for {client.id}, entity created");

				client.Call(SpawnEntity, client.id);
			}
		}

#if !UNITY
		public override void OnDisconnected(Client client) {
			if (isMaster) {
				Entity entity = entities.ContainsKey(client.id) ? entities[client.id] : null;

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
				GetService<MapService>().EnterMap(client, info.map, info.position, info.rotation);
			
			} else {


				
			}

		}
#endif

		

		/// <summary> Gets the ConditionalWeakTable for a given entity type. </summary>
		/// <typeparam name="T"> Generic type of table to get </typeparam>
		/// <returns> ConditionalWeakTable mapping entities to Components of type T </returns>
		private ConditionalWeakTable<Entity, Comp> GetTable<T>() {
			Type type = typeof(T);
			return GetTable(type);
		}

		/// <summary> Gets the ConditionalWeakTable for a given entity type. </summary>
		/// <param name="type"> Type of table to get </typeparam>
		/// <returns> ConditionalWeakTable mapping entities to Components of type T </returns>
		private ConditionalWeakTable<Entity, Comp> GetTable(Type type) {
			return !components.ContainsKey(type)
							? (components[type] = new ConditionalWeakTable<Entity, Comp>())
							: components[type];
		}

		/// <summary> Adds a component of type T for the given entity. </summary>
		/// <typeparam name="T"> Generic type of Component to add </typeparam>
		/// <param name="id"> ID of Entity to add Component to </param>
		/// <returns> Newly created component </returns>
		public T AddComponent<T>(Guid id) where T : Comp { return (T) AddComponent(this[id], typeof(T)); }

		/// <summary> Adds a component of type T for the given entity. </summary>
		/// <param name="t"> Type of Component to add </typeparam>
		/// <param name="id"> ID of Entity to add Component to </param>
		/// <returns> Newly created component </returns>
		public Comp AddComponent(Guid id, Type t) {
			Entity entity = this[id];
			if (!typeof(Comp).IsAssignableFrom(t)) { throw new Exception($"{t} is not a valid ECS Component type."); }
			var table = GetTable(t);

			Comp check;
			if (table.TryGetValue(entity, out check)) {
				throw new InvalidOperationException($"Entity {entity.guid} already has a component of type {t}!");
			}

			Comp component = (Comp)Activator.CreateInstance(t);
			component.Bind(entity);
			table.Add(entity, component);

			return component;
		}

		/// <summary> Gets a component for the given entity </summary>
		/// <typeparam name="T"> Generic type of Component to get </typeparam>
		/// <param name="id"> ID of Entity to get Componment from </param>
		/// <returns> Component of type T if it exists on entity, otherwise null. </returns>
		public T GetComponent<T>(Guid id) where T : Comp { return (T)GetComponent(this[id], typeof(T)); }

		/// <summary> Gets a component for the given entity </summary>
		/// <param name="t"> Type of Component to get </typeparam>
		/// <param name="entity"> Entity to get Componment from </param>
		/// <returns> Component of type T if it exists on entity, otherwise null. </returns>
		public Comp GetComponent(Guid id, Type t) {
			Entity entity = this[id];
			if (entity == null) { return null; }
			var table = GetTable(t);

			Comp c;
			if (table.TryGetValue(entity, out c)) { return (Comp) c; }

			return null;
		}
		
		/// <summary> Checks the entity for a given component type, and if it exists, returns it, otherwise adds one and returns it. </summary>
		/// <typeparam name="T"> Generic type of Component to add </typeparam>
		/// <param name="entity"> Entity to check and/or add Component to </param>
		/// <returns> Previously existing or newly created component </returns>
		public T RequireComponent<T>(Guid id) where T : Comp { return (T) RequireComponent(id, typeof(T)); }

		/// <summary> Checks the entity for a given component type, and if it exists, returns it, otherwise adds one and returns it. </summary>
		/// <param name="t"> Type of Component to add </typeparam>
		/// <param name="id"> ID of Entity to check and/or add Component to </param>
		/// <returns> Previously existing or newly created component </returns>
		public Comp RequireComponent(Guid id, Type t) {
			Entity entity = this[id];
			if (entity == null) { return null; }
			var c = GetComponent(id, t);
			if (c != null) { return c; }
			return AddComponent(id, t); 
		}

		/// <summary> Removes a component from the given entity  </summary>
		/// <typeparam name="T"> Generic type of Component to remove </typeparam>
		/// <param name="id"> ID of Entity to remove component from </param>
		/// <returns> True if component existed prior  and was removed, false otherwise. </returns>
		public bool RemoveComponent<T>(Guid id) where T : Comp { return RemoveComponent(id, typeof(T)); }

		/// <summary> Removes a component from the given entity  </summary>
		/// <typeparam name="T"> Generic type of Component to remove </typeparam>
		/// <param name="entity"> Entity to remove component from </param>
		/// <returns> True if component existed prior  and was removed, false otherwise. </returns>
		public bool RemoveComponent(Guid id, Type t) {
			Entity entity = this[id];
			if (entity == null) { return false; }
			var table = GetTable(t);

			Comp c;
			if (table.TryGetValue(entity, out c)) { c.Invalidate(); }

			return table.Remove(entity);
		}

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
		/// <summary> Checks for a component associated with this entity, returns it or creates a new one if it does not exist </summary>
		/// <typeparam name="T"> Generic type of component to get </typeparam>
		/// <returns> Component of type T that is on this entity, or was just added </returns>
		public T RequireComponent<T>() where T : Comp { return service.RequireComponent<T>(this); }
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
		public int? mapInstanceIndex;
	}

	/// <summary> Component that gives entity a physical location </summary>
	public class TRS : Comp {
		/// <summary> Location of entity </summary>
		public Vector3 position;
		/// <summary> Rotation of entity (euler angles) </summary>
		public Vector3 rotation;
		/// <summary> Scale of entity's display </summary>
		public Vector3 scale;
	}

	/// <summary> Component that moves an entity's TRS every tick. </summary>
	public class Mover : Comp {
		/// <summary> Delta position per second </summary>
		public Vector3 velocity;
		/// <summary> Delta rotation per second </summary>
		public Vector3 angVelocity;
	}

	/// <summary> Component that gives entity a simple radius-based collision </summary>
	public class Sphere : Comp {
		/// <summary> Radius of entity </summary>
		public float radius;
		/// <summary> Is this sphere a trigger client side? </summary>
		public bool isTrigger;
		/// <summary> Layer for client side collision to be on? </summary>
		public int layer;
	}

	/// <summary> Component that gives entity a box-based collision </summary>
	public class Box : Comp {
		/// <summary> Axis Aligned Bounding Box </summary>
		public Bounds bounds;
		/// <summary> Is this sphere a trigger client side? </summary>
		public bool isTrigger;
		/// <summary> Layer for client side collision to be on? </summary>
		public int layer;
	}

	/// <summary> Component that gives one entity control over another. </summary>
	public class Owned : Comp {
		/// <summary> ID of owner of this entity, who is also allowed to send commands to this entity. </summary>
		public Guid owner;
	}




}
