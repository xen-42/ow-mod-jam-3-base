using UnityEngine;

namespace ModJam3;

internal class Oscillator : MonoBehaviour
{
    public float amplitude = 0.25f;
    public float frequency = 2f;

    public Vector3 direction = new Vector3(0, 0, 1);

    private Vector3 _originalPosition;

    public void Start()
    {
        _originalPosition = transform.localPosition;
    }

    public void Update()
    {
        this.transform.localPosition = _originalPosition + amplitude * Mathf.Sin(2f * Mathf.PI * Time.time / frequency) * direction;
    }
}
