using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using Obi;

namespace Ricercar
{
    [CreateAssetMenu(fileName = "Physics Settings", menuName = "Physics Settings")]
    public class PhysicsSettings : ScriptableObject
    {
        [SerializeField]
        [BoxGroup("Prefabs")]
        private Rigidbody2D m_carPrefabRigidbody;

        [SerializeField]
        [BoxGroup("Prefabs")]
        private RopeLauncher m_ropeLauncherPrefab;

        [SerializeField]
        [BoxGroup("Prefabs")]
        private ObiSolver m_obiSolver;

        [SerializeField]
        [BoxGroup("Prefabs")]
        private ObiFixedUpdater m_obiFixedUpdater;

        [SerializeField]
        [MinValue(0f)]
        [BoxGroup("Physics")]
        [OnValueChanged("UpdatePhysics")]
        private float m_physicsTimestep;

        [SerializeField]
        [BoxGroup("Solver")]
        [OnValueChanged("UpdateFixedUpdater")]
        private bool m_substepUnityPhysics;

        [SerializeField]
        [BoxGroup("Solver")]
        [OnValueChanged("UpdateFixedUpdater")]
        private int m_solverSubsteps;
        
        [SerializeField]
        [BoxGroup("Solver")]
        [OnValueChanged("UpdateSolver")]
        private Oni.ConstraintParameters.EvaluationOrder m_distanceConstraintEvaluationOrder;

        [SerializeField]
        [BoxGroup("Solver")]
        [OnValueChanged("UpdateSolver")]
        private int m_distanceContraintIterations;

        [SerializeField]
        [BoxGroup("Car Rigidbody")]
        [OnValueChanged("UpdateCarRigidbody")]
        private float m_carRigidbodyMass;

        [SerializeField]
        [BoxGroup("Car Rigidbody")]
        [OnValueChanged("UpdateCarRigidbody")]
        private float m_carLinearDrag;

        [SerializeField]
        [BoxGroup("Car Rigidbody")]
        [OnValueChanged("UpdateCarRigidbody")]
        private float m_carAngularDrag;

        [SerializeField]
        [BoxGroup("Rope Launcher Settings")]
        [OnValueChanged("UpdateRopeLauncher")]
        private bool m_tightenAfterLaunch = false;

        [SerializeField]
        [MinValue(0f)]
        [ShowIf("m_tightenAfterLaunch")]
        [BoxGroup("Rope Launcher Settings")]
        [OnValueChanged("UpdateRopeLauncher")]
        private float m_ropeSizeChangeSpeed = 1f;

        [SerializeField]
        [MinValue(0f)]
        [ShowIf("m_tightenAfterLaunch")]
        [BoxGroup("Rope Launcher Settings")]
        [OnValueChanged("UpdateRopeLauncher")]
        private float m_ropeTightenTarget = 1f;

        [SerializeField]
        [MinValue(0f)]
        [BoxGroup("Rope Launcher Settings")]
        [OnValueChanged("UpdateRopeLauncher")]
        private float m_ropeResolution = 1f;

        [SerializeField]
        [MinValue(0f)]
        [BoxGroup("Rope Launcher Settings")]
        [OnValueChanged("UpdateRopeLauncher")]
        private float m_ropeThickness = 1f;

        //#region Events

        //private void OnEnable()
        //{
        //    SynchronizeValues();
        //}

        //private void Awake()
        //{
        //    SynchronizeValues();
        //}

        //[Button("Synchronize Values")]
        //private void SynchronizeValues()
        //{
        //    Debug.Log("Synchronizing...");

        //    m_physicsTimestep = Time.fixedDeltaTime;

        //    if (m_obiFixedUpdater != null)
        //    {
        //        m_substepUnityPhysics = m_obiFixedUpdater.substepUnityPhysics;
        //        m_solverSubsteps = m_obiFixedUpdater.substeps;
        //    }

        //    if (m_obiSolver != null)
        //    {
        //        m_distanceContraintIterations = m_obiSolver.distanceConstraintParameters.iterations;
        //        m_distanceConstraintEvaluationOrder = m_obiSolver.distanceConstraintParameters.evaluationOrder;
        //    }

        //    if (m_carPrefabRigidbody != null)
        //    {
        //        m_carRigidbodyMass = m_carPrefabRigidbody.mass;
        //        m_carLinearDrag = m_carPrefabRigidbody.drag;
        //        m_carAngularDrag = m_carPrefabRigidbody.angularDrag;
        //    }

        //    if (m_ropeLauncherPrefab != null)
        //    {
        //        m_tightenAfterLaunch = m_ropeLauncherPrefab.TightenAfterLaunch;
        //        m_ropeSizeChangeSpeed = m_ropeLauncherPrefab.RopeSizeChangeSpeed;
        //        m_ropeTightenTarget = m_ropeLauncherPrefab.RopeTightenTarget;
        //        m_ropeResolution = m_ropeLauncherPrefab.RopeResolution;
        //        m_ropeThickness = m_ropeLauncherPrefab.RopeThickness;
        //    }
        //}

        //private void UpdatePhysics()
        //{
        //    Time.fixedDeltaTime = m_physicsTimestep;
        //}

        //private void UpdateFixedUpdater()
        //{
        //    if (m_obiFixedUpdater != null)
        //    {
        //        m_obiFixedUpdater.substepUnityPhysics = m_substepUnityPhysics;
        //        m_obiFixedUpdater.substeps = m_solverSubsteps;
        //    }
        //}

        //private void UpdateSolver()
        //{
        //    if (m_obiSolver != null)
        //    {
        //        m_obiSolver.distanceConstraintParameters.iterations = m_distanceContraintIterations;
        //        m_obiSolver.distanceConstraintParameters.evaluationOrder = m_distanceConstraintEvaluationOrder;
        //    }
        //}

        //private void UpdateCarRigidbody()
        //{
        //    if (m_carPrefabRigidbody != null)
        //    {
        //        m_carPrefabRigidbody.mass = m_carRigidbodyMass;
        //        m_carPrefabRigidbody.drag = m_carLinearDrag;
        //        m_carPrefabRigidbody.angularDrag = m_carAngularDrag;
        //    }
        //}

        //private void UpdateRopeLauncher()
        //{
        //    if (m_ropeLauncherPrefab != null)
        //    {
        //        m_ropeLauncherPrefab.TightenAfterLaunch = m_tightenAfterLaunch;
        //        m_ropeLauncherPrefab.RopeSizeChangeSpeed = m_ropeSizeChangeSpeed;
        //        m_ropeLauncherPrefab.RopeTightenTarget = m_ropeTightenTarget;
        //        m_ropeLauncherPrefab.RopeResolution = m_ropeResolution;
        //        m_ropeLauncherPrefab.RopeThickness = m_ropeThickness;
        //    }
        //}

        //#endregion
    }
}