using Obi;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using Ricercar.Gravity;

namespace Ricercar
{
    [System.Serializable]
    public class RopeLauncher : MonoBehaviour
    {
        private Transform m_transform;

        [SerializeField]
        [ReadOnly]
        private Color m_currentColour;

        [SerializeField]
        private SpriteRenderer m_aimSpriteRenderer;
        public SpriteRenderer AimSpriteRenderer => m_aimSpriteRenderer;

        [SerializeField]
        private ObiRope m_rope;

        [SerializeField]
        private ObiCollider2D m_ropeSourceObiCollider;
        public ObiCollider2D RopeSourceObiCollider => m_ropeSourceObiCollider;

        public Collider2D RopeSourceCollider => RopeSourceObiCollider.SourceCollider;

        [SerializeField]
        private ObiRopeLineRenderer m_obiRopeLineRenderer;
        public ObiRopeLineRenderer ObiRopeLineRenderer => m_obiRopeLineRenderer;

        [SerializeField]
        private MeshRenderer m_obiRopeMeshRenderer;
        public MeshRenderer ObiRopeMeshRenderer => m_obiRopeMeshRenderer;

        [SerializeField]
        private ObiRopeCursor m_cursor;
        public ObiRopeCursor Cursor => m_cursor;

        [SerializeField]
        private bool m_tightenAfterLaunch = false;
        public bool TightenAfterLaunch { get => m_tightenAfterLaunch; set => m_tightenAfterLaunch = value; }

        [SerializeField]
        [MinValue(0f)]
        [ShowIf("m_tightenAfterLaunch")]
        private float m_ropeSizeChangeSpeed = 1f;
        public float RopeSizeChangeSpeed { get => m_ropeSizeChangeSpeed; set => m_ropeSizeChangeSpeed = value; }

        [SerializeField]
        [MinValue(0f)]
        [ShowIf("m_tightenAfterLaunch")]
        private float m_ropeTightenTarget = 1f;
        public float RopeTightenTarget { get => m_ropeTightenTarget; set => m_ropeTightenTarget = value; }

        [SerializeField]
        [MinValue(0f)]
        private float m_ropeResolution = 1f;
        public float RopeResolution { get => m_ropeResolution; set => SetRopeResolution(value); }

        [SerializeField]
        [MinValue(0f)]
        private float m_ropeThickness = 1f;
        public float RopeThickness { get => m_ropeThickness; set => SetRopeThickness(value); }

        [SerializeField]
        [BoxGroup("Automatic Rotation")]
        private bool m_rotateToAttachPoint = true;

        [SerializeField]
        [ShowIf("m_rotateToAttachPoint")]
        [MinValue(0f)]
        [BoxGroup("Automatic Rotation")]
        private float m_minDistanceBeforeLerp;

        [SerializeField]
        [ShowIf("m_rotateToAttachPoint")]
        [Range(0f, 1f)]
        [BoxGroup("Automatic Rotation")]
        [Tooltip("If rotating the launch source, choose how much to prefer to rotate towards the rope's immediate direction at the launch source, versus the total direction.")]
        private float m_sourceDirectionToAttachDirectionLerp = 0.666f;

        public float Angle { get; private set; }

        private IAttractor m_attractor;

        private ObiSolver m_solver;

        private ObiRopeBlueprint m_ropeBlueprint;

        private Vector3 m_attachedPoint;
        public Vector3 AttachedPoint => m_attachedPoint;

        private ObiColliderBase m_attachedCollider;

        private float m_distanceFromCentre = 0f;

        protected ContactFilter2D m_raycastFilter;
        protected readonly RaycastHit2D[] m_raycastHits = new RaycastHit2D[1];
        protected int m_currentHitCount = 0;

        public bool IsActive => m_rope.isLoaded;

        private float m_desiredRopeLength = -1f;

        private Material m_material;

        public Vector3 DirectionAtSource
        {
            get
            {
                if (!IsActive)
                    return default;

                return (m_solver.positions[m_rope.solverIndices[2]] - m_solver.positions[m_rope.solverIndices[0]]).normalized;
            }

        }

        public Vector3 DirectionToAttachPoint
        {
            get
            {
                if (!IsActive)
                    return default;

                return  (m_attachedPoint - (Vector3)m_attractor.Position).normalized;
            }
        }

        public Vector3 GetSourcePosition()
        {
            return m_transform.position + m_transform.up * m_distanceFromCentre;
        }

        public void Initialize(IAttractor attractor, ObiSolver solver, float distanceFromCentre, ContactFilter2D contactFilter, Material material)
        {
            m_transform = transform;
            m_solver = solver;

            DetachRope();

            m_attractor = attractor;
            m_distanceFromCentre = distanceFromCentre;
            m_raycastFilter = contactFilter;

            gameObject.SetActive(true);

            m_material = new Material(material);
            m_obiRopeMeshRenderer.material = m_material;

            m_ropeBlueprint = ScriptableObject.CreateInstance<ObiRopeBlueprint>();
            m_ropeBlueprint.resolution = m_ropeResolution;
            m_ropeBlueprint.thickness = m_ropeThickness;

            RopeSourceCollider.offset = Vector3.zero;

            m_aimSpriteRenderer.transform.localPosition = Vector3.zero;

            SetRotation(0f);
        }

        private void OnDestroy()
        {
            Destroy(m_material);
        }

        public void ManualUpdate(float deltaTime)
        {
            if (!IsActive)
                return;

            if (m_attachedPoint != default && m_rotateToAttachPoint)
            {
                float distToAttachPoint = Vector3.Distance(m_attachedPoint, m_attractor.Position);

                Vector3 dir;
                if (distToAttachPoint < m_minDistanceBeforeLerp)
                {
                    dir = Vector3.Lerp(DirectionAtSource, DirectionToAttachPoint, m_sourceDirectionToAttachDirectionLerp);
                    Debug.DrawLine(m_attractor.Position, (Vector3)m_attractor.Position + dir * 5f, Color.yellow, Time.deltaTime);
                }
                else
                {
                    dir = DirectionAtSource;

                    Debug.DrawLine(m_attractor.Position, (Vector3)m_attractor.Position + dir * 5f, Color.green, Time.deltaTime);
                }

                SetRotation(-Vector2.SignedAngle(Vector2.up, dir));
            }

            if (m_tightenAfterLaunch)
                m_cursor.ChangeLength(Mathf.Lerp(m_rope.restLength, m_desiredRopeLength, m_rope.restLength * m_ropeSizeChangeSpeed * deltaTime));
        }

        public bool CanLaunch(out Vector3 pos, out Collider2D collider)
        {
            Vector3 source = m_transform.position + m_transform.up * m_distanceFromCentre;
            Vector3 direction = m_transform.up;

            if (Raycast(source, direction, out pos, out collider))
                return true;

            return false;
        }

        private bool Raycast(Vector3 origin, Vector3 direction, out Vector3 pos, out Collider2D collider)
        {
            pos = default;
            collider = null;

            m_currentHitCount = Mathf.Min(1, Physics2D.Raycast(origin, direction, m_raycastFilter, m_raycastHits));

            if (m_currentHitCount <= 0)
                return false;

            pos = m_raycastHits[0].point;
            collider = m_raycastHits[0].collider;

            return true;
        }

        public void SetColour(Color col)
        {
            if (m_currentColour == col)
                return;

            m_currentColour = col;

            m_material.color = m_currentColour;
            m_aimSpriteRenderer.color = m_currentColour;
        }

        public void SetRotation(float angle)
        {
            if (m_transform == null)
                m_transform = transform;

            Angle = angle;
            Vector3 eulerAngles = Vector3.back * Angle;

            m_transform.localPosition = Utils.RotateAround(Vector3.up * m_distanceFromCentre, Vector3.zero, eulerAngles);
            m_transform.localEulerAngles = eulerAngles;
        }

        public void SetRopeThickness(float thickness)
        {
            m_ropeThickness = thickness;

            if (m_ropeBlueprint != null && m_rope != null && m_rope.isLoaded)
            {
                m_ropeBlueprint.thickness = m_ropeThickness;
                m_rope.ropeBlueprint = m_ropeBlueprint;
            }
        }

        public void SetRopeResolution(float resolution)
        {
            m_ropeResolution = resolution;

            if (m_ropeBlueprint != null && m_rope != null && m_rope.isLoaded)
            {
                m_ropeBlueprint.resolution = m_ropeResolution;
                m_rope.ropeBlueprint = m_ropeBlueprint;
            }
        }

        public void DetachRope()
        {
            Angle = 0f;
            m_desiredRopeLength = -1f;
            m_currentHitCount = 0;
            m_rope.ropeBlueprint = null;
            m_rope.enabled = false;
            m_obiRopeLineRenderer.enabled = false;
            m_attachedPoint = default;
            m_attachedCollider = null;
        }

        [SerializeField]
        private bool m_debugBreakOnLaunch = true;

        public IEnumerator Cr_Launch()
        {
            if (!CanLaunch(out Vector3 pos, out Collider2D collider))
                yield break;

            m_rope.enabled = true;

            yield return Cr_GeneratePath(GetSourcePosition(), pos);

            Pin(pos, collider.GetComponent<ObiColliderBase>());

            // Set the blueprint (this adds particles/constraints to the solver and starts simulating them).
            m_rope.ropeBlueprint = m_ropeBlueprint;
            m_obiRopeLineRenderer.enabled = true;

            m_desiredRopeLength = m_rope.restLength * (m_tightenAfterLaunch ? m_ropeTightenTarget : 1f);

            //m_sourceRigidbody.velocity = Vector2.zero;
            //m_sourceRigidbody.angularVelocity = 0f;

            if (m_debugBreakOnLaunch)
                Debug.Break();

            //yield return Cr_ReduceVelocity(200);

            //m_sourceRigidbody.velocity = Vector2.zero;
            //m_sourceRigidbody.angularVelocity = 0f;

        }

        [Button("Reduce Velocity")]
        private void ReduceVelocityButton()
        {
            StartCoroutine(Cr_ReduceVelocity(30));
        }

        private IEnumerator Cr_ReduceVelocity(int frames = 1)
        {
            for (int i = 0; i < frames; i++)
            {
                yield return null;

                ReduceVelocity();
            }
        }

        private void ReduceVelocity()
        {
            if (m_rope == null || m_rope.solverIndices == null)
                return;

            foreach (int index in m_rope.solverIndices)
            {
                m_solver.velocities[index] = Vector3.zero;
                m_solver.angularVelocities[index] = Vector3.zero;
            }
        }

        private IEnumerator Cr_GeneratePath(Vector3 from, Vector3 to)
        {
            from = m_transform.InverseTransformPoint(from);
            to = m_transform.InverseTransformPoint(to);

            // Procedurally generate the rope path (a simple straight line):
            m_ropeBlueprint.path.Clear();
            m_ropeBlueprint.path.AddControlPoint(from, -to.normalized, to.normalized, Vector3.up, 0.1f, 0.1f, 1, 1, Color.white, "Rope Start");
            m_ropeBlueprint.path.AddControlPoint(to, -to.normalized, to.normalized, Vector3.up, 0.1f, 0.1f, 1, 1, Color.white, "Rope End");
            m_ropeBlueprint.path.FlushEvents();

            // Generate the particle representation of the rope (wait until it has finished):
            yield return m_ropeBlueprint.Generate();
        }

        private void Pin(Vector3 attachPoint, ObiColliderBase hitCollider)
        {
            m_attachedPoint = attachPoint;
            m_attachedCollider = hitCollider;

            //ObiColliderBase collider = transform.parent.parent.GetComponent<ObiColliderBase>();

            // Pin both ends of the rope (this enables two-way interaction between character and rope):
            ObiConstraints<ObiPinConstraintsBatch> pinConstraints = m_ropeBlueprint.GetConstraintsByType(Oni.ConstraintType.Pin) as ObiConstraints<ObiPinConstraintsBatch>;
            ObiPinConstraintsBatch batch = pinConstraints.GetFirstBatch();

            batch.AddConstraint(0, m_ropeSourceObiCollider, Vector3.zero, Quaternion.identity);
            batch.AddConstraint(m_ropeBlueprint.activeParticleCount - 1, m_attachedCollider, m_attachedCollider.transform.InverseTransformPoint(m_attachedPoint), Quaternion.identity);

            batch.activeConstraintCount = 2;
        }

        public void AddToRopeLength(float delta)
        {
            SetRopeLength(m_rope.restLength + delta);
        }

        public void SetRopeLength(float newLength)
        {
            if (!m_rope.isLoaded)
                return;

            m_cursor.ChangeLength(newLength);
        }
    }
}