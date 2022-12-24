using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

#if UNITY_EDITOR
using UnityEditor;
[CustomEditor(typeof(TestProject.ManualTests.IntegrationNetworkTransform))]
public class IntegrationNetworkTransformEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
    }
}
#endif

namespace TestProject.ManualTests
{
    public class IntegrationNetworkTransform : NetworkTransform
    {
#if DEBUG_NETWORKTRANSFORM
        public bool IsServerAuthoritative = true;

        public Vector3 LastUpdatedPosition;
        public Vector3 LastUpdatedScale;
        public Quaternion LastUpdatedRotation;

        public Vector3 PreviousUpdatedPosition;
        public Vector3 PreviousUpdatedScale;
        public Quaternion PreviousUpdatedRotation;

        protected override bool OnIsServerAuthoritative()
        {
            return IsServerAuthoritative;
        }

        private void UpdateTransformHistory(bool updatePosition, bool updateRotation, bool updateScale)
        {
            if (updatePosition)
            {
                if (CanCommitToTransform)
                {
                    PreviousUpdatedPosition = LastUpdatedPosition;
                }
                LastUpdatedPosition = InLocalSpace ? transform.localPosition : transform.position;
            }

            if (updateRotation)
            {
                if (CanCommitToTransform)
                {
                    PreviousUpdatedRotation = LastUpdatedRotation;
                }

                LastUpdatedRotation = InLocalSpace ? transform.localRotation : transform.rotation;
            }

            if (updateScale)
            {
                if (CanCommitToTransform)
                {
                    PreviousUpdatedScale = LastUpdatedScale;
                }
                LastUpdatedScale = transform.localScale;
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            UpdateTransformHistory(true, true, true);
        }

        protected override void OnNetworkTransformStateUpdate(ref NetworkTransformStateUpdate networkTransformStateUpdate)
        {
            UpdateTransformHistory(networkTransformStateUpdate.PositionUpdate, networkTransformStateUpdate.RotationUpdate, networkTransformStateUpdate.ScaleUpdate);
        }
#endif
    }
}
