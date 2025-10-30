using System.Collections.Generic;
using UnityEngine;

public class CarManager : MonoBehaviour
{
    [Header("Spawning")]
    [SerializeField] private GameObject carPrefab;
    [SerializeField] private int targetCarCount = 20;
    [SerializeField] private float spawnIntervalSeconds = 0.2f;

    [Header("Area (local to this GameObject)")]
    [SerializeField] private Vector3 areaCenter = Vector3.zero;
    [SerializeField] private Vector3 areaSize = new Vector3(60f, 0f, 60f);

    [Header("Car Settings")]
    [SerializeField] private Vector2 speedRange = new Vector2(5f, 10f);
    [SerializeField] private float carDespawnDistance = 100f;

    private float spawnTimer;

    private void Update()
    {
        if (carPrefab == null) return;

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnIntervalSeconds)
        {
            spawnTimer = 0f;

            // Keep spawning until we reach target count
            if (Car.ActiveCars.Count < targetCarCount)
            {
                SpawnCarOnPerimeter();
            }
        }
    }

    private void SpawnCarOnPerimeter()
    {
        Vector3 worldCenter = transform.TransformPoint(areaCenter);
        Vector3 half = areaSize * 0.5f;

        int side = Random.Range(0, 4); // 0: left, 1: right, 2: bottom, 3: top
        Vector3 localPos;
        Vector3 dir;

        switch (side)
        {
            case 0: // left to right
                localPos = new Vector3(-half.x, 0f, Random.Range(-half.z, half.z));
                dir = Vector3.right;
                break;
            case 1: // right to left
                localPos = new Vector3(half.x, 0f, Random.Range(-half.z, half.z));
                dir = Vector3.left;
                break;
            case 2: // bottom to top (south to north)
                localPos = new Vector3(Random.Range(-half.x, half.x), 0f, -half.z);
                dir = Vector3.forward;
                break;
            default: // top to bottom (north to south)
                localPos = new Vector3(Random.Range(-half.x, half.x), 0f, half.z);
                dir = Vector3.back;
                break;
        }

        Vector3 spawnPos = worldCenter + transform.TransformDirection(localPos);
        Quaternion rotation = Quaternion.LookRotation(transform.TransformDirection(dir), Vector3.up);

        GameObject carObj = Instantiate(carPrefab, spawnPos, rotation);
        Car car = carObj.GetComponent<Car>();
        if (car == null)
        {
            car = carObj.AddComponent<Car>();
        }

        car.moveDirection = transform.TransformDirection(dir).normalized;
        car.moveSpeed = Random.Range(speedRange.x, speedRange.y);
        car.despawnDistance = carDespawnDistance;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 worldCenter = Application.isPlaying ? transform.TransformPoint(areaCenter) : transform.position + areaCenter;
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.DrawWireCube(worldCenter, new Vector3(areaSize.x, 0.01f, areaSize.z));
    }
}
