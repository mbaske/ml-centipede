using UnityEngine;

namespace Centipede
{
    /// <summary>
    /// Resets all child <see cref="ArticulationBody"/> components.
    /// </summary>
    public class ArticulationBodyResetter : MonoBehaviour
    {
        private ArticulationBody[] m_Bodies;
        private Transform[] m_Transforms;
        private Vector3[] m_DefPositions;
        private Quaternion[] m_DefRotations;
        private InvokeAfterFrames m_DelayedEnable;

        /// <summary>
        /// Initializes the component.
        /// </summary>
        public void Initialize()
        {
            m_Bodies = GetComponentsInChildren<ArticulationBody>();
            m_DelayedEnable = new InvokeAfterFrames(this, Enable, 1);

            int n = m_Bodies.Length;
            m_Transforms = new Transform[n];
            m_DefPositions = new Vector3[n];
            m_DefRotations = new Quaternion[n];

            for (int i = 0; i < n; i++)
            {
                m_Transforms[i] = m_Bodies[i].GetComponent<Transform>();
                m_DefPositions[i] = m_Transforms[i].localPosition;
                m_DefRotations[i] = m_Transforms[i].localRotation;
            }
        }

        /// <summary>
        /// Resets all child <see cref="ArticulationBody"/> components.
        /// <see cref="ArticulationBody.TeleportRoot(Vector3, Quaternion)"/>
        /// wasn't always working as expected - my workaround is disabling
        /// the ArticulationBodies, resetting their transforms and re-enabling
        /// the ArticulationBodies after one frame.
        /// </summary>
        public void ManagedReset()
        {
            for (int i = 0; i < m_Bodies.Length; i++)
            {
                m_Bodies[i].enabled = false;

                ArticulationDrive drive = m_Bodies[i].xDrive;
                drive.target = 0;
                m_Bodies[i].xDrive = drive;

                drive = m_Bodies[i].yDrive;
                drive.target = 0;
                m_Bodies[i].yDrive = drive;

                drive = m_Bodies[i].zDrive;
                drive.target = 0;
                m_Bodies[i].zDrive = drive;

                m_Bodies[i].velocity = Vector3.zero;
                m_Bodies[i].angularVelocity = Vector3.zero;
                m_Transforms[i].localPosition = m_DefPositions[i];
                m_Transforms[i].localRotation = m_DefRotations[i];
            }

            m_DelayedEnable.Invoke();
        }

        private void Enable()
        {
            foreach (ArticulationBody body in m_Bodies)
            {
                body.enabled = true;
            }
        }
    }
}