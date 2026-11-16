using UnityEngine;

public sealed class ChairTrafficLoop : MonoBehaviour
{
    [SerializeField] private Rigidbody[] cars;
    [SerializeField] private Vector3 center;
    [SerializeField, Min(0f)] private float straightLength = 19f;
    [SerializeField, Min(0.1f)] private float radius = 6.35f;
    [SerializeField] private Vector2 speedRange = new Vector2(2.6f, 5.4f);

    private const float MinimumGap = 2.5f;
    private float[] distances;
    private float[] advances;
    private float[] speeds;
    private float[] targetSpeeds;
    private float[] speedTimers;

    private void Awake()
    {
        if (cars.Length == 0 || LoopLength <= cars.Length * MinimumGap)
        {
            enabled = false;
            return;
        }

        distances = new float[cars.Length];
        advances = new float[cars.Length];
        speeds = new float[cars.Length];
        targetSpeeds = new float[cars.Length];
        speedTimers = new float[cars.Length];

        float totalWeight = 0f;
        for (int i = 0; i < cars.Length; i++)
        {
            advances[i] = Random.Range(0.2f, 1f);
            totalWeight += advances[i];
        }

        float distance = Random.Range(0f, LoopLength);
        float spareRoom = LoopLength - cars.Length * MinimumGap;
        for (int i = 0; i < cars.Length; i++)
        {
            distances[i] = Mathf.Repeat(distance, LoopLength);
            distance += MinimumGap + spareRoom * advances[i] / totalWeight;
            PickSpeed(i);
            speeds[i] = targetSpeeds[i];
        }
        PlaceCars(false);
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        for (int i = 0; i < cars.Length; i++)
        {
            speedTimers[i] -= dt;
            if (speedTimers[i] <= 0f)
                PickSpeed(i);

            speeds[i] = Mathf.MoveTowards(speeds[i], targetSpeeds[i], 1.3f * dt);
            float gap = cars.Length == 1 ? LoopLength :
                Mathf.Repeat(distances[(i + 1) % cars.Length] - distances[i], LoopLength);
            advances[i] = Mathf.Min(speeds[i] * dt, Mathf.Max(0f, gap - MinimumGap));
        }

        for (int i = 0; i < cars.Length; i++)
            distances[i] = Mathf.Repeat(distances[i] + advances[i], LoopLength);

        PlaceCars(true);
    }

    private void PickSpeed(int index)
    {
        targetSpeeds[index] = Random.Range(speedRange.x, speedRange.y);
        speedTimers[index] = Random.Range(1.8f, 5f);
    }

    private float LoopLength => 2f * (straightLength + Mathf.PI * radius);

    private void PlaceCars(bool moving)
    {
        for (int i = 0; i < cars.Length; i++)
        {
            GetPose(distances[i], out Vector3 point, out Vector3 forward);
            Vector3 position = transform.TransformPoint(center + point);
            Quaternion rotation = Quaternion.LookRotation(transform.TransformDirection(forward));
            if (moving)
            {
                cars[i].MovePosition(position);
                cars[i].MoveRotation(rotation);
            }
            else
            {
                cars[i].position = position;
                cars[i].rotation = rotation;
            }
        }
    }

    private void GetPose(float position, out Vector3 point, out Vector3 forward)
    {
        float arcLength = Mathf.PI * radius;
        float halfLength = straightLength * 0.5f;
        bool returnSide = position >= straightLength + arcLength;
        if (returnSide)
            position -= straightLength + arcLength;

        if (position < straightLength)
        {
            point = new Vector3(position - halfLength, 0f, -radius);
            forward = Vector3.right;
        }
        else
        {
            float angle = (position - straightLength) / radius;
            point = new Vector3(halfLength + radius * Mathf.Sin(angle), 0f,
                -radius * Mathf.Cos(angle));
            forward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        }

        if (returnSide)
        {
            point = -point;
            forward = -forward;
        }
    }
}
