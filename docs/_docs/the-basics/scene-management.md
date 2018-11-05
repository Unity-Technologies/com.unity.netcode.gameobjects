---
title: Scene Management
permalink: /wiki/scene-management/
---

The MLAPI can manage synchronized scene management for you. To use this, it first has to be enabled in NetworkingConfiguration. EnableSceneSwitching has to be set to true and all scenes that are going to be used during Networking has to be registered. The NetworkingConfiguration.RegisteredScenes list has to be populated with all scene names, this a simple security measure to ensure rouge servers don't request client's to switch to sensitive scenes.

Note:
_The scene that is active when the Server is started has to be registered_

### Usage
```csharp
//This can only be called on the server
NetworkSceneManager.SwitchScene(mySceneName);
```