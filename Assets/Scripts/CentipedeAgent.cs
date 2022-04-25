using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using System;
using System.Linq;
using Random = UnityEngine.Random;

namespace Centipede
{
    /// <summary>
    /// This agent controls centipede bones for generating walking motion.
    /// </summary>
    public class CentipedeAgent : Agent
    {
        [SerializeField, Tooltip("Whether to control both, legs and body segments, or legs only")]
        private bool m_ControlBodySegmentRotations = true;

        private void OnValidate()
        {
            m_NumActions = NumActions;
            var param = GetComponent<BehaviorParameters>().BrainParameters;
            var spec = param.ActionSpec;
            spec.NumContinuousActions = m_NumActions;
            param.ActionSpec = spec;
        }


        [SerializeField, Tooltip("Interval (in decision steps) for randomizing target position" +
            " (set 0 to disable randomization)")]
        private int m_RandomizationInterval;
        private bool m_RandomizeTarget;

        [SerializeField, Tooltip("Interval (in decision steps) for sending metrics to Tensorboard")]
        private int m_StatsInterval;
        private StatsRecorder m_Stats;

        [SerializeField, Tooltip("Optional target direction indicator")]
        private Transform m_Arrow;
        private bool m_ShowArrow;

        private Vector3 m_Forward;
        private Vector3 m_Position;
        private Vector3 m_DefPosition;
        private Vector3 m_PrevPosition;
        private Vector3 m_TargetPosition;
        private Vector3 m_TargetDirection;
        private float m_LocalTargetAngle;
        private float m_DeltaTime;
        private float[] m_PrevActions;
        private int m_NumActions;
        private int m_DecisionInterval;
        private int m_DecisionCount;

        private ArticulationBodyResetter m_Resetter;
        private GroundDetector[] m_Detectors;
        private AbstractBone[] m_Bones;
        private Transform[] m_Segments;
        
        private int NumActions => m_ControlBodySegmentRotations
                ? 147 // 21 bones x 7 (2 legs x 2 DOF + 1 body segment x 3 DOF)
                : 84; // 21 bones x 4 (2 legs x 2 DOF)
        

        /// <inheritdoc />
        // Child component initialization and resetting is managed by the agent, in order
        // to avoid any code execution order issues we could run into if child components
        // were utilizing their own MonoBehaviour handlers.
        public override void Initialize()
        {
            m_Stats = Academy.Instance.StatsRecorder;
            m_RandomizeTarget = m_RandomizationInterval > 0;
            m_DecisionInterval = GetComponent<DecisionRequester>().DecisionPeriod;
            m_DeltaTime = Time.fixedDeltaTime * m_DecisionInterval;
            m_ShowArrow = m_Arrow != null && m_Arrow.gameObject.activeSelf;
            
            m_Detectors = GetComponentsInChildren<GroundDetector>();
            m_Resetter = GetComponent<ArticulationBodyResetter>();
            m_Resetter.Initialize();

            m_NumActions = NumActions;
            m_PrevActions = new float[m_NumActions];
            
            m_Bones = GetComponentsInChildren<AbstractBone>();
            m_Segments = GetComponentsInChildren<BodySegment>().Select(x => x.transform).ToArray();
            
            foreach (AbstractBone bone in m_Bones)
            {
                bone.Initialize(m_ControlBodySegmentRotations || bone is Leg);

                if (!m_ControlBodySegmentRotations && bone is BodySegment)
                {
                    // Non-controllable connecting segments.
                    // TBD values, body should be somewhat flexible.
                    bone.SetDriveParams(500, 100, 10000);
                }
            }
            
            OnUpdate();
            m_DefPosition = m_Position;
        }

        /// <inheritdoc />
        // Resets the agent.
        public override void OnEpisodeBegin()
        {
            m_DecisionCount = 0;
            m_PrevPosition = m_DefPosition;
            m_TargetDirection = Vector3.forward;
            
            Array.Clear(m_PrevActions, 0, m_NumActions);
            m_Resetter.ManagedReset();
            
            foreach (GroundDetector detector in m_Detectors)
            {
                detector.ManagedReset();
            }
        }

        /// <inheritdoc />
        // We use global observation stacking (x 2) in order to infer
        // angular velocities from bone joint rotations, as well as
        // leg velocities relative to the ground (distance). 
        public override void CollectObservations(VectorSensor sensor)
        {
            m_DecisionCount++;
            OnUpdate();
            
            if (m_RandomizeTarget)
            {
                Vector3 delta = m_TargetPosition - m_Position;
                m_TargetDirection = Vector3.ProjectOnPlane(delta, Vector3.up).normalized;
                
                if (m_ShowArrow)
                {
                    m_Arrow.SetPositionAndRotation(m_Position, Quaternion.LookRotation(m_TargetDirection));
                }
                
                if (delta.sqrMagnitude < 1 || m_DecisionCount % m_RandomizationInterval == 0)
                {
                    // Will consider new target at next decision.
                    RandomizeTarget();
                }
            }

            m_LocalTargetAngle = Vector3.SignedAngle(
                m_Forward, m_TargetDirection, Vector3.up) / 180f;
            sensor.AddObservation(m_LocalTargetAngle);
            
            foreach (AbstractBone bone in m_Bones)
            {
                bone.CollectObservations(sensor);
            }

            foreach (GroundDetector detector in m_Detectors)
            {
                detector.CollectObservations(sensor);
            }

            AddRewards();
        }

        private void AddRewards()
        {
            Vector3 velocity = (m_Position - m_PrevPosition) / m_DeltaTime;
            m_PrevPosition = m_Position;
            
            float speedReward = Vector3.Dot(m_TargetDirection, velocity);
            float facingReward = 1 - Mathf.Abs(m_LocalTargetAngle);
            
            AddReward(speedReward * facingReward);
            
            if (m_DecisionCount % m_StatsInterval == 0)
            {
                m_Stats.Add("Agent/Speed Reward", speedReward);
                m_Stats.Add("Agent/Facing Reward", facingReward);
            }
        }

        private void OnUpdate()
        {
            m_Forward = default;
            m_Position = default;
            
            foreach (Transform t in m_Segments)
            {
                // -up => forward because of rig rotation.
                m_Forward -= t.up;
                m_Position += t.position;
            }
            
            m_Forward = Vector3.ProjectOnPlane(m_Forward / m_Segments.Length, Vector3.up);
            m_Position /= m_Segments.Length;
        }

        /// <inheritdoc />
        // Not really a heuristic, just inspector values we can set
        // on the bones in order to tweak their joints' behaviors.
        public override void Heuristic(in ActionBuffers actionBuffers)
        {
            var actions = actionBuffers.ContinuousActions;
            for (int i = 0, j = 0; i < m_Bones.Length; i++)
            {
                if (m_Bones[i].IsControllable)
                {
                    var heuristic = m_Bones[i].Heuristic();
                    foreach (var t in heuristic)
                    {
                        actions[j++] = t;
                    }
                }
            }
        }

        /// <inheritdoc />
        public override void OnActionReceived(ActionBuffers actionBuffers)
        {
            var actions = actionBuffers.ContinuousActions.Array;

            // Agent cycle starts with
            // step 1 => action right after decision (same physics time step)
            // ...
            // and ends with
            // step 0 => pre-decision
            int step = StepCount % m_DecisionInterval;

            // Handle action values...
            if (step == 0)
            {
                // Last step in agent cycle: buffer actions for next cycle,
                // apply actions as is below.
                Array.Copy(actions, m_PrevActions, m_NumActions);
            }
            else
            {
                // Interpolate from m_PrevActions to crntActions for smooth motion.
                float t = step / (float) m_DecisionInterval;
                for (int i = 1; i < m_NumActions; i++)
                {
                    actions[i] = Mathf.Lerp(m_PrevActions[i], actions[i], t);
                }
            }

            // ...and apply them to bones.
            for (int i = 0, j = 0; i < m_Bones.Length; i++)
            {
                if (m_Bones[i].IsControllable)
                {
                    m_Bones[i].ApplyActions(actions, ref j);
                }
            }
        }

        private void RandomizeTarget()
        {
            m_TargetPosition = new Vector3(
                Random.Range(-25f, 25f), 0, Random.Range(-25f, 25f));
        }
    }
}