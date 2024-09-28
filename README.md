# Unity Meetgard

This package contains a brand-new TCP setup for networked games.

# Install

This package is not available in any UPM server. You must install it in your project like this:

1. In Unity, with your project open, open the Package Manager.
2. Either refer this Github project: https://github.com/AlephVault/unity-meetgard.git or clone it locally and refer it from disk.
3. Also, the following packages are dependencies you need to install accordingly (in the same way and also ensuring all the recursive dependencies are satisfied):

     - https://github.com/AlephVault/unity-support.git
     - https://github.com/AlephVault/unity-layout.git
     - https://github.com/AlephVault/unity-menuactions.git
     - https://github.com/AlephVault/unity-boilerplates.git
     - https://github.com/AlephVault/unity-binary.git

# Usage

The first thing users need to understand is that this package leverages the power of TCP connections as provided by the standard NET packages (`TCPClient`, `TCPServer`), which requires developing _three_ parts, actually:

- A common protocol definition.
- A server application, which serves as a single source of truth and perhaps has access to external storage systems.
     - _Host_-mode applications are also allowed, instead of dedicated servers. Hosts are no special connections, so far (future versions might have this, however), but standard TCP connections in the local machine. Thus, these applications act as both server and client of themselves. Typical LAN games have a _host_ mode (e.g. Counter-Strike, Age of Empires, ...).
- A client application. Client applications are applications that send commands to the server and get their response and/or asynchronous notifications of what's going on the server in the sense that may be relevant to that client in particular.

Sending the messages is done through a special serialization mechanism provided by the [unity-binary](https://github.com/AlephVault/unity-binary.git) (which is closely -extremely- based on the supported serialization in the MLAPI library).

## Project Structure and Bootstrap

While users can create directories as they please and also setup the protocol classes as they please, this package comes from default actions that generate code (code generation is provided by the [unity-boilerplates](https://github.com/AlephVault/unity-boilerplates.git) package), which also store the code in certain directories that might exist, might be created or might _need to be created_, thus needing actions from the user.

In order to stick to the convention defined in this package, there's a new menu option enabled just by having this package installed in your project:

```
Assets/Create/Aleph Vault/Meetgard/Boilerplates/Project Startup
```

Once you click that menu option, your project will gain (and will retain, if they exist) the following directories:

```
Assets/Scripts: The usual Scripts directory.
Assets/Scripts/Client: Code related to the Client-side app.
Assets/Scripts/Client/Authoring: Code related to Authoring / Inspector-settable things.
Assets/Scripts/Client/Authoring/Behaviours: Client-side behaviours.
Assets/Scripts/Client/Authoring/Behaviours/Protocols: Client-side protocol implementations. They will be explained in this document, later.
Assets/Scripts/Client/Authoring/Behaviours/UI: Client-side UI component behaviours.
Assets/Scripts/Client/Authoring/Types: Other Authoring-related types for the client side app.
Assets/Scripts/Client/Types: Other support types for the Client-side app.
Assets/Scripts/Server: Code related to the Server-side app.
Assets/Scripts/Server/Auhoring: Code related to Authoring / Inspector-settable things.
Assets/Scripts/Server/Authoring/Behaviours: Server-side behaviours.
Assets/Scripts/Server/Authoring/Behaviours/Protocols: Server-side protocol implementations. They will be explained in this document, later.
Assets/Scripts/Server/Authoring/Behaviours/External: Server-side behaviours related to external access (e.g. objects dealing with external services).
Assets/Scripts/Server/Authoring/Types: Other Authoring-related types for the server side app.
Assets/Scripts/Server/Types: Other support types for the Server-side app.
Assets/Scripts/Protocols: Protocol definitions / schemas.
Assets/Scripts/Protocols/Messages: Type definitions for the messages being defined / used in the protocols' methods/commands.
```

These directories are important since many dependent packages will make use of them properly.

## Creating a Client/Server project

Creating a client/server project involves several steps but it starts with:

- Starting the Client app, typically on a scene.
- Starting the Server app, typically on _another_ scene.

If the game was instead a _host_-based game, only one scene (with both client and server functionality) would be needed instead.

### The Server object

One object needs to be added _to the server scene_, i.e. into the object hierarchy in the scene. The steps to have this object ready is:

1. Open your Server scene, or create it if you don't have one and then open it.
2. If you don't have a server object, create one new Game Object in your scene. Name it "Server" just for convenience (this is optional but recommended to not get lost later), and proceed:
     1. Add a `NetworkServer` behaviour class to the just-created object.
     2. You can keep the properties by default (they will be explained later, all of them), but you can also set `Dont Destroy` to `true`. This will ensure the object will remain when scenes are changed / unloaded.
     3. Add **in this order** the following behaviours: `ZeroProtocolServerSide` and `PingProtocolServerSide`.
3. Save your scene changes.

### The Client object

The next step is to do a _reflected_ work in the _client_ scene, i.e. into the object hierarchy in the scene. The steps to have this object ready is:

1. Open your Client scene, or create it if you don't have one and then open it.
2. If you don't have a client object, create one new Game Object in your scene. Name it "Client" just for convenience (this is optional but recommended to not get lost later), and proceed:
     1. Add a `NetworkClient` behaviour class to the just-created object.
     2. You can keep the properties by default (they will be explained later, all of them), but you can also set `Dont Destroy` to `true`. This will ensure the object will remain when scenes are changed / unloaded.
     3. Add **in this order** the following behaviours: `ZeroProtocolClientSide` and `PingProtocolClientSide`.
3. Save your scene changes.

## The protocols

In order to exchange data / communicate & notify between the client and server (in either direction) a _protocol_ needs to be defined.

Protocols are a set of three structures that are defined to exchange messages. Two of them were already named in the previous sections:

- The `Zero` protocol is explained in three parts: The client side and server side (which was installed to both endpoint objects in the previous sections) and the definition (which is a special object that relates both sides).
- The `Ping` protocol is optional, actually, but interacts with each connection ensuring it responds to certain commands to test it's alive.

Then, more protocols can be created (and typically _will_ be created) to satisfy application-specific needs of communication between the client and server endpoint objects.

There's however a rule of thumb that must be preserved when adding new protocols in the client and server endpoint objects: **the protocol implementations must be in the same order in the client and the server**. This might be fixed in a future version but it's a current limitation that must be matched as of today.

In order to create a new protocol, one can be created by two different ways:

1. Manually. This one is not recommended since it's too slow in comparison to the "assisted" method, but perhaps the user is not comfortable with the by-convention project structure (which is used by the automatic method). In such case, users must learn how to declare the three involved classes.
2. Assisted. This method involves using a menu action that comes out of the box. This action opens a window that asks for some data to generate the protocol-related classes in the by-convention project structure directories.

If, for some reason, more than one protocol is needed, users can create all the protocols they need by choosing one of those methods respectively for each of the protocols.

### Protocol creator assistant

In order to quickly create a set of protocol classes, there's a new menu option enabled just by having this package installed in your project:

```
Assets/Create/Aleph Vault/Meetgard/Boilerplates/Create Protocol
```

When clicking this menu option, a window will open and prompt the user to name the protocol. The user must choose a valid `PascalCaseName` for the protocol and, on confirm (let's say the user chose `Foo` as the protocol name), these classes will be created like this:

1. In the `Assets/Scripts/Protocols`, a class named `FooProtocolDefinition`.
2. In the `Assets/Scripts/Client/Authoring/Behaviours/Protocols`, a class named `FooProtocolClientSide`, derived from `ProtocolClientSide<FooProtocolDefinition>`.
3. In the `Assets/Scripts/Server/Authoring/Behaviours/Protocols`, a class named `FooProtocolServerSide`, derived from `ProtocolServerSide<FooProtocolDefinition>`.

The structure of these files will be detailed in the next section (the "manual" method), although the generated files will be slightly different (e.g. the class names will follow that convention and they will be already located in a namespace).

### Manually creating a protocol

The structure of the classes will be described later, but some hints on how the classes are structured are given here:

1. The first thing is to define the `ProtocolDefinition` subclass.
2. Then, the `ProtocolClientSide` subclass, related to that `ProtocolDefinition` subclass.
3. Finally, the `ProtocolServerSide` subclass, related also to that `ProtocolDefinition` subclass.

#### The ProtocolDefinition subclass

The subclass must be defined like this:

```
using AlephVault.Unity.Meetgard.Protocols;

// Assume that, in this example, no namespace will be used
// for this class.
public class Bar : ProtocolDefinition
{
    protected override void DefineMessages()  
    {
        // Messages will be defined here.
        //
        // Client messages will be "sent"
        // by the client and "handled" by
        // the server.
        //
        // Server messages will be "sent"
        // by the server and "handled" by
        // the client.
    }
}
```

More details will be added later.

#### The ProtocolServerSide subclass

The subclass must be defined like this:

```
using System;  
using System.Threading.Tasks;  
using UnityEngine;  
using AlephVault.Unity.Meetgard.Authoring.Behaviours.Server;  
using Protocols;

// Assume that, in this example, no namespace will be used
// for this class, just like `Bar`.
public class BarProtocolServerSide : ProtocolServerSide<Bar>  
{
    protected override void Setup()  
    {
        // Use this method, instead of Awake,
        // to initialize the component.
    }

    protected override void Initialize()  
    {
        // Use this method, instead of Start,
        // to initialize the message senders.
    }

    protected override void SetIncomingMessageHandlers()  
    {
        // Use this method to define message
        // handlers (from client-sent messages).
    }

    public override async Task OnServerStarted()  
    {
        // Handle what happens when the server
        // has just started listening.
    }

    public override async Task OnConnected(ulong clientId)  
    {
        // Handle what happens when a client
        // has just connected.
    }

    public override async Task OnDisconnected(ulong clientId, Exception reason)  
    {
        // Handle what happens when a client
        // has just disconnected. It may be
        // due to an exception: in that case,
        // the reason will not be null.
    }

    public override async Task OnServerStopped(Exception e)  
    {
        // Handle what happens when the server
        // has just stopped.
    }
}
```

Editing this class, in particular for `Initialize` and `SetIncomingMessageHandlers`, will be detailed later.

#### The ProtocolClientSide subclass

Similar to the server side class, the client side will have a sender/handler structure like this:

```
using System;  
using System.Threading.Tasks;  
using UnityEngine;  
using AlephVault.Unity.Meetgard.Authoring.Behaviours.Client;  
using Protocols;

public class BarProtocolClientSide : ProtocolClientSide<Bar>  
{
    protected override void Setup()  
    {
        // Use this method, instead of Awake,
        // to initialize the component.
    }

    protected override void Initialize()  
    {
        // Use this method, instead of Start,
        // to initialize the message senders.
	}

    protected override void SetIncomingMessageHandlers()  
    {
        // Use this method to define message
        // handlers (from server-sent messages).
    }

    public override async Task OnConnected()  
    {
        // Handle what happens when the client
        // has just connected.
    }

    public override async Task OnDisconnected(Exception reason)  
    {
        // Handle what happens when the client
        // has just disconnected. It may be
        // due to an exception: in that case,
        // the reason will not be null.
    }
}
```

Editing this class, in particular for `Initialize` and `SetIncomingMessageHandlers`, will be detailed later.

## Protocol messages

A protocol consists of messages. Those messages flow either:

1. From client to server.
2. From server to client.

Also, a message can be defined to either no carry any data (the sole sending of the message on itself is enough) or to carry some typed data (which become parameters of the message and the data will be properly processed on arrival).
### Messages sent from client to server

Typically, these are user commands or some sort of heartbeat checks. In order to define a new server message, the first thing is to define it in the protocol definition. However, _if the intended new message involves sending parameters / a data payload_, then it's first needed to define the datatype itself.

Defining a message datatype consists of defining an `ISerializable` class from the `unity-binary` package. It will look like this:

```
using AlephVault.Unity.Binary;

class MyClientPayload : ISerializable
{
    ... fields here ...
    
	public void Serialize(Serializer serializer)  
	{
	    // Refer to unity-binary's documentation
	    // to have a complete understanding on
	    // serialization.
	}
}
```

Then, it's just a matter of knowing the `MLAPI` / `unity-binary` serialization strategy to understand how the data is marshaled there. See the `unity-binary` docs for more details. There are also a lot of structures which out of the box come in the package to serialize simple values (e.g. scalar values or Unity types), in case you don't need a `struct` or `class` to be defined for your data.

Then, in the protocol definition, the message would be defined like this:

```
...

public class Bar : ProtocolDefinition
{
    protected override void DefineMessages()  
    {
        // This message sends no parameters.
        // Give it a meaningful name. It will
        // be later referred by that name.
        DefineClientMessage("MyMessageName");

        // This message sends parameters.
        // Give it a meaningful name. It will
        // be later referred by that name.
        DefineClientMessage<MyClientPayload>("MyOtherMessageName");
    }
}
```

After defining the client messages (this example includes TWO client messages), the protocol client side and protocol server side must both make use of them.

The client protocol side will do like this:

```
public class BarProtocolClientSide : ProtocolClientSide<Bar>  
{
    ...

	private Func<Task> SendMyMessageName;
	private Func<MyClientPayload, Task> SendMyOtherMessageName;

    protected override void Initialize()  
    {
        SendMyMessageName = MakeSender("MyMessageName");
        SendMyOtherMessageName = MakeSender<MyClientPayload>("MyOtherMessageName");
	}

    ...
}
```

Then the user must ensure they have a mean to invoke `SendMyMessageName()` (which can be awaited or fired-and-forgotten) and `SendMyOtherMessageName(new MyClientPayload() { ...fields ...})` (which can also be awaited or fired-and-forgotten).

Meanwhile, the server protocol side will do like this:

```
public class BarProtocolServerSide : ProtocolServerSide<Bar>  
{
    ...

    protected override void SetIncomingMessageHandlers()  
    {
        AddIncomingMessageHandler(
            "MyMessageName", async (proto, clientId) => {
                ... process this message which has no parameters ...
            }
        );
        AddIncomingMessageHandler<MyClientPayload>(
            "MyOtherMessageName", async (proto, clientId, message) => {
                ... process this message which has parameters ...
            }
        );        
    }
    
    ...
}
```

The server can process this in many different forms. Example:

1. Doing some Unity server-side code.
2. Accessing the storage and doing something.
3. Responding by sending server messages to the client.

However, these handlers _run in a separate thread and not in the main Unity thread_. Later, a utility will be detailed to synchronize code executions to the main Unity thread (otherwise, stuff like accessing a GameObject's properties will fail).
### Messages sent from server to client

Typically, these are responses or notifications. In order to define a new server message, the first thing is to define it in the protocol definition, in a similar way. We'll also define a sample payload sent in one of the sample commands:

```
using AlephVault.Unity.Binary;

class MyServerPayload : ISerializable
{
    ... fields here ...
    
	public void Serialize(Serializer serializer)  
	{
	    // Refer to unity-binary's documentation
	    // to have a complete understanding on
	    // serialization.
	}
}
```

And then the protocol definition's specification (notice how it's `DefineServerMessage` now instead of `DefineClientMessage`):

```
...

public class Bar : ProtocolDefinition
{
	protected override void DefineMessages()  
    {
        ... the client messages from the previous section ...
        
        DefineServerMessage("SomeServerMessage");
        DefineServerMessage<MyServerPayload>("SomeOtherServerMessage");
    }
}
```

This is an example involving also a message with parameters and a message with no parameters. However, this time the message comes from the server and to the client.

Then, the protocol server side will add, for each message, one out of two flavors:

1. A function to send a server message to a specific client.
2. A function to send a server message to _many_ clients.

```
public class BarProtocolServerSide : ProtocolServerSide<Bar>  
{
    ...

    // Parameterless sender to one client, if wanted.
    private Func<ulong, Task> SendSomeServerMessage;

    // With-parameters sender to one client, if wanted.
    private Func<ulong, MyServerPayload, Task> SendSomeOtherServerMessage;

    // Parameterless sender to many clients, if wanted.
	private Func<IEnumerable, Dictionary<ulong, Task>> BroadcastSomeServerMessage;

    // With-parameters sender to many clients, if wanted.
	private Func<IEnumerable, MyServerPayload, Dictionary<ulong, Task>> BroadcastSomeServerMessage;

    protected override void Initialize()  
    {
        // The four cases are instantiated here, as needed:
        SendSomeServerMessage = MakeSender("SomeServerMessage");
        SendSomeOtherServerMessage = MakeSender<MyServerPayload>("SomeOtherServerMessage");
        BroadcastSomeServerMessage = MakeBroadcaster("SomeServerMessage");
        BroadcastSomeOtherServerMessage = MakeBroadcaster<MyServerPayload>("SomeOtherServerMessage");
    }
    
    ...
}
```

The main difference between the _sender_ when created in a client (for a client message) and when created in a server (for a server message) is that, in the server side, also the `client id` must be specified. This will be explained later, but essentially boils down that there's only one server the client would want to send messages (the one it's connected to) but there are many clients a server would like to send a message to, each with a unique id.

Also, the server has the concept of _broadcasters_. A broadcaster is a function that tells not just one but _many_ clients to send a message instead (passing a null list of clients sends the signals to _all of the established connections_).

Invoking all of them are _awaiteable_ calls like these:

```
await SendSomeServerMessage(1); // To client id 1.
await SendSomeOtherServerMessage(2, new MyServerPayload() { ... }); // To client 2, with data.
Dictionary<ulong, Task> result = BroadcastSomeServerMessage(null); // To all the current clients.
Dictionary<ulong, Task> result2 = BroadcastSomeOtherServerMessage(new ulong[] {2, 3, 5}, new MyServerPayload() { ... }); // To clients 2, 3 and 5, with some payload.
```

A friendly reminder here: As long as the sender / broadcaster function(s) are assigned and the server is started and not stopped, there's no limit on when the server can invoke those senders: either responding to a user or in an asynchronous life-cycle are valid moments to send messages

The next thing to set up is the client side for the incoming server message:

```
public class BarProtocolClientSide : ProtocolClientSide<Bar>  
{
    ...

    protected override void SetIncomingMessageHandlers()  
    {
        AddIncomingMessageHandler(
            "SomeServerMessage", async (proto) => {
                ... process this message which has no parameters ...
            }
        );
        AddIncomingMessageHandler<MyServerPayload>(
            "SomeOtherServerMessage", async (proto, message) => {
                ... process this message which has parameters ...
            }
        );        
    }
    
    ...
}
```

Notice that, pretty much like in the previous case, the client-side handlers are similar to the server-side handlers but they don't have a concept of a `client id` among the arguments. This is because, again, a client only knows a single server and expects data from a single server.

As part of the response, the client can do anything (reflecting local objects, or even replying back by invoking another sender on its own...).

### Handling in Main Thread

Both the client's and server's versions of `AddIncomingMessageHandler` and `AddIncomingMessageHandler<T>` functions accept a callback that will handle the incoming message. However, **it's important to remember that those callbacks will not be invoked in the main Unity thread**. This is important, since making use of a GameObject's properties will trigger obscure exceptions when you try.

The only way to avoid these annoying errors is to force an execution in the main thread. This can be wrapped like this (both for the client and the server, it's done the same way) in the handlers.

Client side:

```
public class BarProtocolClientSide : ProtocolClientSide<Bar>  
{
    ...

    protected override void SetIncomingMessageHandlers()  
    {
        AddIncomingMessageHandler(
            "SomeServerMessage", async (proto) => {
                ... perhaps do something which is not Main-Thread required ...
                await RunInMainThread(async () => {
                    ... do Main-Thread required code here...
                });
                ... perhaps do something which is not Main-Thread required ...
            }
        );
        AddIncomingMessageHandler<MyServerPayload>(
            "SomeOtherServerMessage", async (proto, message) => {
                ... perhaps do something which is not Main-Thread required ...
                await RunInMainThread(async () => {
                    ... do Main-Thread required code here...
                });
                ... perhaps do something which is not Main-Thread required ...
            }
        );        
    }
    
    ...
}
```

Server side:

```
public class BarProtocolServerSide : ProtocolServerSide<Bar>  
{
    ...

    protected override void SetIncomingMessageHandlers()  
    {
        AddIncomingMessageHandler(
            "MyMessageName", async (proto, clientId) => {
                ... perhaps do something which is not Main-Thread required ...
                await RunInMainThread(async () => {
                    ... do Main-Thread required code here...
                });
                ... perhaps do something which is not Main-Thread required ...
            }
        );
        AddIncomingMessageHandler<MyClientPayload>(
            "MyOtherMessageName", async (proto, clientId, message) => {
                ... perhaps do something which is not Main-Thread required ...
                await RunInMainThread(async () => {
                    ... do Main-Thread required code here...
                });
                ... perhaps do something which is not Main-Thread required ...
            }
        );        
    }
    
    ...
}
```

The `RunInMainThread` utility method runs a callback (which is an asynchronous function or a synchronous one) by queuing it in the related server's `AsyncQueueManager` (defined in `unity-support`), which will have a life-cycle to execute all those queued callbacks in the main thread.

**WARNING**: **Do NEVER EVER EVER EVER EVER** invoke an `await RunInMainThread(() => {...})` (this means: with the `await` keyword included) inside a callback passed to an outer `RunInMainThread(() => { ... })` call. **NEVER**. By doing that, the code will enter a non-crashing deadlock that will run unnoticed in the code. Ensure the `RunInMainThread` calls are as topmost as needed only, and not inside game logic itself.

## Managing servers and clients

Launching and stopping a server is done via code:

```
// Starts a server. This call is synchronous and might
// raise an error.
//
// All the ProtocolServerSide<T> components that are
// attached to the server will have its callbacks
// invoked: OnServerStarted().
theInSceneServer.StartServer(2357); // 2357 is the port.

// Stops the server. This call is synchronous and might
// raise an error.
//
// All the ProtocolServerSide<T> components that are
// attached to the server will have its callbacks invoked:
// OnServerStopped(null).
theInSceneServer.StopServer();
```

Forcing disconnecting a client from the server:

```
// Closes a client by its client id. This call is
// synchronous and might raise an error.
// Let's say the client to disconnect has id: 1.
//
// All the ProtocolServerSide<T> components that are
// attached to the server will have its callbacks
// invoked: OnDisconnected(1, null).
//
// Also, similar for all the ProtocolClientSide<T>
// components: OnDisconnected(null). HOWEVER THIS
// IS NOT ALWAYS GUARANTEED. ENSURE YOU NOTIFY A
// CLIENT (via an **AWAITED** sender) about the
// connection being closed before closing it.
theInSceneServer.Close(1);
```

Launching and stopping a client is done via code:

```
// Starts a client. This call is synchronous and might
// raise an error.
//
// All the ProtocolClientSide<T> components that are
// attached to the client will, on success, have their
// callbacks invoked: OnConnected().
//
// Also, in the same line all the ProtocolServerSide<T>
// components in the server will be triggered when the
// connection succeeds: OnConnected(newClientId).
theInSceneClient.Connect("localhost", 2357); // Localhost and same port.

// Stops a client. This call is synchronous and might raise
// an error.
//
// All the ProtocolClientSide<T> components that are
// attached to the client will have their callbacks
// invoked: OnDisconnected().
//
// Also, similar for all the ProtocolServerSide<T>
// components: OnDisconnected(theClientId, null). STILL
// THIS IS NOT ALWAYS GUARANTEED. ENSURE YOU NOTIFY THE
// SERVER (via an **AWAITED** sender) about the connection
// being closed before closing it.
```

### Understanding client ids

Client ids are unique and actually a very big pool of numbers. When a server just starts receiving incoming connections, the first client id it will get is `1` and the number will grow. So at first glance is incremental. However, as the connections get closed or dropped, for any reason, the ids are released (and, if the last ids are releases systematically, the server counter may actually decrease and some ids might be reused).

With this in mind, the user must keep that the connection id does not reflect any user authentication of any sort but a transient connection id instead that can be tracked only for gaming purposes (e.g. associating, later, an authenticated session against a connection id).

## Throttling

There are ways to throttle your interactions, in particular if the interactions are costly in execution. This includes innocent errors or malicious DDoS / LOIC payloads.

For this, a server might have a `ServerSideThrottler` attached to it (to filter out malicious calls from the clients).
Also, the client might have a `ClientSideThrottler` attached to it (to locally prevent malicious sends to the server).

### Client-side throttling

Client-side throttling can be understood in terms of a prior but not mandatory check on the client side to a priori ensure they don't flood malicious commands.
However, since this is client-side, it's not the _ground truth_ throttling.

In order to implement client-side throttling, ensure your desired protocol client side makes use of ClientSideThrottler:

1. Add the client-side throttler.
2. Refer it on Setup.
3. Throttle your code.

Adding the throttler involves adding a component:

```
using UnityEngine;
using AlephVault.Unity.Meetgard.Authoring.Behaviours.Client;

[RequireComponent(typeof(ClientSideThrottler))]
public class BarProtocolClientSide : ProtocolClientSide<Bar>  
{
}
```

Then, refer it on Setup:

```
...

[RequireComponent(typeof(ClientSideThrottler))]
public class BarProtocolClientSide : ProtocolClientSide<Bar>  
{
    ...

    private ClientSideThrottler throttler;
    
    protected override void Setup()  
    {
        throttler = GetComponent<ClientSideThrottler>();
    }

    ...
}
```

With this done, the `ClientSideThrottler` will be configurable in the client object. It has means to configure the available intervals (lapses) and it has at least 1 element (lapse 0) of 1 second.
An arbitrary amount of lapses can be configured that way, and they can be referred by their index (0 to N-1). Typically, they will be _stricter_ than the corresponding server-side throttles.

Making use of the throttle mechanism can be done like this over one of those methods.

```
...

[RequireComponent(typeof(ClientSideThrottler))]
public class BarProtocolClientSide : ProtocolClientSide<Bar>  
{
    ...

    // We modify the initialization code to wrap the
    // command senders in this throttle technology.
    protected override void Initialize()  
    {
        // Throttler for void senders.
        // The `0` corresponds to use the 0th throttler. It's optional (assumed 0 by default) and must stand for one of the defined lapses' indices.
        SendMyMessageName = throttler.MakeThrottledSender(MakeSender("MyMessageName"), 0);
        // Throttler for typed senders.
        SendMyOtherMessageName = throttler.MakeThrottledSender(MakeSender<MyClientPayload>("MyOtherMessageName"));
	}
	
    ...
}
```

Now, making use of `SendMyMessageName` and `SendMyMessageName` will only succeed after the intervals elapsed after the last call over that lapse index. "Too quick" calls will silently fail / be ignored.
In this example, calling either function will have to wait after calling either function (since both use index `[0]` in those calls) and waiting for the lapse.

Also, there are cases where users might want to change lapses (not adding / removing, but only changing):

```
throttler.SetThrottleTime(time[, index = 0]);
float time = throttler.GetThrottleTime([index = 0]);
```

Where the time is expressed in seconds, and the default index to affect/get is 0 if not specified.

### Server-side throttling

Contrary to the client side, the server-side throttle is the ground truth. Instead of configuring senders, the _handlers_ are throttled.

The first thing, similar to the clients, is to add the component to the server object, reference it and configure it:

```
using UnityEngine;
using AlephVault.Unity.Meetgard.Authoring.Behaviours.Server;

[RequireComponent(typeof(ServerSideThrottler))]
public class BarProtocolServerSide : ProtocolServerSide<Bar>  
{
    ...
    
    private ServerSideThrottler throttler;
    
    protected override void Setup()  
    {
        throttler = GetComponent<ServerSideThrottler>(); 
    }
    
    ...

    protected override void SetIncomingMessageHandlers()  
    {
        AddIncomingMessageHandler(
            "MyMessageName", async (proto, clientId) => {
                // Invoke the throttling over index 0 (by default)
                // and the given connectionId.
                await throttler.DoThrottled(clientId, async () => {
                    ... perhaps do something which is not Main-Thread required ...
                    await RunInMainThread(async () => {
                        ... do Main-Thread required code here...
                    });
                    ... perhaps do something which is not Main-Thread required ...
                });
            }
        );
        AddIncomingMessageHandler<MyClientPayload>(
            "MyOtherMessageName", async (proto, clientId, message) => {
                // Invoke the throttling over index 0 (specified)
                // and the given connectionId. Also, custom logic
                // for when the command was throttled (it can be
                // null to perform no logic at all).
                await throttler.DoThrottled(clientId, async () => {
                    ... perhaps do something which is not Main-Thread required ...
                    await RunInMainThread(async () => {
                        ... do Main-Thread required code here...
                    });
                    ... perhaps do something which is not Main-Thread required ...
                }, async (clientId, now, count) => {
                    // `clientId` is ulong.
                    // `now` is DateTime.
                    // `count` is int. The number of previous consecutive throttles. 
                    ...something here to process the throttle and prev. count...
                }, 0);
            }
        );        
    }
    
    ...
}
```

Both handlers, in this case, pay attention to the current clientId and the constant index 0 (one by default, the other explicitly).

Also, aside from setting the lapses (more than one can be set) via editor, two methods exist to change them by code:

```
throttler.SetThrottleTime(time[, index = 0]);
float time = throttler.GetThrottleTime([index = 0]);
```

(Yes: the signatures are the same as in `ClientSideThrottler`).

## Versioning

Games and applications evolve over time, and it's not weird that attempted connections happen to have mismatching protocols.

The `ZeroProtocol` on both sides (client and server) must define a version in all their 4 fields: Major, Minor, Revision and Release Type.

As of today, the version match is exact: If the attempting client does not have a version _exactly matching_ what the server has configured, the connection is rejected. _In a future, that version match might not be THAT exact_.

The `ZeroProtocolClientSide` offers a set of events that can be listened to:

1. `public event Func<Task> OnZeroHandshakeStarted`: Triggered when the server issued a handshake to the client. Also, the client will start the version handshake.
2. `public event Func<Task> OnVersionMatch`: Triggered when the server decided that there's a version match and the connection will continue.
3. `public event Func<Task> OnVersionMismatch`: Triggered when the server decided that there's a version mismatch. The connection will also terminate from the server side. The client side must also close its connection (_in a future version, this will be done automatically_).
4. `public event Func<Task> OnTimeout`: Triggered when the server decided there was a timeout: the server did not receive the version handshake from the client. The connection will also terminete from the server side. The client side must also close its connection (_in a future version, this will be done automatically_).
5. `public event Func<Task> OnNotReadyError`: Triggered when the server decided the command cannot be processed since the version handshake was not done first.
6. `public event Func<Task> OnAlreadyDoneError`: Triggered when the server decided the version handshake was already done, and it's not needed again.

## Extra types

### `Nothing` type

There's a dummy type which does not hold any data. It's called `Nothing` (implementing `unity-binary`'s `ISerializable` interface). Its serialization and de-serialization will do nothing.
