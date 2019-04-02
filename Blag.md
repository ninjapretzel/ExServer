# Blag
##### aka Blog

## It Begins
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

