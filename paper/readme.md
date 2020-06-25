# What I've learned: MMORPG Server Tech
###### Jonathan Cohen

## Preamble
I have always wanted to make an MMORPG (Massively Multiplayer Online Role Playing Game), specifically because it is a lot of work. I had looked at a number of tools that others have created for developing and serving such games, but always felt that they all hid 99% of the real work behind "magic".  

Magically running scripts in the right place, magically connecting players to other servers, and such. I could never stomach using those kinds of tools, as I always want to fundamentally understand whatever tools I am using, to know how to use them properly at every point along the way. Apparently I am a perfectionist in this capacity, but it has lead me to attempt and re-attempt developing MMORPG server tech a few times over the years, and I would like to catalog some of what I have learned here. I have attempted building game server technology 3 times.  

Each attempt, I have used C#. Not just because it is a language I know, but it is used by the Unity3d Game Engine making interoperability with that engine easier, and is a very expressive language, and has much less cumbersome tooling than something like C/C++.


## First attempt

The first attempt was a pretty big failure. I had an architecture that was not organized properly for the task of multiplayer games, and also spun up multiple threads per connecting user. I knew that many games had thousands of connected players at once, and in a test where I was able to get maybe 30 players to connect at once, my server's memory usage skyrocketed. This was unfortunate. There were a number of other failures, and some successes.

I couldn't find the code for this, unfortunately. I am writing most of this from memory.

### Failures

#### Blob
I started writing the server without much of a concept as to how any of it should be structured, and put everything in one big "blob". This was a problem for obvious reasons, as the code in the server expanded, it became much harder to work.

#### Memory & threading
So I naively created multiple threads for each connecting user. One for sending data, one for receiving data (IIRC, and another for updating that user's data). This was not good, as each thread had some overhead (stack space).  
I didn't know much at the time of how to optimize/profile for memory usage. 
The other thing is at the time, I didn't know about the `struct`/`ValueType`s in C#, so I was `new`ing a bunch of things everywhere, causing lots of garbage collection.

#### Database
I was able to fairly easily "Write my own" database, but it was just simple files containing data for each user/entity/etc. Data was simple and supported nested properties, and was stored like this:  
`foo.data`
```
name:Foo
id:ddcf1641-ebfd-4ec3-bfeb-a9a0a277b815
pos.x:123
pos.y:456
pos.z:789
rot.x:0
rot.y:45
rot.z:0
equipment.left:ca146153-10ca-4231-bd27-8a3078a92680
equipment.right:3d5dbfac-5573-4f73-8631-9eaf8ed66ca8
...etc
```
It would but would have had serious performance issues at larger amounts of data, as it would have been bottlenecked hard by HDD speeds, as I would read/write the entire file at once. I consider this a failure, even though it never failed in testing, because I moved away from this self-made format for JSON as soon as I wrote my [XtoJSON](https://github.com/ninjapretzel/XtoJSON) library.

#### Maps (or lack-thereof)
I  failed to anticipate the requirement for players to be spread into different maps.  
This is kind of critical for a real MMORPG, players should only see things in the map they were in.  
The overall architecture was not well suited for this, and I would need to re-structure the server to add this feature.

#### Glue-code 
There was also a lot of very specific glue code needed to hook up Unity to talk to the server, for example to move objects smoothly when the server sent messages, I ended up needing to pipe the message into the main thread (most of unity's types are not accessible from off the main thread), and find some way of interpolating the positions, so that the players would not suddenly warp each time the server would 'tick'.

The amount of code needed to hook the client to respond to any server action was still quite high.  
This always seems to be a problem with "generic" server/client libraries, which is always how I tried to structure these projects.  
Other things this touched include: 
- Handling logging in on the client
- Initializing the player-controlled object (client side) once logged in
- Chat system
- Binding a "skin" of some sort to the player objects
- Movement + Interpolation of other player's movement
- Providing animation data to animate skinned objects
- Displaying various game data to players

There was way too much code involved in all of these things. Glue code is very resistant to my attempts to reduce it, and I have tried to do so in each iteration.

### Successes

#### RPCs and a RPC protocol
The underlying mechanisms for message passing (RPC) are essentially what I am currently using in my current attempt, just with an easier mechanism for providing encryption/encoding. Both the client and server transfer data over the same protocol, so it is entirely symmetric. 

The protocol is simple:
- Only ASCII/UTF8 text is transmitted.
- Messages may be transmitted one at a time, or in batches.
- Messages are broken up by non-ASCII characters 
	- 'Bell' (0x07) to separate parts within a message
	- 'Unit Separator' (0x1F) separates messages themselves. 
	- The specific characters used do not matter, so long as they have no meaning to the underlying transfer protocol. 
- The first segment(s) of the message describes what method to call
- The receiver is free to choose to ignore messages sent by the other party. 
- The rest of the message describes the parameters to the method.

With C#, I use reflection to look up the method requested, which must match a specific signature (so arbitrary code cannot easily be called by an attacker. If the method is found, it is called, if not, an error is logged and the request is ignored.  
The protocol is flexible enough to be implemented on top of any underlying transport (TCP/UDP, or even could be used with WebSockets). 

I have slightly adjusted the way I treat these messages (eg adding extra locator information into the messages, and time-stamping them both client/server sided), but the idea has not changed much from its initial conception, just layers of compression and encryption added, as well as mechanisms to pack binary data into base64/base85. 

## Second Attempt

I still have the code for this attempt, but it is in a private repository offsite, so I attached it as [`bakabaka-master.zip`](bakabaka-master.zip).

This code was intended to be put into a Unity3d project, within a `/Plugins` directory, alongside some of my other libraries such as the aforementioned [XtoJSON](https://github.com/ninjapretzel/XtoJSON).

Attempting to learn from my mistakes, I rewrote much of the code handling the database and interacting with unity's systems. 

### Failures

#### Embedding within Unity
The first attempt, I wrote all of my code to make a standalone server executable, which any game would be able to connect to. This second attempt, I married my library to the Unity engine. While that simplified a lot of things, it also made a lot of other things much harder.

Some small things like collision detection and having basic mathematical types was nice to "just have", as well as binary interoperability between these types across client/server meant it was easier to send/receive these types of data within RPCs.

Managing the lifetime of the server was one of those things, as I wanted to be able to use this for regular multiplayer as well as MMO-scale games, and with that, the server would need to be able to be easily created and destroyed, but this proved to be fairly difficult to get right, especially considering my next failure.

In order to be able to easily work on the code within the editor, the codebase needed to know if unity was stopping or recompiling, so it could terminate any server tasks, instead of letting them run indefinitely. This wouldn't be necessary in a standalone release, but did enlarge the code footprint for me to maintain while working.

#### `async` overhead
Async programming has taken off recently. However, there is considerable overhead any time async code is run. This turns out to be pretty large when you try to "multithread" a server using `async`, when it was not intended for such a job at all. The memory usage of this version of the server was even worse, needing constant garbage collection (every few seconds seemed to work the best), and had a serious "leak" that would eat up igs of RAM after the server was running for hours.

I can only assume this is due to the way that `async` works in C#, where every `async` method called generates a State-Machine object that is used to re-enter the `async` method and a `Task` that can be used to query its completion.
I pretty blindly just made everything an `async` method, so I may have made some subtle mistakes. Within C#, I would now avoid using `async` for anything that is intended to be long-running, unless it is within something that is fully `async`, like `deno.js`.

I found it is much easier to anticipate how your program will run when you take very tight, specific control over your threads of execution. Leaving anything up to a system that you don't understand will lead to subtle mistakes.

#### `Module`s
While I did try to organize code into groups, my approach was not the correct one. I mistakenly structured the server/client to require all modules passed to it on creation, and did not leave any room to add or remove modules as they were running. This was a critical oversight, as I found that I would often need to change the capabilities of the server at runtime (eg, change callbacks used to respond to RPC messages, or ignore certain RPC messages based on the current state of things).

The other thing is I did not properly separate out certain modules, as I did in my third attempt. For example, my `Entities` module has a `Maps` subsystem, which is complicated enough to be its own independent `Module`, and heavily depends on another `SyncData` module to handle `Entity` data.

I had started working on a system to allow me to load `Module`s dynamically, but scrapped that as I moved to my third and current attempt.

#### Hotloading/content repository
I had made an attempt to send game content from the server. The intent was that clients would request assets they did not have, and the server would send them the data for those assets. This didn't work for many reasons, unfortunately.

Unity doesn't make it easy to send assets over the network on demand, unless you use their solution, `AssetBundle`s, which is not really easy to work with in an automated way. Their solution requires hosting the bundles over HTTP, and I would prefer to avoid HTTP if possible. I was able to get some types of assets loaded dynamically using a hotloading system (and a separate project I can no longer find that was dedicated to it), but decided to scrap the repository system that I would have used to request/send files, as it was ballooning in size, without much visible progress every time I tried to finish it.

While I wanted to avoid HTTP as much as possible, it unfortunately seems like the best system for this kind of thing, however I would still want to have automated asset bundling if possible, so I could just modify a model/rig prefab and have a new version published, which clients would download the next time they need to load that model.


#### Data-driven design lead to me Re-inventing classes and virtual methods
I wanted to have a very, very data driven design. This basically lead to what's on the tin. 

`Entity`s had "pointers" to (names of) what method to call under various circumstances. I typically loaded `Entity`s from database files, which would be like their classes. I had `Entity`s that could change their methods on the fly, which is cool, but I basically re-invented something in a much, much more inefficient way.

#### Still too much glue-code.
Like it says on the tin. I was able to reduce the amount of glue-code that was present, but I still needed quite a bit of code to hook up things I thought of as fundamental, such as handling the local player, it still took a bit of code to hook up the player upon spawning, and I keep thinking there has to be an easier way to handle this.

For example, in my design, I would like players to be able to enter/exit/change vehicles, but each vehicle may have very different scripts on the client side. Each may have very different requirements, skins, and so on.


### Successes

#### `Module`s
Yes, they were a failure, but also a success.  
I was able to clearly see the kinds of methods that would need to be called within these `Module`s, and separate out a clear interface for them to communicate with the server, and each other.

For example, lots of things depended on login information, and by separating that system out into its own module. Other things that relied on that information could request the `LoginModule` from the server, and query login state for a user.

#### `SyncData`
I also had a `SyncData` module, which was used to implement a Publish/Subscribe model of data synchronization. This data was organized into a tree, and the server could "subscribe" a client to a subtree of data. It would do some basic dirty checking, and send only updated data to clients that had subscribed to that data. 

This was used by a lot of things, including everything that handled entity data.

Overall, this module was pretty successful, and I still have a similar module in my current attempt, though its use is now more limited.

#### `Map`s
One thing I did in the second attempt, but not in the first, was include a system for separating `Entity`s based on what `Map` they are in, as well as where they are in that `Map`. 

I used a very simple cell-grid system, where every `Entity` is contained at some coordinate within a fixed-sized grid. Clients are automatically subscribed to every `Entity` within some vision range around their cell. This vision range was implemented as either a square/cube, but could also be implemented as a radius.

When an `Entity` transitions from one cell to another, sets of cells are generated:
- The set of cells it is no longer visible to
- The set of cells it is now visible to
All clients in the first are unsubscribed, and all clients in the second are subscribed.  
If the `Entity` itself is bound to a client, it is unsubscribed from all of the first set of cells, and subscribed to all of the second set of cells (all cell relationships are reflexive).

This works both when the `Entity` simply "steps" across a cell boundary, or if it is teleported to a far away cell.

Additionally, each `Map` had its own `Update` cycle, though the actual movement of entities was an often-deferred `async` method.


## Third Attempt

This is my current attempt at building server technology, and I have applied many lessons I learned above.  
The code is [Here in this github repository.](https://github.com/ninjapretzel/ExServer)

My overall structure has shifted a bit from above. I can't really label anything specifically a success or failure yet, as I am still working on the tech, and hope everything below turns out to be a success.

### Threading + ConcurrentQueue + WorkPool
I created a tight threading system, which automatically allocates and removes threads based on demand. These are used to tightly control how many threads are used for various tasks.

Additionally, the core work (sending/receiving data from clients) was rewritten in such a way that any thread could work on any client, rather than having threads be bound to clients, or `async` functions spinning in the background, and C#'s builtin types for multithreaded queueing, `ConcurrentQueue` was used.

#### Reduced/stable cpu/memory usage
From the failures before, it was apparent how hard it is to properly structure a server in a garbage collected language. Great care has to be taken to not unnecessarily `new` too many things, and anything that may have unexpected overhead should be avoided.  

So far, I have been able to do just that, but it is a lot easier when the execution points of any code is well known, and not just tossed into some global thread pool like with `async`. 

I also thought that maybe it was all of the lambda captures (eg the `()=>{}` notation for functions that has become very popular across many languages recently) that I used everywhere, and the overuse of nearby variables, but I still use quite a few such state-capturing lambdas, without any of the overhead (so far).

### `Service`s instead of `Module`s
I did not just rename `Module`s, I fundamentally changed the way that they are hooked up and interop with each other and the `Server`.  
I renamed them `Service`, as that makes the most sense given how I decided to think about them:

- `Service`s are the fundamental building blocks of a `Server`, A `Server` is made up of of a number of `Service`s.
- `Service`s can query the server to see if other `Service`s exist, and send specific messages to other `Service`s.
- `Service`s can send arbitrary messages that **_any `Service`_** can listen for.
- `Service`s may be added or removed from a `Server` at any time.
	- Obviously, some core services will never be removed, but the potential is there!
- They are what contain the definitions for RPCs. (Like `Module`s did before.)

The message passing part of these `Service`s is a big win, as it makes it much easier for any `Service` to interop with other services.

#### Benefit #1- Message passing
I have a single generic method that is called on the `Server` to pass a message:  
`public void On<T>(T val)`  
This method records the message object into a `ConcurrentQueue` to be handled later in a single, server-wide update thread. This enforces a strict order- no two messages can ever be invoked at the same time, and also acts as a single point that any service can output messages on for other services to act on, _without knowing ahead of time what those services are_.  
This was a major success, as it heavily simplified a callback focused model into a fire-and-forget model.

#### Benefit #2- reduced "glue code"
It also let me cut down on the amount of "glue code" involved, as I can have services that will listen for messages that are added when needed (for example, on the login screen), and removed when not needed. All relevant code for some functionality becomes self-contained within a single `Service`. 

The pattern that I found most effective, Unity's existing APIs, is to  simply register the `Service` with the server using `Server.AddService<>()` in the `OnEnable()` method, and remove it using `Server.RemoveService<>()` in the `OnDisable()` method. This works well for single-use UI like the login screen.

I also found this pattern incredibly useful for attaching a `MonoBehaviour` to `GameObject`s which represent `Entity`s on the server, except in this case, there are many `ExEntityLink` behaviours that are attached on the Unity side, and only one `ExEntityLinkService` that is attached to the server and responding to messages.


### ECS - Entity-Component-Service
Essentially an ECS - Entity-Component-System, however, I have currently discarded the notion of "System"- there is no one "System" to manage any specific component type.

- There is a `Service` that handles `Entity`s.
- All `Entity`s are equivalent to the GUIDs that identify them within their `Service`.
	- `null` `Entity`s are equivalent to an Empty/Impossible GUID.
- `Entity`s may have `Comp`onents. (Shortened name to be distinct from `UnityEngine.Component`)
- `Comp`onents may be attached or removed at any time.
- `Comp`onent types must only contain "[Blittable](https://docs.microsoft.com/en-us/dotnet/framework/interop/blittable-and-non-blittable-types)" memory
	- (memory must be able to be directly copied to any system and retain the same meaning)
	- this basically just means no pointers/system specific hashes/identifiers of any kind.
- Any `Entity` may be used to look up attached `Comp`onents
- Any `Comp` may be used to look up its `Entity`
- `Entity`s and `Comp`onents may only interact with others within the same `Service`.

I used a not-well-known type called `ConditionalWeakTable<TKey, TValue>`, which stores weak-references to value objects, alongside a weak-reference to a key object. When the key "disappears", the weak-reference data is also inaccessible, and dropped upon the next garbage collection. These are thread safe, as far as I can tell, and work perfectly for storing arbitrary entity data attached to some entity reference.

I use a composite type: `ConcurrentDictionary<Type, ConditionalWeakTable<Entity, Comp>>`, to store all of the `Comp`onents, organized by type. If an entity has a `Comp`onent attached to it, it appears as a key in the inner dictionary, with its current component data as the value.

`Entity`/`Comp`onent data is transferred between server/client by the `EntityService` on a publish/subscribe model, however some `Comp`onents may be marked as "server only", or "owner only", and are respectively only kept on the server, or between the server and the client that owns the `Entity` it is attached to. Whenever `Comp`onent data is sent, the entire `Comp`onent is serialized and sent. This encourages `Comp`onents to be kept as small as possible to minimize network overhead.

The sending of data is still invoked manually (rather than attempting to do it automatically), to allow for control over when data is actually sent, due to the previous constraint. This way every field of a `Comp`onent can be set before the data is sent to all subscribers.

I have a bit of reflection that is done to automatically sync fields on components from client to server, without need for writing more "glue code" to synchronize the entity type. A lot of it is in a fairly hacky state, but works feasibly well. 

I consider it hacky, as I have caches for methods that can pack any possible type, when technically only `ValueType`s that do not contain references should be packed. I have no way to statically verify this at the moment (though I am sure there is a way to get the C# compiler to do so for me). In order to have certain functionality with these, I made various `ValueType` `struct`s which are able to be converted to/from reference types like `string` or `float[]` of various maximum sizes. This allows for fields within the `Comp`onents to be directly serialized, but they have to be declared as one of those types, eg `InteropString32`.

(In the cases of sending `string`s directly, plain `string`s can be used, the interop types I made are specifically for packing into `struct`s.)

The "System" part of a strict ECS system can be accomplished simply by registering additional `Service`s, which get the `EntityService` attached to the same `Server`, and do lookups for `Entity`s that contain a set of 1, 2, or 3 well known `Comp`onents, and operating on those sets of entities as needed.

### `Map`s again
`Map`s are handled by a `MapService`, which is used to locate, create, and destroy `Map`s.

I brought over all of the `Map` code from the previous attempt, and rewired it to work with `Entity`s and `Comp`onents. Clients can be subscribed to an `Entity` ID by the server (and they are always given an `Entity` with the same ID as their `Client` object). This will make the client always receive data for that entity when it is updated, however technically the subscription model is a part of the `EntityService`, which the `MapService` makes use of.

`Map`s no longer run on `async Task`s, but run on workers within a thread pool. The `MapService` Active `Map`s are thrown into a `ConcurrentQueue` and yanked out by a worker, which calculates the delta time for that map, and runs the update logic, and then puts it back into the queue. The `MapService` maintains a `WorkPool<Map>`, which manages worker threads that will update `Map`s, as well as the set of `Map`s that should actually be worked on, to allow for removals.

Movement, spawning, and despawning, and server-side `Entity` collision detection is handled within the worker thread, with each map being an isolated container. Currently, collision detection is un-implemented, but will use `Comp`onents on entities within the map for very simple collision detection.

I designed the `MapService` to allow for "sharding"/"channels"/"blocks", or multiple separate copies of the same map, with different sets of clients connected, but haven't actually tested it yet.

`Map`s are still broken into fixed-sized cells, and the visibility calculation is the same as the previous attempt, but I added a feature to add bounds to the map itself, which clamps the positions of any `Entity`s within the map, when they are moved.

### Using a real database

I had before tried to write my own database, and that was sufficient for simple things. As complexity grew, I would need to spend more time maintaining my own database, so I started using MongoDB as the database software. I did learn a lot by attempting to write my own database module.

I would prefer if I could statically link to MongoDB or something else and directly make queries, rather than having to have a separate application that mine connects to, but that is the standard model for most database software. MongoDB's official C# driver, for me, leads a lot to be desired. I wrote a small wrapper around it to alleviate most of my desires. 

The wrapper `DBService` consists of mostly generic methods that select the specific "Collection" (Table) to load from, based on the type requested. I then have types that are used as names for the tables, for example `LoginAttempt`, `UserAccountCreation`, and `UserLoginInfo`, as well as describing the data contained within. 

I only have one constraint for most types saved/loaded to the database, which is a base type of `DBEntry`. This standardizes the mongo-generated `id` field, and allows the inclusion of a separate `Guid`, as most of the server code is based off of using the `Guid` type, and it is not easily interchangeable with the MongoDB `ObjectId` type. They also have variations that allow for the search field to be specified, as well as the search criteria as a string (rather than an ID).

The final result is being able to write something like `db.Get<UserLoginInfo>("name", username);` to build a query that searches for a user named with the contents of the `username` variable. 

Saving documents works with just `db.Save(userLoginInfo)`, as the generic `Save<T>(T item)` method can infer the type of the parameter, and save it to the correct table, and overwrite the existing entry if found by id (if not, creating a new entry).

All of the wrapper methods and all of their variants build the internal MongoDB queries using the driver, and make it easier to write C# to work with the database.

After having that wrapper set up, the next thing I did was build a little seed system to load JSON files into the database, so I could maintain a well-known state for the database as I develop, and include it as json data in source control. It consists of a `seed.json` file that describes what files/directories to load, and into what databases/collections. The only place that sidesteps this convention is the loading of the `Comp`onents of `Entity`s. `Entity`s are loaded from a `EntityInfo`, which is a `DBEntry`.  which contains data for the type of entity, the filename it was loaded from, and any attached `Comp`onents in an array. `Map`s then have their data specified in a `MapInfo` which is also a normal `DBEntry`, and has a list of `EntityInstanceInfo`s, which contain the information of where to spawn the entity initially. 

Finally, since I expect a lot of my server-side gameplay code to work with my `Vector2`, `Vector3`, `Rect`, (other mathematical types) `JsonObject` and `JsonArray` types, I wrote serializers and deserializers for all of them. Most of the mathematical types are serialized into arrays, which are a more sensible solution than using names to refer to each field. `JsonObject` and `JsonArray` are recursively serialized as the corresponding MongoDB types, `BsonDocument` and `BsonArray`.


### Other nifty stuff


#### `__makeref` differences across `.net` and `Mono`.
While I was working on the netcode, I wanted a way to easily serialize data. Typically, I just wanted to cast  arbitrary `struct`s into `byte[]`s. I created something based on this [Fun With `__makeRef` blogpost](http://benbowen.blog/post/fun_with_makeref/). I discovered that there were even more low-level gotchas- it turns out that Mono Project's runtime, even on Windows, has a different order in it's internal `TypedReference` `struct`. This makes the entirely wrong data get used when accessing parts of that internal struct - the type segment on one platform is the pointer segment on the other.

Code that makes use of specific offsets within `TypedReference` can't rely on the runtime always being one or the other, and for maximum portability, should have some way of investigating if it is in a `Mono` based runtime or a `.net` based runtime (simply checking the platform is not sufficient, as for example, Unity3d's editor uses the Mono runtime.

This code can be found [in this file](ExServer/Core/Utils/Unsafe.cs) around line 469. That method, and some similar methods above. Thankfully it is fairly easy to test if the Mono Runtime is being used, by using reflection and checking for a type being present: simply `Mono.Runtime`.

I mostly use the `ToBytes`/`FromBytes` methods in my netcode, indirectly through the [`Pack`/`Unpack`](ExServer/Core/Utils/Packing.cs) static classes, which are used whenever binary data needs to be provided to an RPC.


#### `UDP` and `TCP` simultaneously.
I started writing my netcode on top of TCP. This seems to work fairly well when I am testing things on my own, however TCP is a congestion controlled protocol, and as such can introduce delays when packets are received out of order. 

I started to add support for multiple simultaneous connections on a single `Client`, which UDP connections could be used to accelerate certain messages. Currently, a connection between client/server still starts with a TCP connection, and a UDP connection is attempted upon connecting. If a handshake occurs, then the UDP side of things is enabled, otherwise all messaging is done through TCP.

Currently that is the state of the code, and I have been playing with ideas of how to avoid any excess retransmission, while still making use of the UDP connection for acceleration. To pave the way for this system, to my `RPCMessage` class, I have added the transmission mode (UDP/TCP), and (sender) timestamp to all messages, as well as a receiver timestamp upon reception. The transmission protocol information can be used to prioritize TCP messages, and the timestamps can be used to check if a message is outdated (in the case that it is UDP.)

To make use of the above, I have been thinking about how the server may further need to be restructured to have some form of transmission policies for various messages, for example, a policy when sending "Move" commands (either to/from server) could be `if UDP is available, every 5th one should be sent over TCP, while all the rest are sent over UDP, unless the movement is important (eg a "rubberband")`.