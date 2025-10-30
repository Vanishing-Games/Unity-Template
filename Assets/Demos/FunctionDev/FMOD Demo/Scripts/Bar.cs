using System.Collections.Generic;
using UnityEngine;

public class Bar : MonoBehaviour
{
    public static readonly List<Bar> Instances = new List<Bar>();

    [Tooltip("Max distance at which this bar contributes to the Walla parameter.")]
    public float wallaMaxDistance = 20f;

    private void OnEnable()
    {
        if (!Instances.Contains(this))
        {
            Instances.Add(this);
        }
    }

    private void OnDisable()
    {
        Instances.Remove(this);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.65f, 0f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(0f, wallaMaxDistance));
    }
}
