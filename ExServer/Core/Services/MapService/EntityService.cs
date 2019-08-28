#if UNITY_2017 || UNITY_2018 || UNITY_2019 || UNITY_2020
#define UNITY
#endif

#if !UNITY
using MongoDB.Bson.Serialization.Attributes;
#else
using UnityEngine;
#endif
using Ex.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Reflection;

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
		
		/// <summary> EntityInfo (spawn source) data cached from database by type </summary>
		public ConcurrentDictionary<string, EntityInfo> entityInfos;
		public EntityInfo GetEntityInfo(string typeName) {
			if (entityInfos.ContainsKey(typeName)) { return entityInfos[typeName]; }
			return (entityInfos[typeName] = GetService<DBService>().Get<EntityInfo>("Content", "type", typeName));
		}
#endif

		/// <summary> Current set of entities. </summary>
		private ConcurrentDictionary<Guid, Entity> entities;
		/// <summary> Components for entities </summary>
		public ConcurrentDictionary<Type, ConditionalWeakTable<Entity, Comp>> componentTables;
		/// <summary> Subscriptions, server only. </summary>
		public ConcurrentDictionary<Client, ConcurrentSet<Guid>> subscriptions;
		/// <summary> Subscribers, server only. </summary>
		public ConcurrentDictionary<Guid, ConcurrentSet<Client>> subscribers;

		/// <summary> Holds pre-processed type information for a given component type. </summary>
		private class TypeInfo {
			/// <summary> Type of cached information </summary>
			public Type type;
			/// <summary> Names of fields that are sync'd </summary>
			public FieldInfo[] syncedFields;
			/// <summary> Cached setter functions </summary>
			public Delegate[] syncedFieldSetters;
			/// <summary> Cached getter functions </summary>
			public Delegate[] syncedFieldGetters;
		}
		/// <summary> Types of components </summary>
		private ConcurrentDictionary<string, TypeInfo> componentTypes;

		
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
		public Entity CreateEntity(Guid? id = null) {
			Entity entity = (id == null) ? new Entity(this) : new Entity(this, id.Value);
			entities[entity.guid] = entity;

			if (isMaster) {
				subscribers[entity.guid] = new ConcurrentSet<Client>();
			}

			return entity;
		}

		/// <summary> Revokes an entity by ID </summary>
		/// <param name="guid"> ID of entity to revoke </param>
		/// <returns> True if Entity existed prior and was removed, false otherwise. </returns>
		public bool Revoke(Guid guid) {
			if (entities.ContainsKey(guid)) {
				Log.Debug($"Master:{isMaster}, revoking entity {guid}");
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
			componentTables = new ConcurrentDictionary<Type, ConditionalWeakTable<Entity, Comp>>();
			componentTypes = new ConcurrentDictionary<string, TypeInfo>();
			
			if (isMaster) {
				subscriptions = new ConcurrentDictionary<Client, ConcurrentSet<Guid>>();
				subscribers = new ConcurrentDictionary<Guid, ConcurrentSet<Client>>();
#if !UNITY
				entityInfos = new ConcurrentDictionary<string, EntityInfo>();
				GetService<LoginService>().initializer += InitializeEntityInfo;
#endif
			}
		}
		public override void OnDisable() {
			entities = null;
			componentTables = null;
			componentTypes = null;
			if (isMaster) {
#if !UNITY
				entityInfos = null;
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
				CreateEntity(id);
				bool islocalEntity = msg.numArgs > 1 && msg[1] == "local";
				Log.Debug($"slave.SpawnEntity: Spawned entity {id}. local? {islocalEntity} ");
				if (islocalEntity) {
					localGuid = id;
				}
			} else {
				Log.Debug($"slave.SpawnEntity: No properly formed guid to spawn.");
			}

		}

		/// <summary> Server -> Client RPC. Requests that the client despawn an existing entity. </summary>
		/// <param name="msg"></param>
		public void DespawnEntity(RPCMessage msg) {
			/// noop on server
			if (isMaster) { return; }

			for (int i = 0; i < msg.numArgs; i++) {
				Guid id;
				if (Guid.TryParse(msg[i], out id)) { 
					Revoke(id); 
					Log.Debug($"slave.DespawnEntity: Despawning entity {id}");
				}
			}
				
		}

		/// <summary> Loads a ECS Component type by name. Prepares getters and setters for any value-type data fields </summary>
		/// <param name="name"> Name of type to load </param>
		/// <returns> Type of given ECS component by name, if valid and found. Null otherwise. </returns>
		public Type GetCompType(string name) {
			if (componentTypes.ContainsKey(name)) { return componentTypes[name].type; }
			Type t = Type.GetType(name);
			if (t != null) {
				if (typeof(Comp).IsAssignableFrom(t)) {
					LoadCompType(name, t);
					return t;
				}

				Log.Warning($"Type {name} does not inherit from {typeof(Comp)}.");
				componentTypes[name] = null;

			}
			Log.Warning($"No valid Type {name} could be found");
			componentTypes[name] = null;
			return null;
		}

		/// <summary> Loads required information for interacting with a component of type T. </summary>
		/// <param name="name"> FullName of type </param>
		/// <param name="t"> Type </param>
		private void LoadCompType(string name, Type t) {
			Log.Debug($"EntityService.LoadCompType: isMaster?{isMaster} Loading component {t}");
			TypeInfo info = new TypeInfo();
			info.type = t;
			List<FieldInfo> syncedFields = new List<FieldInfo>();
			List<Delegate> syncedFieldGetters = new List<Delegate>();
			List<Delegate> syncedFieldSetters = new List<Delegate>();
			FieldInfo[] fields = t.GetFields(BindingFlags.Public | BindingFlags.Instance);
			foreach (var field in fields) {
				if (field.FieldType.IsValueType) {
					syncedFields.Add(field);

					Func<object, dynamic> getDel = (obj) => { 
						try {
							return field.GetValue(obj); 
						} catch (Exception e) { Log.Warning(e, $"Failed to Get field {t}.{field.Name}"); }
						return null;
					};
					Action<object, dynamic> setDel = (obj, val) => { 
						try {
							field.SetValue(obj, val); 
						} catch (Exception e) { Log.Warning(e, $"Failed to Set field {t}.{field.Name} to a value of type {val.GetType()} "); }
					};
					syncedFieldGetters.Add(getDel);
					syncedFieldSetters.Add(setDel);
					
				}
			}
			info.syncedFields = syncedFields.ToArray();
			info.syncedFieldGetters = syncedFieldGetters.ToArray();
			info.syncedFieldSetters = syncedFieldSetters.ToArray();

			componentTypes[name] = info;
		}

		/// <summary> Server -> Client RPC. Requests the client add components to an entity </summary>
		/// <param name="info"> RPC Info </param>
		public void AddComps(RPCMessage msg) {
			/// noop on server
			if (isMaster) { return; }

			Guid id;
			if (Guid.TryParse(msg[0], out id)) {
				Log.Debug($"slave.AddComps: Adding {msg.numArgs-1} Comps to entity {id}");
				for (int i = 1; i < msg.numArgs; i++) {
					string typeName = msg[i];
					Type type = GetCompType(typeName);
					AddComponent(id, type);
				}
			}
		}

		/// <summary> Server -> Client RPC. Requests the client removes components from an entity </summary>
		/// <param name="info"> RPC Info </param>
		public void RemoveComps(RPCMessage msg) {
			/// noop on server
			if (isMaster) { return; }

			Guid id;
			if (Guid.TryParse(msg[0], out id)) {
				Log.Debug($"slave.AddComps: Removing {msg.numArgs - 1} Comps to entity {id}");

				for (int i = 1; i < msg.numArgs; i++) {
					string typeName = msg[i];
					Type type = GetCompType(typeName);
					RemoveComponent(id, type);
				}
			}
		}

		/// @BAD @HACKY @IMPROVEME - Baking generic args for packers/unpackers. 
		/// There has gotta be a more efficient way to bind generic types to unknown function calls.
		/// Maybe bake lambdas instead? I know those can leak captures....
		private static MethodInfo PACKER;
		private static MethodInfo UNPACKER;
		private static ConcurrentDictionary<Type, MethodInfo> GENERIC_PACKERS;
		private static ConcurrentDictionary<Type, MethodInfo> GENERIC_UNPACKERS;
		public static MethodInfo GET_PACKER(Type t) {
			if (!t.IsValueType) { return null; }
			if (PACKER == null) { INITIALIZEPACKERS(); }
			if (!GENERIC_PACKERS.ContainsKey(t)) { GENERIC_PACKERS[t] = PACKER.MakeGenericMethod(t); }
			return GENERIC_PACKERS[t];
		}
		public static MethodInfo GET_UNPACKER(Type t) {
			if (!t.IsValueType) { return null; }
			if (PACKER == null) { INITIALIZEPACKERS(); }
			if (!GENERIC_UNPACKERS.ContainsKey(t)) { GENERIC_UNPACKERS[t] = UNPACKER.MakeGenericMethod(t); }
			return GENERIC_UNPACKERS[t];
		}
		private static void INITIALIZEPACKERS() {
			PACKER = typeof(Pack).GetMethod("Base64", BindingFlags.Static | BindingFlags.Public);
			UNPACKER = typeof(Unpack).GetMethod("Base64", BindingFlags.Static | BindingFlags.Public);
			GENERIC_PACKERS = new ConcurrentDictionary<Type, MethodInfo>();
			GENERIC_UNPACKERS = new ConcurrentDictionary<Type, MethodInfo>();
		}

		/// <summary> Server -> Client RPC. Requests the client set information into a component. </summary>
		/// <param name="msg"></param>
		public void SetComponentInfo(RPCMessage msg) {
			/// noop on server
			if (isMaster) { return; }
			Guid id;
			if (Guid.TryParse(msg[0], out id)) {
				string typeName = msg[1];
				Type type = GetCompType(typeName);
				TypeInfo info = componentTypes[typeName];

				Log.Debug($"slave.SetComponentInfo: Setting info for {id}.{typeName}, {msg.numArgs-2} fields.");
				Comp component = GetComponent(id, type);
				TypedReference cref = __makeref(component);
				
				if (component != null) {
					Log.Debug($"slave.SetComponentInfo:\nBefore: {component}");
					object[] unpackerArgs = new object[1];
					for (int i = 0; i+2 < msg.numArgs && i < info.syncedFields.Length; i++) {
						FieldInfo field = info.syncedFields[i];
						unpackerArgs[0] = msg[i + 2];
						try {
							
							field.SetValue(component, GET_UNPACKER(field.FieldType).Invoke(null, unpackerArgs));
							//This doesn't work inside of unity because mono.
							//field.SetValueDirect(cref, GET_UNPACKER(field.FieldType).Invoke(null, unpackerArgs));
						} catch (Exception e) {
							Log.Warning(e, $"Failed to unpack and set {field.FieldType} {type}.{field.Name}");
						}
					}
					Log.Debug($"slave.SetComponentInfo:\nAfter: {component}");
				} else {

					Log.Debug($"slave.SetComponentInfo: No COMPONENT {type} FOUND on {id}! ");
				}

			}
		}

		public void Subscribe(Client client, Guid id) {
			if (isMaster) {
				Entity entity = this[id];
				if (entity == null) { 
					Log.Debug($"Master: no entity for {id} to subscribe {client.identity} to");
					return; 
				}

				var subsA = subscribers[id];
				var subsB = subscriptions[client];
			
				if (!subsA.Contains(client) && !subsB.Contains(id)) {
					Log.Debug($"Master: Subscribing {client.identity} to {id}");
					subsA.Add(client);
					subsB.Add(id);
					if (id == client.id) {
						client.Call(SpawnEntity, id, "local");
					} else { 
						client.Call(SpawnEntity, id);
					}

					// Todo: Clean this up and separate into deltas once it works 
					Comp[] components = GetComponents(id);
					List<object> addArgs = new List<object>();
					addArgs.Add(id);
					foreach (var component in components) { addArgs.Add(component.GetType().FullName); }
					client.Call(AddComps, addArgs.ToArray());
					
					List<object[]> argLists = PackSetComponentArgs(id, components);
					foreach (var args in argLists) {
						client.Call(SetComponentInfo, args);
					}
					
				} else {
					Log.Debug($"Master: {client.id} already subscribed to {id}");
				}
			}
		}

		/// <summary> Removes an entity ID from a client's subscription list, if they were subscribed. </summary>
		/// <param name="client"> Client to unsubscribe </param>
		/// <param name="id"> ID to unsub from </param>
		public void Unsubscribe(Client client, Guid id) {
			if (isMaster) {
				Entity entity = this[id];
				if (entity == null) {
					Log.Debug($"Master: no entity for {id} to unsubscribe {client.identity} from");
					return;
				}
				if (id == client.id) {
					Log.Debug($"Master: Cannot unsub client from its own entity");
					return;
				}


				var subsA = subscribers[id];
				var subsB = subscriptions[client]; 

				if (subsA.Contains(client) && subsB.Contains(id)) {
					Log.Debug($"Master: Unsubbing {client.id} from {id}");
					subsA.Remove(client);
					subsB.Remove(id);

					client.Call(DespawnEntity, id);
				}
			}
		}

		private List<object[]> PackSetComponentArgs(Guid id) {
			var components = GetComponents(id);
			return PackSetComponentArgs(id, components);
		}

		private object[] PackSetComponentArgs(Guid id, Comp component) {
			List<object> args = new List<object>();
			args.Add(id);
			string typeName = component.GetType().FullName;
			args.Add(typeName);
			var type = GetCompType(typeName);
			var typeInfo = componentTypes[typeName];

			for (int i = 0; i < typeInfo.syncedFields.Length; i++) {
				dynamic value = typeInfo.syncedFieldGetters[i].DynamicInvoke(component);
				args.Add(Pack.Base64(value));
			}

			return args.ToArray();
		}

		private List<object[]> PackSetComponentArgs(Guid id, Comp[] components) {
			List<object[]> argLists = new List<object[]>();
			foreach (var component in components) {
				argLists.Add(PackSetComponentArgs(id, component));
			}

			return argLists;
		}

		public override void OnConnected(Client client) {
			if (isMaster) {
				Entity entity = CreateEntity(client.id);
				subscriptions[client] = new ConcurrentSet<Guid>();

				Log.Info($"OnConnected for {client.id}, entity created");

				Subscribe(client, client.id);
			} else {
				Log.Info($"Clientside OnConnected.");
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

				Revoke(client.id);
				{ ConcurrentSet<Guid> _; subscriptions.TryRemove(client, out _); }
			} else {

				Log.Info($"Clientside OnDisconnected.");
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
			Log.Info($"OnLoginSuccess_Server for user {clientId} -> { username } / UserID={userId }, EntityInfo={info}, TRS={trs}");

			if (info != null) {

				trs.position = info.position;
				trs.rotation = info.rotation;
				trs.scale = Vector3.one;

				trs.Send();

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
			return !componentTables.ContainsKey(type)
							? (componentTables[type] = new ConditionalWeakTable<Entity, Comp>())
							: componentTables[type];
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
			if (isMaster) {
				var subs = subscribers[id];
				string addMsg = Client.FormatCall(AddComps, id, t.FullName);
				foreach (var client in subs) { client.SendMessageDirectly(addMsg); }

				/// Actually don't need to send data yet, components will be empty right when added.
				//var args = PackSetComponentArgs(id, component);
				//string setMsg = Client.FormatCall(SetComponentInfo, args);
				//foreach (var client in subs) { client.SendMessageDirectly(setMsg); }
			}

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

		/// <summary> Gets all associated components with an entity by a given id </summary>
		/// <param name="id"> ID of entity to check </param>
		/// <returns> Array of Components associated with the given Entity id </returns>
		/// <remarks> as with <see cref="Comp"/>, do NOT hold onto references to the array. </remarks>
		public Comp[] GetComponents(Guid id) {
			Entity e = this[id];
			if (e == null) { return new Comp[0]; }

			List<Comp> components = new List<Comp>();
			foreach (var pair in componentTables) {
				Type type = pair.Key;
				var table = pair.Value;
				Comp comp;
				if (table.TryGetValue(e, out comp)) { components.Add(comp); }
			}
			return components.ToArray();
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
			if (isMaster) {
				var subs = subscribers[id];
				foreach (var client in subs) { client.Call(RemoveComps, id, t.FullName); }
			}

			return table.Remove(entity);
		}

		/// <summary> Sends the information for a component to all subscribers of that component's entity </summary>
		/// <param name="comp"> Component to send </param>
		public void SendComponent(Comp comp) {
			Guid id = comp.entityId;
			var subs = subscribers[id];
			var args = PackSetComponentArgs(id, comp);
			foreach (var sub in subs) {
				sub.Call(SetComponentInfo, args);
			}
		}

		/// <summary> Get a snapshot list of all entities that have the given component. </summary>
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
	




}
