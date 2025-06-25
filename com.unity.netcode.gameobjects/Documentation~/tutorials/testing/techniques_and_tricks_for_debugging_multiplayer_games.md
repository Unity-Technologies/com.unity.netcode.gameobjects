# Techniques and tricks for debugging multiplayer games

When debugging games, multiplayer games included, it helps to know the toolbag that we have at our disposal.

All the conventional game development wisdom applies, however certain scenarios that are typical to multiplayer game development call for special tricks and approaches.

Below is a list of practices and techniques that we use daily when working on the Boss Room sample. These recommendations, we find, compound into a more comfortable working experience when developing multiplayer games with Unity.

## Debugging tricks for multiplayer games

### Use ParrelSync clone-based workflow during development.

[ParrelSync workflow](testing_locally.md#parrelsync) is faster than creating builds and it allows you to debug the separate editor instances via separate IDE debuggers.

Use ParrelSync to run separate editor instances for your Host/Server and Client.

### Use debug drawing techniques extensively

Unity engine has two debug rendering APIs that are useful for the purposes of multiplayer game debugging:
 - [Debug.DrawRay](https://docs.unity3d.com/ScriptReference/Debug.DrawRay.html)
 - [Debug.DrawLine](https://docs.unity3d.com/ScriptReference/Debug.DrawLine.html)

Both of these functions allow us to draw arbitrary debug lines that would be visible in the Scene view and in the Game view, provided Gizmo rendering is enabled (to enable Gizmo rendering in Game view you need to click on the `Gizmos` menu at the top of the Game view).

The key trick here is to use different colors for different kinds of information and to make the lines stay long enough for visual inspection by setting `duration` parameter. This technique shines when it's combined with [screen recordings](#7-recording-the-video-of-gameplay) of [multiple peers running side by side in separate editor instances via ParrelSync](#1-use-parrelsync-workflow-during-development).

The code below would render a green debug line that's 2 units tall at the position of the transform, and this line would stay on screen for 4 seconds:
`Debug.DrawLine(this.transform.position, this.transform.position + Vector3.UP * 2f, Color.green, duration: 4f);`

When working on Boss Room we found it valuable to draw debug lines for the following items:
 - Visual position
 - Network position
 - Movement intention
 - Object interactions

### A Netcode Enabled Line Renderer
Sometimes it's useful to have visual feedback that shows a specific direction, value, or any other useful debug metric pertinent to your project.  Below is a fully working example of a netcode enabled line renderer that can be used for visual debugging purposes:
```csharp
using UnityEngine;
using Unity.Netcode;
public class NetcodeEnabledLineRenderer : NetworkBehaviour
{
    private LineRenderer m_LineRenderer;
    private Vector3[] m_Positions;
    private Material m_LineMaterial;

    [Tooltip("The default state of the netcode debug render lines")]
    [SerializeField]
    private bool m_EnableDebugLines = true;

    [Tooltip("The length of the line")]
    public float LineLength = 4.0f;

    [Tooltip("The width of the line")]
    public float LineWidth = 0.3f;

    private void Awake()
    {
        // Add a line renderer component
        m_LineRenderer = gameObject.AddComponent<LineRenderer>();

        // Switch to a legacy built-in shader that will work with just colors and/or a color gradient
        m_LineMaterial = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));

        // Assign this new material to the line renderer
        m_LineRenderer.material = m_LineMaterial;

        // Create our start color for the gradient
        var colorKeyStart = new GradientColorKey();
        colorKeyStart.color = Color.red;
        // The "start" should be 0.0f
        colorKeyStart.time = 0.0f;

        // Create our end color for the gradient
        var colorKeyEnd = new GradientColorKey();
        colorKeyEnd.color = Color.blue;
        // The "end" should be 1.0f
        colorKeyEnd.time = 1.0f;

        // Now create and apply the gradient
        m_LineRenderer.colorGradient = new Gradient() { colorKeys =
            new GradientColorKey[2] { colorKeyStart, colorKeyEnd },
            mode = GradientMode.Blend };

        //declare two positions for the start and end of the line
        m_Positions = new Vector3[2];

        //Ensure that the line renderer knows to draw in world space
        m_LineRenderer.useWorldSpace = true;

        //set the beginning and ending width of the line in units
        m_LineRenderer.startWidth = 0.3f;
        m_LineRenderer.endWidth = 0.3f;
    }

    public override void OnDestroy()
    {
        // With NetworkBehaviours, always make sure to invoke the
        // base.OnDestroy method
        base.OnDestroy();

        // We should always destroy any runtime materials we create
        if (m_LineMaterial != null)
        {
            Destroy(m_LineMaterial);
        }
    }

    /// <summary>
    /// Update the position of the line
    /// </summary>
    private void Update()
    {
        if (IsSpawned && m_EnableDebugLines)
        {
            //set the start and end positions of our line
            m_Positions[0] = transform.position;
            m_Positions[1] = transform.position + Vector3.up * LineLength;

            //feed the values to the line renderer
            m_LineRenderer.SetPositions(m_Positions);
        }
    }

    /// <summary>
    /// Example for having netcode debug render lines
    /// </summary>
    [Rpc(SendTo.ClientsAndHost)]
    private void ToggleDebugLinesClientRpc(bool drawDebugLines)
    {
        m_EnableDebugLines = drawDebugLines;
    }

    /// <summary>
    /// Server-Side only
    /// Enables/Disables the debug line rendering
    /// </summary>
    public void DebugLinesEnabled(bool enableDebugLines)
    {
        if (IsSpawned && IsServer)
        {
            m_EnableDebugLines = enableDebugLines;
            ToggleDebugLinesClientRpc(m_EnableDebugLines);
        }
    }
}
```

### Use debug logging for situations where visual debugging isn't appropriate.

Text-based logging is valuable for tracking down non-visual events (such as RPCs) and information.
It is a good idea to include network tick and client id in log messages so that it's easier to build a timeline when reading the logs.

### Use the artificial network conditioning tools.

The options that are currently available to us are covered in the related [article on artificial network conditioning tools](testing_with_artificial_conditions.md).

Of particular interest to us is the application-level network conditioning provided by [Network Simulator tools](https://docs-multiplayer.unity3d.com/tools/current/tools-network-simulator/), as it allows us to easily specify conditions for our individual peers that live within separate editors by means of ParrelSync.

Artificial network conditions allow the errors and oddities that are hidden by nigh-absence of lag when running your instances locally to show up, and it's a good thing!

### Capturing screen recordings of the game instances.

First of all, it's valuable to record both your Client and Server at the same time - it allows you to compare what is happening on either peer in realtime.

When recording your screen, sometimes it's hard to see if we are legitimately missing an update in our game or if it's just our recording refresh rate isn't synced with Unity's refresh calls.

In debug builds it's a great idea to show the Peer ID and the current frame number somewhere in the corner of the screen - this way there is a visual reference to the number of the frame we're currently observing on the recording.

Sometimes, despite us using good debug rendering and logging it's still hard to understand what's going on even when going through the frames one by one. Increasing our FixedTimeStep setting to a ridiculous value (something as high as `0.2`) helps to have more time to see what's going on.

The same applies to high latencies (1000ms) - these stress the lag hiding techniques, allowing us to visualize what the different lag hiding techniques are doing.

### Using breakpoints to debug a Client or Server

You can use breakpoints to debug a game, but your connection may time out if you stay too long in this mode. Since it pauses your game, you can temporarily increase the timeout value to avoid disconnecting.
