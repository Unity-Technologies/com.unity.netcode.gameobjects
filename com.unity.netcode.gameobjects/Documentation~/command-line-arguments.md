# WORK IN PROGRESS
# Command line arguments

You can use [command line arguments](https://docs.unity3d.com/Documentation/Manual/CommandLineArguments.html) to configure some aspects of your game. With dedicated server you can use command line arguments to override default ip address and port. 



Something you can use if you want to launch a standalone build (particulary usefull for dedicated server builds)
(include all known command line )
-port
-ip (TODO) check where is the endpoint, I may only need to assign it with no convert


we provided port and ip and you can add your own command line args and retieve them in the CommanLineOptions class and grab them in your project by using GetArgs

[!Note]
Adding a command line argument requires that you retrieve and set that command line argument




You can force override the command line arguments by using the optional boolean argument in SetConnectionData(string, ushort, string, bool) from UnityTransport.

~~~~