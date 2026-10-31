using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class KMeansGenerator
{
    public bool Initialized { get; private set; } = false;
    private Vector3[] _means;

    private ColorSpace _colorSpace;

    private Point[] _points;
    private int[] _closestId;

    public struct Point
    {
        public Color color;
        public Vector3 position;
        public int count;
        public Point(Color color, Vector3 position, int count)
        {
            this.color = color;
            this.position = position;
            this.count = count;
        }
    }



    public KMeansGenerator(Texture2D tex, ColorSpace colorSpace)
    {

        _colorSpace = colorSpace;

        _points = GetPoints(tex, _colorSpace);

    }

    public void Init(int k, int seed)
    {
        Initialized = true;
        Random.InitState(seed);
        _means = new Vector3[k];

        for (int i = 0; i < k; i++)
        {
            _means[i] = _points[Random.Range(0, _points.Length)].position;
        }
    }

    public void ExecuteSteps(int stepcount, float jitter = 0)
    {
        for (int i = 0; i < stepcount; i++)
        {
            _closestId = Step(_points, ref _means, _colorSpace, jitter);
        }
    }

    public PointDrawer.Point[] GetPoints()
    {
        return new List<Point>(_points).Select((a) => new PointDrawer.Point(a.color, a.position, a.count)).ToArray();
    }

    public Vector3[] GetMeans()
    {
        return _means;
    }

    public Color[] GetColors()
    {
        if (!Initialized)
        {
            return new Color[0];
        }
        Color[] cols = new Color[_means.Length];

        for (int i = 0; i < _means.Length; i++)
        {
            cols[i] = _colorSpace.ToRGB(_means[i]);
        }

        return cols;
    }

    public int[] GetClosestIDs()
    {
        return _closestId;
    }


    private static Point[] GetPoints(Texture2D tex, ColorSpace colorSpace)
    {
        Color[] pixels = tex.GetPixels();
        Dictionary<Color, int> colors = new();

        foreach (Color c in pixels)
        {
            if (colors.ContainsKey(c))
            {
                colors[c]++;
            }
            else
            {
                colors[c] = 1;
            }
        }

        List<Point> points = new();
        foreach (KeyValuePair<Color, int> kv in colors)
        {
            points.Add(new Point(kv.Key, colorSpace.FromRGB(kv.Key), kv.Value));
        }

        return points.ToArray();
    }

    private static int[] Step(Point[] points, ref Vector3[] means, ColorSpace colorSpace, float maxJitter = 0)
    {
        int k = means.Length;
        Vector3[] prevMeans = new Vector3[k];
        Vector3[] total = new Vector3[k];
        int[] count = new int[k];

        for (int i = 0; i < k; i++)
        {
            prevMeans[i] = means[i];
            means[i] += maxJitter * Random.value * Random.onUnitSphere;
        }

        int n = points.Length;
        int[] closestId = new int[n];

        for (int i = 0; i < n; i++)
        {
            Point p = points[i];

            float smallestDist = Mathf.Infinity;
            Vector3 closestPos = Vector3.zero;
            int closestCount = 0;

            for (int j = 0; j < k; j++)
            {
                float dist = colorSpace.GetDistance(p.position, means[j]);
                if (dist < smallestDist)
                {
                    smallestDist = dist;
                    closestPos = p.position;
                    closestCount = p.count;
                    closestId[i] = j;
                }
            }

            total[closestId[i]] += closestPos * closestCount;
            count[closestId[i]] += closestCount;
        }

        for (int i = 0; i < k; i++)
        {
            if (count[i] > 0)
            {
                means[i] = total[i] / count[i];
            }
            else
            {
                means[i] = prevMeans[i]; // jitter fucked it up, we go back
            }
        }

        return closestId;
    }
}
