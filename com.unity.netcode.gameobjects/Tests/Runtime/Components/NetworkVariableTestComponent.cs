using UnityEngine;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// This provides coverage for all of the predefined NetworkVariable types
    /// The initial goal is for generalized full coverage of NetworkVariables:
    /// Covers all of the various constructor calls (i.e. various parameters or no parameters)
    /// Covers the local NetworkVariable's OnValueChanged functionality (i.e. when a specific type changes do we get a notification?)
    /// This was built as a NetworkBehaviour for further client-server unit testing patterns when this capability is available.
    /// </summary>
    internal class NetworkVariableTestComponent : NetworkBehaviour
    {
        private NetworkVariable<bool> m_NetworkVariableBool;
        private NetworkVariable<byte> m_NetworkVariableByte;
        private NetworkVariable<Color> m_NetworkVariableColor;
        private NetworkVariable<Color32> m_NetworkVariableColor32;
        private NetworkVariable<double> m_NetworkVariableDouble;
        private NetworkVariable<float> m_NetworkVariableFloat;
        private NetworkVariable<int> m_NetworkVariableInt;
        private NetworkVariable<long> m_NetworkVariableLong;
        private NetworkVariable<sbyte> m_NetworkVariableSByte;
        private NetworkVariable<Quaternion> m_NetworkVariableQuaternion;
        private NetworkVariable<short> m_NetworkVariableShort;
        private NetworkVariable<Vector4> m_NetworkVariableVector4;
        private NetworkVariable<Vector3> m_NetworkVariableVector3;
        private NetworkVariable<Vector2> m_NetworkVariableVector2;
        private NetworkVariable<Ray> m_NetworkVariableRay;
        private NetworkVariable<ulong> m_NetworkVariableULong;
        private NetworkVariable<uint> m_NetworkVariableUInt;
        private NetworkVariable<ushort> m_NetworkVariableUShort;


        public NetworkVariableHelper<bool> Bool_Var;
        public NetworkVariableHelper<byte> Byte_Var;
        public NetworkVariableHelper<Color> Color_Var;
        public NetworkVariableHelper<Color32> Color32_Var;
        public NetworkVariableHelper<double> Double_Var;
        public NetworkVariableHelper<float> Float_Var;
        public NetworkVariableHelper<int> Int_Var;
        public NetworkVariableHelper<long> Long_Var;
        public NetworkVariableHelper<sbyte> Sbyte_Var;
        public NetworkVariableHelper<Quaternion> Quaternion_Var;
        public NetworkVariableHelper<short> Short_Var;
        public NetworkVariableHelper<Vector4> Vector4_Var;
        public NetworkVariableHelper<Vector3> Vector3_Var;
        public NetworkVariableHelper<Vector2> Vector2_Var;
        public NetworkVariableHelper<Ray> Ray_Var;
        public NetworkVariableHelper<ulong> Ulong_Var;
        public NetworkVariableHelper<uint> Uint_Var;
        public NetworkVariableHelper<ushort> Ushort_Var;


        public bool EnableTesting;
        private bool m_Initialized;
        private bool m_FinishedTests;
        private bool m_ChangesAppliedToNetworkVariables;

        private float m_WaitForChangesTimeout;

        // Start is called before the first frame update
        private void InitializeTest()
        {
            // Generic Constructor Test Coverage
            m_NetworkVariableBool = new NetworkVariable<bool>();
            m_NetworkVariableByte = new NetworkVariable<byte>();
            m_NetworkVariableColor = new NetworkVariable<Color>();
            m_NetworkVariableColor32 = new NetworkVariable<Color32>();
            m_NetworkVariableDouble = new NetworkVariable<double>();
            m_NetworkVariableFloat = new NetworkVariable<float>();
            m_NetworkVariableInt = new NetworkVariable<int>();
            m_NetworkVariableLong = new NetworkVariable<long>();
            m_NetworkVariableSByte = new NetworkVariable<sbyte>();
            m_NetworkVariableQuaternion = new NetworkVariable<Quaternion>();
            m_NetworkVariableShort = new NetworkVariable<short>();
            m_NetworkVariableVector4 = new NetworkVariable<Vector4>();
            m_NetworkVariableVector3 = new NetworkVariable<Vector3>();
            m_NetworkVariableVector2 = new NetworkVariable<Vector2>();
            m_NetworkVariableRay = new NetworkVariable<Ray>();
            m_NetworkVariableULong = new NetworkVariable<ulong>();
            m_NetworkVariableUInt = new NetworkVariable<uint>();
            m_NetworkVariableUShort = new NetworkVariable<ushort>();


            // NetworkVariable Value Type Constructor Test Coverage
            m_NetworkVariableBool = new NetworkVariable<bool>(true);
            m_NetworkVariableByte = new NetworkVariable<byte>((byte)0);
            m_NetworkVariableColor = new NetworkVariable<Color>(new Color(1, 1, 1, 1));
            m_NetworkVariableColor32 = new NetworkVariable<Color32>(new Color32(1, 1, 1, 1));
            m_NetworkVariableDouble = new NetworkVariable<double>(1.0);
            m_NetworkVariableFloat = new NetworkVariable<float>(1.0f);
            m_NetworkVariableInt = new NetworkVariable<int>(1);
            m_NetworkVariableLong = new NetworkVariable<long>(1);
            m_NetworkVariableSByte = new NetworkVariable<sbyte>((sbyte)0);
            m_NetworkVariableQuaternion = new NetworkVariable<Quaternion>(Quaternion.identity);
            m_NetworkVariableShort = new NetworkVariable<short>(256);
            m_NetworkVariableVector4 = new NetworkVariable<Vector4>(new Vector4(1, 1, 1, 1));
            m_NetworkVariableVector3 = new NetworkVariable<Vector3>(new Vector3(1, 1, 1));
            m_NetworkVariableVector2 = new NetworkVariable<Vector2>(new Vector2(1, 1));
            m_NetworkVariableRay = new NetworkVariable<Ray>(new Ray());
            m_NetworkVariableULong = new NetworkVariable<ulong>(1);
            m_NetworkVariableUInt = new NetworkVariable<uint>(1);
            m_NetworkVariableUShort = new NetworkVariable<ushort>(1);

            m_NetworkVariableBool = new NetworkVariable<bool>(NetworkVariableReadPermission.Everyone);
            m_NetworkVariableByte = new NetworkVariable<byte>(NetworkVariableReadPermission.Everyone);
            m_NetworkVariableColor = new NetworkVariable<Color>(NetworkVariableReadPermission.Everyone);
            m_NetworkVariableColor32 = new NetworkVariable<Color32>(NetworkVariableReadPermission.Everyone);
            m_NetworkVariableDouble = new NetworkVariable<double>(NetworkVariableReadPermission.Everyone);
            m_NetworkVariableFloat = new NetworkVariable<float>(NetworkVariableReadPermission.Everyone);
            m_NetworkVariableInt = new NetworkVariable<int>(NetworkVariableReadPermission.Everyone);
            m_NetworkVariableLong = new NetworkVariable<long>(NetworkVariableReadPermission.Everyone);
            m_NetworkVariableSByte = new NetworkVariable<sbyte>(NetworkVariableReadPermission.Everyone);
            m_NetworkVariableQuaternion = new NetworkVariable<Quaternion>(NetworkVariableReadPermission.Everyone);
            m_NetworkVariableShort = new NetworkVariable<short>(NetworkVariableReadPermission.Everyone);
            m_NetworkVariableVector4 = new NetworkVariable<Vector4>(NetworkVariableReadPermission.Everyone);
            m_NetworkVariableVector3 = new NetworkVariable<Vector3>(NetworkVariableReadPermission.Everyone);
            m_NetworkVariableVector2 = new NetworkVariable<Vector2>(NetworkVariableReadPermission.Everyone);
            m_NetworkVariableRay = new NetworkVariable<Ray>(NetworkVariableReadPermission.Everyone);
            m_NetworkVariableULong = new NetworkVariable<ulong>(NetworkVariableReadPermission.Everyone);
            m_NetworkVariableUInt = new NetworkVariable<uint>(NetworkVariableReadPermission.Everyone);
            m_NetworkVariableUShort = new NetworkVariable<ushort>(NetworkVariableReadPermission.Everyone);


            // NetworkVariable Value Type and NetworkVariableSettings Constructor Test Coverage
            m_NetworkVariableBool = new NetworkVariable<bool>(NetworkVariableReadPermission.Everyone, true);
            m_NetworkVariableByte = new NetworkVariable<byte>(NetworkVariableReadPermission.Everyone, 0);
            m_NetworkVariableColor = new NetworkVariable<Color>(NetworkVariableReadPermission.Everyone, new Color(1, 1, 1, 1));
            m_NetworkVariableColor32 = new NetworkVariable<Color32>(NetworkVariableReadPermission.Everyone, new Color32(1, 1, 1, 1));
            m_NetworkVariableDouble = new NetworkVariable<double>(NetworkVariableReadPermission.Everyone, 1.0);
            m_NetworkVariableFloat = new NetworkVariable<float>(NetworkVariableReadPermission.Everyone, 1.0f);
            m_NetworkVariableInt = new NetworkVariable<int>(NetworkVariableReadPermission.Everyone, 1);
            m_NetworkVariableLong = new NetworkVariable<long>(NetworkVariableReadPermission.Everyone, 1);
            m_NetworkVariableSByte = new NetworkVariable<sbyte>(NetworkVariableReadPermission.Everyone, 0);
            m_NetworkVariableQuaternion = new NetworkVariable<Quaternion>(NetworkVariableReadPermission.Everyone, Quaternion.identity);
            m_NetworkVariableShort = new NetworkVariable<short>(NetworkVariableReadPermission.Everyone, 1);
            m_NetworkVariableVector4 = new NetworkVariable<Vector4>(NetworkVariableReadPermission.Everyone, new Vector4(1, 1, 1, 1));
            m_NetworkVariableVector3 = new NetworkVariable<Vector3>(NetworkVariableReadPermission.Everyone, new Vector3(1, 1, 1));
            m_NetworkVariableVector2 = new NetworkVariable<Vector2>(NetworkVariableReadPermission.Everyone, new Vector2(1, 1));
            m_NetworkVariableRay = new NetworkVariable<Ray>(NetworkVariableReadPermission.Everyone, new Ray());
            m_NetworkVariableULong = new NetworkVariable<ulong>(NetworkVariableReadPermission.Everyone, 1);
            m_NetworkVariableUInt = new NetworkVariable<uint>(NetworkVariableReadPermission.Everyone, 1);
            m_NetworkVariableUShort = new NetworkVariable<ushort>(NetworkVariableReadPermission.Everyone, 1);

            // Use this nifty class: NetworkVariableHelper
            // Tracks if NetworkVariable changed invokes the OnValueChanged callback for the given instance type
            Bool_Var = new NetworkVariableHelper<bool>(m_NetworkVariableBool);
            Byte_Var = new NetworkVariableHelper<byte>(m_NetworkVariableByte);
            Color_Var = new NetworkVariableHelper<Color>(m_NetworkVariableColor);
            Color32_Var = new NetworkVariableHelper<Color32>(m_NetworkVariableColor32);
            Double_Var = new NetworkVariableHelper<double>(m_NetworkVariableDouble);
            Float_Var = new NetworkVariableHelper<float>(m_NetworkVariableFloat);
            Int_Var = new NetworkVariableHelper<int>(m_NetworkVariableInt);
            Long_Var = new NetworkVariableHelper<long>(m_NetworkVariableLong);
            Sbyte_Var = new NetworkVariableHelper<sbyte>(m_NetworkVariableSByte);
            Quaternion_Var = new NetworkVariableHelper<Quaternion>(m_NetworkVariableQuaternion);
            Short_Var = new NetworkVariableHelper<short>(m_NetworkVariableShort);
            Vector4_Var = new NetworkVariableHelper<Vector4>(m_NetworkVariableVector4);
            Vector3_Var = new NetworkVariableHelper<Vector3>(m_NetworkVariableVector3);
            Vector2_Var = new NetworkVariableHelper<Vector2>(m_NetworkVariableVector2);
            Ray_Var = new NetworkVariableHelper<Ray>(m_NetworkVariableRay);
            Ulong_Var = new NetworkVariableHelper<ulong>(m_NetworkVariableULong);
            Uint_Var = new NetworkVariableHelper<uint>(m_NetworkVariableUInt);
            Ushort_Var = new NetworkVariableHelper<ushort>(m_NetworkVariableUShort);
        }

        /// <summary>
        /// Test result for all values changed the expected number of times (once per unique NetworkVariable type)
        /// </summary>
        public bool DidAllValuesChange()
        {
            if (NetworkVariableBaseHelper.VarChangedCount == NetworkVariableBaseHelper.InstanceCount)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Returns back whether the test has completed the total number of iterations
        /// </summary>
        public bool IsTestComplete()
        {
            return m_FinishedTests;
        }

        // Update is called once per frame
        private void Update()
        {
            if (EnableTesting)
            {
                //Added timeout functionality for near future changes to NetworkVariables and the Snapshot system
                if (!m_FinishedTests && m_ChangesAppliedToNetworkVariables)
                {
                    //We finish testing if all NetworkVariables changed their value or we timed out waiting for
                    //all NetworkVariables to change their value
                    m_FinishedTests = DidAllValuesChange() || (m_WaitForChangesTimeout < Time.realtimeSinceStartup);
                }
                else
                {
                    if (NetworkManager != null && NetworkManager.IsListening)
                    {
                        if (!m_Initialized)
                        {
                            InitializeTest();
                            m_Initialized = true;
                        }
                        else
                        {
                            //Now change all of the values to make sure we are at least testing the local callback
                            m_NetworkVariableBool.Value = false;
                            m_NetworkVariableByte.Value = 255;
                            m_NetworkVariableColor.Value = new Color(100, 100, 100);
                            m_NetworkVariableColor32.Value = new Color32(100, 100, 100, 100);
                            m_NetworkVariableDouble.Value = 1000;
                            m_NetworkVariableFloat.Value = 1000.0f;
                            m_NetworkVariableInt.Value = 1000;
                            m_NetworkVariableLong.Value = 100000;
                            m_NetworkVariableSByte.Value = -127;
                            m_NetworkVariableQuaternion.Value = new Quaternion(100, 100, 100, 100);
                            m_NetworkVariableShort.Value = short.MaxValue;
                            m_NetworkVariableVector4.Value = new Vector4(1000, 1000, 1000, 1000);
                            m_NetworkVariableVector3.Value = new Vector3(1000, 1000, 1000);
                            m_NetworkVariableVector2.Value = new Vector2(1000, 1000);
                            m_NetworkVariableRay.Value = new Ray(Vector3.one, Vector3.right);
                            m_NetworkVariableULong.Value = ulong.MaxValue;
                            m_NetworkVariableUInt.Value = uint.MaxValue;
                            m_NetworkVariableUShort.Value = ushort.MaxValue;

                            //Set the timeout (i.e. how long we will wait for all NetworkVariables to have registered their changes)
                            m_WaitForChangesTimeout = Time.realtimeSinceStartup + 0.50f;
                            m_ChangesAppliedToNetworkVariables = true;
                        }
                    }
                }
            }
        }
    }
}
