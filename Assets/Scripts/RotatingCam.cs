using UnityEngine;

namespace Centipede
{
    public class RotatingCam : MonoBehaviour
    {
        [SerializeField] private Transform m_Target;
        [SerializeField] private float m_Height = 0.75f;
        [SerializeField] private float m_Offset = -0.25f;
        [SerializeField] private float m_Radius = 1.25f;
        [SerializeField] private float m_Speed = 0.25f;
        [SerializeField] private float m_Smooth = 0.5f;

        private Vector3 m_LookTarget;
        private Vector3 m_LookVelo;
        private Vector3 m_MoveTarget;
        private Vector3 m_MoveVelo;
        private float m_Angle;
        
        private void LateUpdate()
        {
            m_Angle += Time.deltaTime * m_Speed;
            Vector3 pos = m_Target.position;
            Vector3 target = pos + new Vector3(
                Mathf.Cos(m_Angle) * m_Radius,
                m_Height,
                Mathf.Sin(m_Angle) * m_Radius);
            m_MoveTarget = Vector3.SmoothDamp(
                m_MoveTarget, target, ref m_MoveVelo, 0.5f);
            transform.position = m_MoveTarget;

            target = pos + Vector3.up * m_Offset;
            m_LookTarget = Vector3.SmoothDamp(
                m_LookTarget, target, ref m_LookVelo, m_Smooth);
            transform.LookAt(m_LookTarget);
        }
    }
}