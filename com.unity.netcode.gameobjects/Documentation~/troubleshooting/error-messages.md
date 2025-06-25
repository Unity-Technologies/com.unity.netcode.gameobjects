# Netcode for GameObjects error messages

Learn more about Unity error messages, including error collecting, issues that cause them, and how to handle.

## Error Capturing

Error messages are captured and returned through Unity Editor Diagnostics (required) and Roslyn Analyzers.

* Unity ILPP and Editor errors are the source of truth. ILPP occurs in Unity and returns error messages, which prevents you from building/playing your game (hard compile errors).
* [Roslyn Analyzers](https://devblogs.microsoft.com/dotnet/write-better-code-faster-with-roslyn-analyzers/) provide immediate feedback within the IDE, without jumping back to Unity to let it compile with your new changes.  

## NetworkObject errors

**Error:**
* `Can't find pending soft sync object. Is the projects the same? UnityEngine.Debug:LogError(Object)`
* `ArgumentNullException: Can't spawn null object  Parameter name: netObject`

This exception should only occur if your scenes aren't the same, for example if the scene of your server has a `NetworkObject` which isn't present in the client scene. Verify the scene objects work correctly by entering playmode in both editors.

## ServerRPC errors

**Error:**
* Server: `[Netcode] Only owner can invoke ServerRPC that is marked to require ownership`
* Host: `KeyNotFoundException: The given key wasn't present in the dictionary.`

The ServerRPC should only be used on the server. Make sure to add `isServer` check before calling.

If the call is added correctly but still returns a `nullreferenceexception`, `NetworkManager.Singleton` may be returning `null`. Verify you created the `GameObject` with a `NetworkManager` component, which handles all initialization. `NetworkManager.Singleton` is a reference to a instance of the `NetworkManager` component.
