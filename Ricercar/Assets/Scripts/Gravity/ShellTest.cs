using Ricercar;
using Ricercar.Gravity;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ShellTest : MonoBehaviour
{
    [SerializeField]
    private Attractor m_shell;

    [SerializeField]
    private Rigidbody2D m_rigidbody;

    [SerializeField]
    private float m_mass;

    [SerializeField]
    private bool m_applyGravity;

    private Vector2 m_currentGravity;

    private void FixedUpdate()
    {
        Vector2 otherPosition = m_shell.Position;
        float otherMass = m_shell.Mass;

        Vector2 position = transform.position;

        Vector2 difference = otherPosition - position;

        float sqrMagnitude = difference.sqrMagnitude;
        float magnitude = difference.magnitude;

        Vector2 direction = difference.normalized;

        float forceMagnitude = (GravityField.G * otherMass) / sqrMagnitude;

        if (magnitude < m_shell.Radius)
        {
            forceMagnitude = m_shell.SurfaceGravityForce * magnitude / m_shell.Radius;
        }

        m_currentGravity = direction * forceMagnitude;// * m_mass;

        if (m_applyGravity)
            m_rigidbody.AddForce(m_currentGravity);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Vector2 otherPosition = m_shell.Position;
        float otherMass = m_shell.Mass;

        Vector2 position = transform.position;

        Vector2 difference = otherPosition - position;

        float sqrMagnitude = difference.sqrMagnitude;
        float magnitude = difference.magnitude;

        Vector2 direction = difference.normalized;

        float forceMagnitude = (GravityField.G * otherMass) / sqrMagnitude;

        Color arrowColour = Color.white;

        if (magnitude < m_shell.Radius)
        {
            arrowColour = Color.red;

            float surfaceGravity = m_shell.SurfaceGravityForce;

            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.red;

            Handles.Label(transform.position + Vector3.up * 2f, $"{surfaceGravity} * ({magnitude} / {m_shell.Radius}) = {surfaceGravity * magnitude / m_shell.Radius}", style);

            forceMagnitude = surfaceGravity * magnitude / m_shell.Radius;
        }

        Vector2 gravity = direction * forceMagnitude * m_mass;

        Utils.DrawArrow(transform.position, gravity.normalized, arrowColour, gravity.magnitude * 0.01f, 1f);

        Handles.color = Color.white;
        Handles.Label(transform.position, gravity.ToString());
    }
#endif
}