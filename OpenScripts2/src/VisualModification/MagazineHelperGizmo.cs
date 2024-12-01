using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagazineHelperGizmo : MonoBehaviour
{
    public float GizmoSize;

#if DEBUG
    public void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, GizmoSize);
    }
#endif
}