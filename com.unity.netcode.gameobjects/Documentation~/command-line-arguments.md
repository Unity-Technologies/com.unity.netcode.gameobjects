# WORK IN PROGRESS
# Command line arguments

You can use [command line arguments](https://docs.unity3d.com/Documentation/Manual/CommandLineArguments.html) to configure certain aspects of your game at launch. This is especially useful for dedicated server builds, where arguments let you override default network settings such as the IP address and port.

## Using Command Line Arguments

When launching a standalone build (for example, a headless dedicated server), you can supply custom arguments to modify runtime behavior.

Available arguments:
- -port
- -ip

Unity provides built-in parsing for standard arguments, and you can extend this behavior by adding your own.

---

## Custom Arguments

You can define additional custom command line arguments and retrieve them through the `CommandLineOptions` class.
Use `GetArgs()` in your project code to collect and process these values.

[!NOTE]
Adding a custom command line argument requires you to explicitly retrieve and handle it in your implementation.

---

## Example: Reading Command Line Arguments
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

---

## Overriding Connection Data

If you want to ignore the connection **port** provided through command line arguments, you can override it by using the optional `forceOverride` parameter in:

```
UnityTransport.SetConnectionData(string ip, ushort port, string listenAddress, bool forceOverride);
```

Setting `forceOverride` to `true` ensures that the values you pass to `SetConnectionData` override any values specified via command line arguments.

