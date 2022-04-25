using UnityEngine;
using Unity.MLAgents.Sensors;

namespace Centipede
{
    /// <summary>
    /// Raycast detection of environment ground. Component is attached to a leg.
    /// </summary>
    public class GroundDetector : MonoBehaviour
    {
        private bool m_IsTouching;

        private const int k_GroundLayer = 3;
        private const int k_GroundMask = 1 << 3;
        private const float k_RayLength = 0.1f;
        // Ray origin relative to leg transform.
        private readonly Vector3 m_Pos = new Vector3(0, 0.0775f, -0.0375f);

        /// <summary>
        /// Resets the detector.
        /// </summary>
        public void ManagedReset()
        {
            m_IsTouching = false;
        }

        /// <summary>
        /// Calculates normalized ground distance value and adds it to the sensor.
        /// The value is -1 if the leg touches the ground and > 0 as long as it's 
        /// lifted above the ground.
        /// </summary>
        /// <param name="sensor">The agent's VectorSensor</param>
        public void CollectObservations(VectorSensor sensor)
        {
            if (m_IsTouching)
            {
                sensor.AddObservation(-1);
            }
            else
            {
                Transform t = transform;
                // Ignore scale.
                Vector3 p = Matrix4x4.TRS(t.position, t.rotation,
                    Vector3.one).MultiplyPoint3x4(m_Pos);

                if (Physics.Raycast(p, Vector3.down, 
                        out RaycastHit hit, k_RayLength, k_GroundMask))
                {
                    sensor.AddObservation(hit.distance / k_RayLength);
                    // Debug.DrawLine(p, hit.point, Color.red);
                }
                else
                {
                    // No ground detected.
                    sensor.AddObservation(1);
                    // Debug.DrawRay(p, Vector3.down * k_RayLength, Color.green);
                }
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.layer == k_GroundLayer)
            {
                m_IsTouching = true;
            }
        }

        private void OnCollisionExit(Collision collision)
        {
            if (collision.gameObject.layer == k_GroundLayer)
            {
                m_IsTouching = false;
            }
        }
    }
}