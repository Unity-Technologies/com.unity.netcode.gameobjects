using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using MLAPI.Messaging;
using MLAPI.NetworkVariable;

namespace MLAPI.RuntimeTests
{
    public class NetworkVariableTests
    {
        [SetUp]
        public void Setup()
        {
            // Create, instantiate, and host
            Assert.IsTrue(NetworkManagerHelper.StartNetworkManager(out _));
        }

        /// <summary>
        /// This tests the RPC Queue outbound and inbound buffer capabilities.
        /// It will send
        /// </summary>
        /// <returns>IEnumerator</returns>
        [UnityTest]
        public IEnumerator TestAllNetworkVariableTypes()
        {
            Guid gameObjectId = NetworkManagerHelper.AddGameNetworkObject("NetworkVariableTestComponent");

            var networkVariableTestComponent = NetworkManagerHelper.AddComponentToObject<NetworkVariableTestComponent>(gameObjectId);

            NetworkManagerHelper.SpawnNetworkObject(gameObjectId);

            // Start Testing
            networkVariableTestComponent.EnableTesting = true;

            var testsAreComplete = networkVariableTestComponent.IsTestComplete();

            // Wait for the rpc pipeline test to complete or if we exceeded the maximum iterations bail
            while (!testsAreComplete)
            {
                //Wait for 20ms
                yield return new WaitForSeconds(0.02f);

                testsAreComplete = networkVariableTestComponent.IsTestComplete();
            }

            // Stop Testing
            networkVariableTestComponent.EnableTesting = false;

            // Just disable this once we are done.
            networkVariableTestComponent.gameObject.SetActive(false);

            Assert.IsTrue(testsAreComplete);
        }

        [TearDown]
        public void TearDown()
        {
            // Stop, shutdown, and destroy
            NetworkManagerHelper.ShutdownNetworkManager();
        }
    }



    //public class NetworkVariableTestComponent : NetworkBehaviour
    //{
    //    private NetworkVariableBool         m_NetworkVariableBool;
    //    private NetworkVariableByte         m_NetworkVariableByte;
    //    private NetworkVariableColor        m_NetworkVariableColor;
    //    private NetworkVariableColor32      m_NetworkVariableColor32;
    //    private NetworkVariableDouble       m_NetworkVariableDouble;
    //    private NetworkVariableFloat        m_NetworkVariableFloat;
    //    private NetworkVariableInt          m_NetworkVariableInt;
    //    private NetworkVariableLong         m_NetworkVariableLong;
    //    private NetworkVariableSByte        m_NetworkVariableSByte;
    //    private NetworkVariableQuaternion   m_NetworkVariableQuaternion;
    //    private NetworkVariableShort        m_NetworkVariableShort;
    //    private NetworkVariableString       m_NetworkVariableString;
    //    private NetworkVariableVector4      m_NetworkVariableVector4;
    //    private NetworkVariableVector3      m_NetworkVariableVector3;
    //    private NetworkVariableVector2      m_NetworkVariableVector2;
    //    private NetworkVariableRay          m_NetworkVariableRay;
    //    private NetworkVariableULong        m_NetworkVariableULong;
    //    private NetworkVariableUInt         m_NetworkVariableUInt;
    //    private NetworkVariableUShort       m_NetworkVariableUShort;

    //    public OnMyNetworkVariableChanged<bool> Bool_Var;
    //    public OnMyNetworkVariableChanged<byte> Byte_Var;
    //    public OnMyNetworkVariableChanged<Color> Color_Var;
    //    public OnMyNetworkVariableChanged<Color32> Color32_Var;
    //    public OnMyNetworkVariableChanged<double> Double_Var;
    //    public OnMyNetworkVariableChanged<float> Float_Var;
    //    public OnMyNetworkVariableChanged<int> Int_Var;
    //    public OnMyNetworkVariableChanged<long> Long_Var;
    //    public OnMyNetworkVariableChanged<sbyte> Sbyte_Var;
    //    public OnMyNetworkVariableChanged<Quaternion> Quaternion_Var;
    //    public OnMyNetworkVariableChanged<short> Short_Var;
    //    public OnMyNetworkVariableChanged<string> String_Var;
    //    public OnMyNetworkVariableChanged<Vector4> Vector4_Var;
    //    public OnMyNetworkVariableChanged<Vector3> Vector3_Var;
    //    public OnMyNetworkVariableChanged<Vector2> Vector2_Var;
    //    public OnMyNetworkVariableChanged<Ray> Ray_Var;
    //    public OnMyNetworkVariableChanged<ulong> Ulong_Var;
    //    public OnMyNetworkVariableChanged<uint> Uint_Var;
    //    public OnMyNetworkVariableChanged<ushort> Ushort_Var;


    //    private int m_ChangedCounter;

    //    public bool EnableTesting;
    //    private bool m_Initialized;
    //    private bool m_FinishedTests;


    //    // Start is called before the first frame update
    //    private void InitializeTest()
    //    {
    //        //Generic Constructor Test Coverage
    //        m_NetworkVariableBool = new NetworkVariableBool();
    //        m_NetworkVariableByte = new NetworkVariableByte();
    //        m_NetworkVariableColor = new NetworkVariableColor();
    //        m_NetworkVariableColor32 = new NetworkVariableColor32();
    //        m_NetworkVariableDouble = new NetworkVariableDouble();
    //        m_NetworkVariableFloat = new NetworkVariableFloat();
    //        m_NetworkVariableInt = new NetworkVariableInt();
    //        m_NetworkVariableLong = new NetworkVariableLong();
    //        m_NetworkVariableSByte = new NetworkVariableSByte();
    //        m_NetworkVariableQuaternion = new NetworkVariableQuaternion();
    //        m_NetworkVariableShort = new NetworkVariableShort();
    //        m_NetworkVariableString = new NetworkVariableString();
    //        m_NetworkVariableVector4 = new NetworkVariableVector4();
    //        m_NetworkVariableVector3 = new NetworkVariableVector3();
    //        m_NetworkVariableVector2 = new NetworkVariableVector2();
    //        m_NetworkVariableRay = new NetworkVariableRay();
    //        m_NetworkVariableULong = new NetworkVariableULong();
    //        m_NetworkVariableUInt = new NetworkVariableUInt();
    //        m_NetworkVariableUShort = new NetworkVariableUShort();

    //        //NetworkVariable Value Type Constructor Test Coverage
    //        m_NetworkVariableBool = new NetworkVariableBool(true);
    //        m_NetworkVariableByte = new NetworkVariableByte(0);
    //        m_NetworkVariableColor = new NetworkVariableColor(new Color(1,1,1,1));
    //        m_NetworkVariableColor32 = new NetworkVariableColor32(new Color32(1,1,1,1));
    //        m_NetworkVariableDouble = new NetworkVariableDouble(1.0);
    //        m_NetworkVariableFloat = new NetworkVariableFloat(1.0f);
    //        m_NetworkVariableInt = new NetworkVariableInt(1);
    //        m_NetworkVariableLong = new NetworkVariableLong(1);
    //        m_NetworkVariableSByte = new NetworkVariableSByte(0);
    //        m_NetworkVariableQuaternion = new NetworkVariableQuaternion(Quaternion.identity);
    //        m_NetworkVariableShort = new NetworkVariableShort(256);
    //        m_NetworkVariableString = new NetworkVariableString("My String Value");
    //        m_NetworkVariableVector4 = new NetworkVariableVector4(new Vector4(1,1,1,1));
    //        m_NetworkVariableVector3 = new NetworkVariableVector3(new Vector3(1, 1, 1));
    //        m_NetworkVariableVector2 = new NetworkVariableVector2(new Vector2(1, 1));
    //        m_NetworkVariableRay = new NetworkVariableRay(new Ray());
    //        m_NetworkVariableULong = new NetworkVariableULong(10);
    //        m_NetworkVariableUInt = new NetworkVariableUInt(10);
    //        m_NetworkVariableUShort = new NetworkVariableUShort(256);

    //        //NetworkVariable NetworkVariableSettings Constructor Test Coverage
    //        var settings = new NetworkVariableSettings();
    //        settings.ReadPermission = NetworkVariablePermission.ServerOnly;
    //        settings.WritePermission = NetworkVariablePermission.ServerOnly;
    //        m_NetworkVariableBool = new NetworkVariableBool(settings);
    //        m_NetworkVariableByte = new NetworkVariableByte(settings);
    //        m_NetworkVariableColor = new NetworkVariableColor(settings);
    //        m_NetworkVariableColor32 = new NetworkVariableColor32(settings);
    //        m_NetworkVariableDouble = new NetworkVariableDouble(settings);
    //        m_NetworkVariableFloat = new NetworkVariableFloat(settings);
    //        m_NetworkVariableInt = new NetworkVariableInt(settings);
    //        m_NetworkVariableLong = new NetworkVariableLong(settings);
    //        m_NetworkVariableSByte = new NetworkVariableSByte(settings);
    //        m_NetworkVariableQuaternion = new NetworkVariableQuaternion(settings);
    //        m_NetworkVariableShort = new NetworkVariableShort(settings);
    //        m_NetworkVariableString = new NetworkVariableString(settings);
    //        m_NetworkVariableVector4 = new NetworkVariableVector4(settings);
    //        m_NetworkVariableVector3 = new NetworkVariableVector3(settings);
    //        m_NetworkVariableVector2 = new NetworkVariableVector2(settings);
    //        m_NetworkVariableRay = new NetworkVariableRay(settings);
    //        m_NetworkVariableULong = new NetworkVariableULong(settings);
    //        m_NetworkVariableUInt = new NetworkVariableUInt(settings);
    //        m_NetworkVariableUShort = new NetworkVariableUShort(settings);


    //        //NetworkVariable Value Type and NetworkVariableSettings Constructor Test Coverage
    //        m_NetworkVariableBool = new NetworkVariableBool(settings, true);
    //        m_NetworkVariableByte = new NetworkVariableByte(settings, 0);
    //        m_NetworkVariableColor = new NetworkVariableColor(settings, new Color(1, 1, 1, 1));
    //        m_NetworkVariableColor32 = new NetworkVariableColor32(settings, new Color32(1, 1, 1, 1));
    //        m_NetworkVariableDouble = new NetworkVariableDouble(settings, 1.0);
    //        m_NetworkVariableFloat = new NetworkVariableFloat(settings, 1.0f);
    //        m_NetworkVariableInt = new NetworkVariableInt(settings, 1);
    //        m_NetworkVariableLong = new NetworkVariableLong(settings, 1);
    //        m_NetworkVariableSByte = new NetworkVariableSByte(settings, 0);
    //        m_NetworkVariableQuaternion = new NetworkVariableQuaternion(settings, Quaternion.identity);
    //        m_NetworkVariableShort = new NetworkVariableShort(settings, 256);
    //        m_NetworkVariableString = new NetworkVariableString(settings, "My String Value");
    //        m_NetworkVariableVector4 = new NetworkVariableVector4(settings, new Vector4(1, 1, 1, 1));
    //        m_NetworkVariableVector3 = new NetworkVariableVector3(settings, new Vector3(1, 1, 1));
    //        m_NetworkVariableVector2 = new NetworkVariableVector2(settings, new Vector2(1, 1));
    //        m_NetworkVariableRay = new NetworkVariableRay(settings, new Ray());
    //        m_NetworkVariableULong = new NetworkVariableULong(settings, 10);
    //        m_NetworkVariableUInt = new NetworkVariableUInt(settings, 10);
    //        m_NetworkVariableUShort = new NetworkVariableUShort(settings, 256);

            
    //        //Register for callbacks
    //        Bool_Var        = new OnMyNetworkVariableChanged<bool>(m_NetworkVariableBool);
    //        Byte_Var        = new OnMyNetworkVariableChanged<byte>(m_NetworkVariableByte);
    //        Color_Var       = new OnMyNetworkVariableChanged<Color>(m_NetworkVariableColor);
    //        Color32_Var     = new OnMyNetworkVariableChanged<Color32>(m_NetworkVariableColor32);
    //        Double_Var      = new OnMyNetworkVariableChanged<double>(m_NetworkVariableDouble);
    //        Float_Var       = new OnMyNetworkVariableChanged<float>(m_NetworkVariableFloat);
    //        Int_Var         = new OnMyNetworkVariableChanged<int>(m_NetworkVariableInt);
    //        Long_Var        = new OnMyNetworkVariableChanged<long>(m_NetworkVariableLong);
    //        Sbyte_Var       = new OnMyNetworkVariableChanged<sbyte>(m_NetworkVariableSByte);
    //        Quaternion_Var  = new OnMyNetworkVariableChanged<Quaternion>(m_NetworkVariableQuaternion);
    //        Short_Var       = new OnMyNetworkVariableChanged<short>(m_NetworkVariableShort);
    //        String_Var      = new OnMyNetworkVariableChanged<string>(m_NetworkVariableString);
    //        Vector4_Var     = new OnMyNetworkVariableChanged<Vector4>(m_NetworkVariableVector4);
    //        Vector3_Var     = new OnMyNetworkVariableChanged<Vector3>(m_NetworkVariableVector3);
    //        Vector2_Var     = new OnMyNetworkVariableChanged<Vector2>(m_NetworkVariableVector2);
    //        Ray_Var         = new OnMyNetworkVariableChanged<Ray>(m_NetworkVariableRay);
    //        Ulong_Var       = new OnMyNetworkVariableChanged<ulong>(m_NetworkVariableULong);
    //        Uint_Var        = new OnMyNetworkVariableChanged<uint>(m_NetworkVariableUInt);
    //        Ushort_Var      = new OnMyNetworkVariableChanged<ushort>(m_NetworkVariableUShort);


    //        Bool_Var.OnMyValueChanged += Bool_Var_OnMyValueChanged;
    //        Byte_Var.OnMyValueChanged += Byte_Var_OnMyValueChanged;
    //        Color_Var.OnMyValueChanged += Color_Var_OnMyValueChanged;
    //        Color32_Var.OnMyValueChanged += Color32_Var_OnMyValueChanged;
    //        Double_Var.OnMyValueChanged += Double_Var_OnMyValueChanged;
    //        Float_Var.OnMyValueChanged += Float_Var_OnMyValueChanged;
    //        Int_Var.OnMyValueChanged += Int_Var_OnMyValueChanged;
    //        Long_Var.OnMyValueChanged += Long_Var_OnMyValueChanged;
    //        Sbyte_Var.OnMyValueChanged += Sbyte_Var_OnMyValueChanged;
    //        Quaternion_Var.OnMyValueChanged += Quaternion_Var_OnMyValueChanged;
    //        Short_Var.OnMyValueChanged += Short_Var_OnMyValueChanged;
    //        String_Var.OnMyValueChanged += String_Var_OnMyValueChanged;
    //        Vector4_Var.OnMyValueChanged += Vector4_Var_OnMyValueChanged;
    //        Vector3_Var.OnMyValueChanged += Vector3_Var_OnMyValueChanged;
    //        Vector2_Var.OnMyValueChanged += Vector2_Var_OnMyValueChanged;
    //        Ray_Var.OnMyValueChanged += Ray_Var_OnMyValueChanged;
    //        Ulong_Var.OnMyValueChanged += Ulong_Var_OnMyValueChanged;
    //        Uint_Var.OnMyValueChanged += Uint_Var_OnMyValueChanged;
    //        Ushort_Var.OnMyValueChanged += Ushort_Var_OnMyValueChanged;

    //        m_ChangedCounter = 0;
    //    }

        

    //    private void Ushort_Var_OnMyValueChanged(ushort previous, ushort next)
    //    {
    //        if (previous != next)
    //        {
    //            m_ChangedCounter++;
    //        }            
    //    }

    //    private void Uint_Var_OnMyValueChanged(uint previous, uint next)
    //    {
    //        if (previous != next)
    //        {
    //            m_ChangedCounter++;
    //        }
    //    }

    //    private void Ulong_Var_OnMyValueChanged(ulong previous, ulong next)
    //    {
    //        if (previous != next)
    //        {
    //            m_ChangedCounter++;
    //        }
    //    }

    //    private void Ray_Var_OnMyValueChanged(Ray previous, Ray next)
    //    {
    //            m_ChangedCounter++;
    //    }

    //    private void Vector2_Var_OnMyValueChanged(Vector2 previous, Vector2 next)
    //    {
    //        if (previous != next)
    //        {
    //            m_ChangedCounter++;
    //        }
    //    }

    //    private void Vector3_Var_OnMyValueChanged(Vector3 previous, Vector3 next)
    //    {
    //        if (previous != next)
    //        {
    //            m_ChangedCounter++;
    //        }
    //    }

    //    private void Vector4_Var_OnMyValueChanged(Vector4 previous, Vector4 next)
    //    {
    //        if (previous != next)
    //        {
    //            m_ChangedCounter++;
    //        }
    //    }

    //    private void String_Var_OnMyValueChanged(string previous, string next)
    //    {
    //        if (previous != next)
    //        {
    //            m_ChangedCounter++;
    //        }
    //    }

    //    private void Short_Var_OnMyValueChanged(short previous, short next)
    //    {
    //        if (previous != next)
    //        {
    //            m_ChangedCounter++;
    //        }
    //    }

    //    private void Quaternion_Var_OnMyValueChanged(Quaternion previous, Quaternion next)
    //    {
    //        if (previous != next)
    //        {
    //            m_ChangedCounter++;
    //        }
    //    }

    //    private void Sbyte_Var_OnMyValueChanged(sbyte previous, sbyte next)
    //    {
    //        if (previous != next)
    //        {
    //            m_ChangedCounter++;
    //        }
    //    }

    //    private void Long_Var_OnMyValueChanged(long previous, long next)
    //    {
    //        if (previous != next)
    //        {
    //            m_ChangedCounter++;
    //        }
    //    }

    //    private void Int_Var_OnMyValueChanged(int previous, int next)
    //    {
    //        if (previous != next)
    //        {
    //            m_ChangedCounter++;
    //        }
    //    }

    //    private void Float_Var_OnMyValueChanged(float previous, float next)
    //    {
    //        if (previous != next)
    //        {
    //            m_ChangedCounter++;
    //        }
    //    }

    //    private void Double_Var_OnMyValueChanged(double previous, double next)
    //    {
    //        if (previous != next)
    //        {
    //            m_ChangedCounter++;
    //        }
    //    }

    //    private void Color32_Var_OnMyValueChanged(Color32 previous, Color32 next)
    //    {
    //        m_ChangedCounter++;
    //    }

    //    private void Color_Var_OnMyValueChanged(Color previous, Color next)
    //    {
    //        if (previous != next)
    //        {
    //            m_ChangedCounter++;
    //        }
    //    }

    //    private void Byte_Var_OnMyValueChanged(byte previous, byte next)
    //    {
    //        if (previous != next)
    //        {
    //            m_ChangedCounter++;
    //        }
    //    }

    //    private void Bool_Var_OnMyValueChanged(bool previous, bool next)
    //    {
    //        if (previous != next)
    //        {
    //            m_ChangedCounter++;
    //        }
    //    }

    //    public override void NetworkStart()
    //    {
    //        base.NetworkStart();
    //    }


    //    /// <summary>
    //    /// Returns back whether the test has completed the total number of iterations
    //    /// </summary>
    //    /// <returns></returns>
    //    public bool IsTestComplete()
    //    {

    //        return m_FinishedTests;
    //    }

    //    // Update is called once per frame
    //    private void Update()
    //    {
    //        if (EnableTesting)
    //        {
    //            if (!m_FinishedTests && NetworkManager != null && NetworkManager.IsListening)
    //            {
    //                if (!m_Initialized)
    //                {
    //                    InitializeTest();
    //                    m_Initialized = true;
    //                }
    //                else
    //                {
    //                    m_NetworkVariableBool.Value = false;
    //                    m_NetworkVariableByte.Value = 255;
    //                    m_NetworkVariableColor.Value = new Color(100, 100, 100);
    //                    m_NetworkVariableColor32.Value = new Color32(100, 100, 100, 100);
    //                    m_NetworkVariableDouble.Value = 1000;
    //                    m_NetworkVariableFloat.Value = 1000.0f;
    //                    m_NetworkVariableInt.Value = 1000;
    //                    m_NetworkVariableLong.Value = 100000;
    //                    m_NetworkVariableSByte.Value = -127;
    //                    m_NetworkVariableQuaternion.Value = new Quaternion(100, 100, 100, 100);
    //                    m_NetworkVariableShort.Value = short.MaxValue;
    //                    m_NetworkVariableString.Value = "My Changed String Value";
    //                    m_NetworkVariableVector4.Value = new Vector4(1000, 1000, 1000, 1000);
    //                    m_NetworkVariableVector3.Value = new Vector3(1000, 1000, 1000);
    //                    m_NetworkVariableVector2.Value = new Vector2(1000, 1000);
    //                    m_NetworkVariableRay.Value = new Ray(Vector3.one, Vector3.right);
    //                    m_NetworkVariableULong.Value = ulong.MaxValue;
    //                    m_NetworkVariableUInt.Value = uint.MaxValue;
    //                    m_NetworkVariableUShort.Value = ushort.MaxValue;
    //                    m_FinishedTests = true;
    //                }
    //            }
    //        }
    //    }
    //}

    //public class OnMyNetworkVariableChanged<T>
    //{
    //    private NetworkVariable<T> m_NetworkVariable;
    //    public delegate void OnMyValueChangedDelegateHandler(T previous, T next);
    //    public event OnMyValueChangedDelegateHandler OnMyValueChanged;

    //    private void OnVariableChanged(T previous, T next)
    //    {
    //        OnMyValueChanged?.Invoke(previous, next);
    //    }

    //    public OnMyNetworkVariableChanged(INetworkVariable networkVariable)
    //    {
    //        m_NetworkVariable = networkVariable as NetworkVariable<T>;
    //        m_NetworkVariable.OnValueChanged = OnVariableChanged;
    //    }
    //}
   
}
