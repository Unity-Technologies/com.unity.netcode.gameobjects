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
```
private const string k_OverrideArg = "-argName";

private bool ParseCommandLineOptions(out string command)
{
    if (CommandLineOptions.Instance.GetArg(k_OverrideArg) is string argValue)
    {
        command = argValue;
        return true;
    }
    command = default;
    return false;
}
```

Usage example:

```
if (ParseCommandLineOptions(out var command))
{
    // Your logic here
}
```

## Override connection data

If you want to ignore the connection port provided through command-line arguments, you can override it by using the optional `forceOverride` parameter in:

```
UnityTransport.SetConnectionData(string ip, ushort port, string listenAddress, bool forceOverride);
```

Setting `forceOverride` to `true` ensures that the values you pass to `SetConnectionData` override any values specified via command-line arguments.

## Additional resources

- [Command-line arguments in the Unity Manual](https://docs.unity3d.com/6000.2/Documentation/Manual/CommandLineArguments.html)
