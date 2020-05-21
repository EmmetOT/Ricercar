using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace Ricercar.Gravity
{
    public class GravityMeter : MonoBehaviour
    {
        [SerializeField]
        private GameObject m_arrow;

        //private void Update()
        //{
        //    Vector2 gravity = GravityField.GetGravity(transform.position);

        //    m_arrow.transform.localRotation = Quaternion.LookRotation(Vector3.forward, gravity.normalized);
        //    m_arrow.transform.localScale = Mathf.Min(gravity.magnitude * 0.05f, 8f) * Vector3.one;
        //}
    }
}