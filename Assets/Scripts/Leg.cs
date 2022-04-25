using UnityEngine;

namespace Centipede
{
    /// <summary>
    /// Centipede leg, connects to <see cref="BodySegment"/>.
    /// </summary>
    public class Leg : AbstractBone
    {
        [Range(-1, 1), Tooltip("Normalized rotation value for agent heuristic")]
        public float m_Y;

        [Range(-1, 1), Tooltip("Normalized rotation value for agent heuristic")]
        public float m_Z;

        /// <inheritdoc />
        public override float[] Heuristic()
        {
            return new[] {m_Y, m_Z};
        }

        /// <inheritdoc />
        public override void ApplyActions(float[] actions, ref int index)
        {
            var drive = m_Body.yDrive;
            drive.target = actions[index++] * m_DriveLimit;
            m_Body.yDrive = drive;

            drive = m_Body.zDrive;
            drive.target = actions[index++] * m_DriveLimit;
            m_Body.zDrive = drive;
        }
    }
}