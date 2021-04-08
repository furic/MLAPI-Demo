# MLAPI-Demo
A simple project that use MLAPI and its Steam transport with basic connection function and few RPC calls for demo purpose.

** Tested Environment
- Windows 10 19042.867
- Unity 2019.4.18f1
- [MLAPI 0.1.0 preview](https://github.com/Unity-Technologies/com.unity.multiplayer.mlapi.git?path=/com.unity.multiplayer.mlapi#release/0.1.0)
- [SteamP2P Transport 1.0.0](https://github.com/Unity-Technologies/mlapi-community-contributions/tree/master/Transports/com.mlapi.contrib.transport.steamp2p)

** Prerequisites
- Make sure Steam is running on the background.
- Use another PC/Mac with another Steam account to test connection.

** Demo
*** Menu
![Screenshot 1 - Menu](Documents/screenshot_01_menu.jpg)
In menu scrren, you can:
- Host a room.
- Join a public room.
- Join a friend room.

*** Host
![Screenshot 2 - Host](Documents/screenshot_02_host.jpg)
Show only on host, you can:
- Invite a friend to the room.
- Execute the RPC events on different objects.

*** Client
![Screenshot 3 - Client](Documents/screenshot_03_client.jpg)
Show only on client, you can:
- Execute the RPC events on different objects.

** Prefabs
There are 3 prefabs for different demo purposes:
- `MultiplayerDemoPlayer`: The player prefab, each client has its own one.
- `MultiplayerDemoSceneObject`: An object spawned with the scene, MLAPI reference them using soft-sync, owned by host.
- `MultiplayerDemoSpawnedObject`: An object spawned after connection, owned by host.