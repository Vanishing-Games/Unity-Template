using System.Collections.Generic;
using UnityEngine;

public class Car : MonoBehaviour
{
    public static readonly List<Car> ActiveCars = new List<Car>();

    [Header("Movement")]
    public Vector3 moveDirection = Vector3.forward;
    public float moveSpeed = 7.5f;
    public float despawnDistance = 100f;

    private Vector3 spawnPosition;

    private void OnEnable()
    {
        if (!ActiveCars.Contains(this))
        {
            ActiveCars.Add(this);
        }

        spawnPosition = transform.position;
    }

    private void OnDisable()
    {
        ActiveCars.Remove(this);
    }

    private void Update()
    {
        Vector3 directionPlanar = new Vector3(moveDirection.x, 0f, moveDirection.z).normalized;
        transform.position += directionPlanar * moveSpeed * Time.deltaTime;

        if (despawnDistance > 0f)
        {
            float traveled = Vector3.Distance(spawnPosition, transform.position);
            if (traveled >= despawnDistance)
            {
                Destroy(gameObject);
            }
        }
    }

    public static int CountNearby(Vector3 position, float radius)
    {
        if (radius <= 0f) return 0;

        int count = 0;
        float radiusSqr = radius * radius;
        for (int i = 0; i < ActiveCars.Count; i++)
        {
            Car car = ActiveCars[i];
            if (car == null) continue;
            Vector3 to = car.transform.position - position;
            to.y = 0f;
            if (to.sqrMagnitude <= radiusSqr)
            {
                count++;
            }
        }
        return count;
    }
}
