# Command-line arguments

Use [command-line arguments](https://docs.unity3d.com/Documentation/Manual/CommandLineArguments.html) to configure certain aspects of your game at launch. This is especially useful for dedicated server builds, where arguments let you override default network settings such as the IP address and port.

## Using command-line arguments

When launching a standalone build (such as a headless dedicated server), you can supply custom arguments to modify runtime behavior.

Reserved arguments:

- `-port`
- `-ip`

Unity provides built-in parsing for standard arguments, and you can extend this behavior by adding your own.

## Custom arguments

You can define additional custom command-line arguments and retrieve them through the `CommandLineOptions` class. Use `GetArgs()` in your project code to collect and process these values.

> [!NOTE]
> Adding a custom command-line argument requires you to explicitly retrieve and handle it in your implementation.

## Example

The following code shows you an example of defining and then reading a custom command-line argument.

[!code-cs[](../Tests/Runtime/DocumentationCodeSamples/Configuration/CommandLineOptionsDocsTests.cs#DefineAndRead)]

Usage example:

[!code-cs[](../Tests/Runtime/DocumentationCodeSamples/Configuration/CommandLineOptionsDocsTests.cs#Usage)]

## Override connection data

By default, the command line provided connection port and ip address take precedence over runtime configured values when using the [Unity transport](./advanced-topics/transports.md#unity-transport-package).

> [!NOTE]
> When the [Unity dedicated server package](https://docs.unity3d.com/Documentation/Manual/dedicated-server.html) is installed, Unity transport will use the port and ip address provided by the dedicated server package.

If you want to ignore the connection port provided through command-line arguments, you can override it by setting the `forceOverrideCommandLineArgs` parameter of UnityTransport's [`SetConnectionData`](xref:Unity.Netcode.Transports.UTP.UnityTransport.SetConnectionData(System.Boolean,System.String,System.UInt16,System.String)). Setting `forceOverrideCommandLineArgs` to `true` ensures that the values you pass to `SetConnectionData` will override any values specified via command-line arguments.

## Additional resources

- [Command-line arguments in the Unity Manual](https://docs.unity3d.com/Documentation/Manual/CommandLineArguments.html)
