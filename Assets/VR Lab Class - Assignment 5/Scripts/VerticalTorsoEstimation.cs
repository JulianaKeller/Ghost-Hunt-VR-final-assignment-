using UnityEngine;

namespace SampleSolution
{
    public class VerticalTorsoEstimation : MonoBehaviour
    {
        [Tooltip(
            "The head transform which is used to align the body to. If nothing is specified, transform of parent GameObject will be used.")]
        public Transform headTransform;

        private void Awake()
        {
            if (headTransform == null)
                headTransform = transform.parent;
        }

        void Update()
        {
            ApplyTorsoUpdate(headTransform, transform);
        }

        public static void ApplyTorsoUpdate(Transform headTransform, Transform torsoTransform)
        {
            torsoTransform.position = headTransform.position;

            var eulerX = headTransform.localEulerAngles.x;
            var invRot = Quaternion.Inverse(headTransform.localRotation);
            var lookAtRot =
                Quaternion.LookRotation(Vector3.Cross(headTransform.localRotation * Vector3.right, Vector3.up));

            torsoTransform.localRotation = invRot * lookAtRot;

            // When user is looking down, we need to move the body back, to avoid clipping into the shirt
            if (eulerX > 0.0f && eulerX < 120.0f)
            {
                float movementFactor;
                if (eulerX < 90.0f)
                {
                    movementFactor = eulerX / 90.0f;
                }
                else
                {
                    eulerX -= 90.0f;
                    movementFactor = 1.0f - eulerX / 30.0f;
                }

                torsoTransform.position +=
                    torsoTransform.forward * -(movementFactor * 0.30f * torsoTransform.lossyScale.x);
            }
        }
    }
}