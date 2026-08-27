using UnityEngine;

namespace ApiUpdaterProject
{
    /// <summary>
    /// Type argument for the <c>NetcodeEditorBase&lt;TT&gt;</c> references in Assets/Editor.
    /// </summary>
    /// <remarks>
    /// Deliberately a local MonoBehaviour rather than NGO's NetworkManager. com.unity.transport 6.6.0
    /// (the builtin on some 6000.6 editors) ships a `Unity.Netcode.NetworkManager` of its own in
    /// Unity.Networking.Transport.NetcodeInterop, so any NetworkManager reference from an
    /// auto-referencing assembly like Assembly-CSharp-Editor is CS0433-ambiguous. The type argument is
    /// incidental to what the upgrade test measures - only the generic type reference itself has to be
    /// rewritten - so this keeps the test independent of the resolved transport version.
    /// </remarks>
    // public, not internal: the references to it live in Assembly-CSharp-Editor, a different assembly.
    public class UpgradeProbeBehaviour : MonoBehaviour
    {
    }
}
