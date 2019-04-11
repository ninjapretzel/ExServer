# Blag
##### aka Blog (Confused? Go read XKCD at https://xkcd.com/)

## ==>
### 2019/04/07 thru 2019/04/10
Ported a bunch of math/vector code from unity into a single file to have parity with Unity's Mathf/Vector and other structs. Not much to say about that, lots of repetitive code to handle simple stuff. Ready to start actually building the entity/map system now, I think.

## ==>
### 2019/04/06 and 2019/04/07
Lots of cleanup and ported the `SyncData` code took a bit of similar reworking as to how it is organized, and I added the ability to spin up a thread tied to the server's lifetime for custom separate update logic, as to not peg the global server tick with potentially heavy update logic. `SyncData` is inteded to be similar to how MeteorJS works interally, pushing updates to clients, and maintaining a lightweight database on the client that can be easily queried and used to fill displays.

It is separate from any database, and this time it is not intended to hold actual game information. It is really just intended to be used for pushing data updates to clients from the server in a structured way.
It will be used for slower updates to clients, not close to real-time, useful for things like inventory content, currency statistics, guild/party information, global events or gamestate.

Been thinking about map/entity code for a few days, while making minor cleanups that I haven't committed, and how to implement it so it does not use `SyncData`, as it does in the version I am porting, which is unfortunate.
SyncData turned out to be too slow for creating an entity system for large numbers of temporary entities, so I need to create something that is much more optimized.


## ==>
### 2019/04/04
After a bit of rumination, and mostly taking a day mostly off to think, I figured out a good way of dealing with MongoDB, and handling user logins. Suprisingly, it did not take much to massage MongoDB code into doing what I want, and I think I have a really good "DRY" pattern started, using type information in generic methods to automatically persist/retrieve objects to database.

The next thing is the login code, I send hashes currently, but eventually I will have a simple encryption layer on top of the transmission protocol, which would make it less terrible to just send them raw. 

There was a private server for RuneScape which I played, that would create a user if someone tried logging in with a new username. This worked really well in letting new players start quickly, or allowing people to roll new characters at a whim. It flies in the face of most of what most people/companies would do, forcing players to register on a website before logging in (to collect information on them to sell for $$$$), but there are ways of making it inconvinient to register multiple accounts quickly and difficult to exploit.

## ==>
### 2019/04/02
I already see a problem with providing daily updates. It is hard to find something genuine to talk about, and I do not like just excreting words. Lots is going on in my head, but callouts need time to stew. Many useful and useless thoughts run through my head, and not many of them are actually worth capturing and writing down. This one did.

## It Begins.
### 2019/04/01

This is the most convinient way for me to organize my thoughts and track my progress.
This is a project I have been wanting to make properly for some time, technology for a small-scale MMO server, which I could use to make a small online game.

I know it is possible for one person to accomplish a lot. It takes discipline to organize things properly, and some forethought to organize them to not leave behind pitfalls to fall in later. I have had a few rounds of creating this kind of project before, but due to time constraints I had and having to cut corners, things did not work as intended.

This time, I intend to build things correctly from the start.

I mashed together some code from a few projects, and cleaned up some of the loose ends for the Initial Commit.

This contained the Core module (which contains the code intended to be shared between client and server), and the winforms harness for the server.

I'm starting with just a set of logs and some places to expand, but eventually that console will be the main place to interface with the server from.

I then brought over some of my netcode from another project, cut out the `async` stuff, and worked the loops that handle processing client information so they could be multithreaded properly. Async stuff is okay. But it is merely okay, one-size-fits all, does not fit that great. When you need performance, and want to be able to reason about the effects on memory and processing, async is no good. Nothing beats a properly engineered (profiled/optimized) tight loop, just like nothing beats clothing that is properly tailored to your personal requirements.

The main messaging loop works, making today a good bunch of progress towards getting a game up.
I hope to have a bit of progress every day for this month, with the goal of having an actual demo by the end of the month.

Currently, the Unity hooks to this code are fairly simple: the content of "Core" / the "Ex" namespace exists in the unity project, and a `Ex.Client` is created with a `System.Net.TcpClient` representing the connection to the server. That client gets a `Ex.Server` created behind the scenes, which acts as a 'slave' server to the remote 'master' server. It is configured slightly (hooks for logging are provided and some color codes are changed)

I will likely also post the source for the Unity project, which I am using to build the game client, once it is in a somewhat completed spot. SVN is just better suited for Unity than git in general, so that is the source control that I am using. Once it is at a good spot, I will just bulk upload the whole project as a git repository. 

