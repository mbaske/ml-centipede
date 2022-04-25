using UnityEngine;
using Unity.MLAgents.Sensors;

namespace Centipede
{
    /// <summary>
    /// A bone can be articulated using its <see cref="ArticulationBody"/>.
    /// <see cref="BodySegment"/> and <see cref="Leg"/> inherit from AbstractBone.
    /// </summary>
    public abstract class AbstractBone : MonoBehaviour
    {
        /// <summary>
        /// Whether the agent can control this bone's joint rotation.
        /// </summary>
        public bool IsControllable { get; private set; }

        protected ArticulationBody m_Body;
        protected float m_DriveLimit;
        private Vector3 m_DefEulers;

        /// <summary>
        /// Managed component initialization.
        /// </summary>
        /// <param name="isControllable">Whether the agent
        /// can control the bone's joint rotation</param>
        public virtual void Initialize(bool isControllable)
        {
            IsControllable = isControllable;

            m_Body = GetComponent<ArticulationBody>();
            m_DefEulers = transform.localEulerAngles;

            // NOTE Assuming identical and symmetrical 
            // limits for all drives of the bone type.
            m_DriveLimit = m_Body.zDrive.upperLimit;
        }

        /// <summary>
        /// Returns the heuristic action values for this bone.
        /// Currently, these are just inspector settings for joint rotation.
        /// </summary>
        /// <returns>Continuous action values</returns>
        public abstract float[] Heuristic();

        ///// <summary>
        ///// Resets the <see cref="ArticulationBody"/> rotation.
        ///// </summary>
        //public abstract void ManagedReset();

        /// <summary>
        /// Applies actions to the <see cref="ArticulationBody"/> rotation.
        /// </summary>
        /// <param name="actions">Continuous agent action values</param>
        /// <param name="index">Action index</param>
        public abstract void ApplyActions(float[] actions, ref int index);

        /// <summary>
        /// Calculates normalized angles and adds them to the sensor.
        /// We're using eulers here since rotation limits are < 90 degrees.
        /// </summary>
        /// <param name="sensor">The agent's VectorSensor</param>
        public virtual void CollectObservations(VectorSensor sensor)
        {
            Vector3 e = transform.localEulerAngles - m_DefEulers;
            e.x = Mathf.Clamp(Mathf.DeltaAngle(0, e.x) / m_DriveLimit, -1, 1);
            e.y = Mathf.Clamp(Mathf.DeltaAngle(0, e.y) / m_DriveLimit, -1, 1);
            e.z = Mathf.Clamp(Mathf.DeltaAngle(0, e.z) / m_DriveLimit, -1, 1);
            sensor.AddObservation(e);
        }

        /// <summary>
        /// Sets drive params for <see cref="ArticulationBody"/>.
        /// Currently, this method is only invoked for body segments
        /// in case they are NOT controlled by agent actions.
        /// Otherwise, we use the values set in the ArticulationBody
        /// component inspector.
        /// </summary>
        /// <param name="stiffness">Spring stiffness</param>
        /// <param name="damping">Spring damping</param>
        /// <param name="forceLimit">Drive force limit</param>
        public void SetDriveParams(float stiffness, float damping, float forceLimit)
        {
            ArticulationDrive drive = m_Body.xDrive;
            drive.stiffness = stiffness;
            drive.damping = damping;
            drive.forceLimit = forceLimit;
            m_Body.xDrive = drive;

            drive = m_Body.yDrive;
            drive.target = 0;
            drive.stiffness = stiffness;
            drive.damping = damping;
            drive.forceLimit = forceLimit;
            m_Body.yDrive = drive;

            drive = m_Body.zDrive;
            drive.target = 0;
            drive.stiffness = stiffness;
            drive.damping = damping;
            drive.forceLimit = forceLimit;
            m_Body.zDrive = drive;
        }
    }
}