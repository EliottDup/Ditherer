using System;
using System.Collections.Generic;
using UnityEngine;
public class PointDrawer : MonoBehaviour
{
    private ParticleSystem _particleSystem;
    private ParticleSystem.Particle[] _particles;


    [Range(0.001f, 0.5f)]
    [SerializeField]
    private float _maxSize = 0.1f;

    [Range(0.001f, 0.05f)]
    [SerializeField]
    private float _minSize;


    public struct Point
    {
        public Color color;
        public Vector3 pos;
        public int size;

        public Point(Color color, Vector3 pos, int size)
        {
            this.color = color;
            this.pos = pos;
            this.size = size;
        }
    }

    private void Awake()
    {
        _particleSystem = GetComponent<ParticleSystem>();
    }

    private void Start()
    {
        // SetPoints(points.ToArray());
    }

    public void SetPoints(Point[] points, ColorSpace colorSpace)
    {
        int count = points.Length;
        print($"setting {count} points");

        _particles = new ParticleSystem.Particle[count];

        for (int i = 0; i < count; i++)
        {
            Point p = points[i];

            float slope = _minSize;//1 / _pointScaling;
            float deltasize = _maxSize;
            float size = (p.size * slope * deltasize) / (deltasize + p.size * slope) + _minSize;
            Color c = p.color;

            _particles[i].position = colorSpace.Normalize01(p.pos);
            _particles[i].startColor = c;
            _particles[i].startSize = size;
            _particles[i].remainingLifetime = Mathf.Infinity;
            _particles[i].startLifetime = Mathf.Infinity;
        }

        _particleSystem.SetParticles(_particles);
    }
    public void SetColors(int[] indices, Color[] colors)
    {
        int particleCount = _particleSystem.particleCount;
        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[particleCount];
        _particleSystem.GetParticles(particles);

        if (particleCount != indices.Length)
        {
            Debug.LogError("color count incorrect!");
            return;
        }

        for (int i = 0; i < particleCount; i++)
        {
            particles[i].startColor = colors[indices[i]];
        }

        _particleSystem.SetParticles(particles);
    }
}
