using UnityEngine;

namespace Centipede
{
    /// <summary>
    /// Centipede body segment, connects to previous segment or head.
    /// </summary>
    public class BodySegment : AbstractBone
    {
        [Range(-1, 1), Tooltip("Normalized rotation value for agent heuristic")]
        public float m_X;

        [Range(-1, 1), Tooltip("Normalized rotation value for agent heuristic")]
        public float m_Y;

        [Range(-1, 1), Tooltip("Normalized rotation value for agent heuristic")]
        public float m_Z;

        /// <inheritdoc />
        public override float[] Heuristic()
        {
            return new[] {m_X, m_Y, m_Z};
        }

        /// <inheritdoc />
        public override void ApplyActions(float[] actions, ref int index)
        {
            ArticulationDrive drive = m_Body.xDrive;
            drive.target = actions[index++] * m_DriveLimit;
            m_Body.xDrive = drive;

            drive = m_Body.yDrive;
            drive.target = actions[index++] * m_DriveLimit;
            m_Body.yDrive = drive;

            drive = m_Body.zDrive;
            drive.target = actions[index++] * m_DriveLimit;
            m_Body.zDrive = drive;
        }
    }
}