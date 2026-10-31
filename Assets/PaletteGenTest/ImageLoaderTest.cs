using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SFB;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using Random = UnityEngine.Random;

public class ImageLoaderTest : MonoBehaviour
{
    public string imagePath = "";
    public Texture2D tex;
    public PointDrawer pd;
    [SerializeField] int _k = 16, _iterations = 1;

    [SerializeField] bool _init, _step;

    [Range(0, 0.2f)]
    [SerializeField]
    float _jitter = 0.05f;

    [SerializeField] GameObject _meanObject;
    List<Transform> _meanObjects;
    List<Vector3> _means;
    PointDrawer.Point[] _points;
    void Start()
    {
        if (imagePath == "")
        {
            imagePath = StandaloneFileBrowser.OpenFilePanel("Select an image", "", new ExtensionFilter[] { new("Image Files", "png", "jpg", "bmp") }, false)[0];
        }

        print("loading points");
        tex = LoadImage(imagePath);

        _points = GetColors(tex);

        DateTime t = DateTime.Now;
        pd.SetPoints(_points, new OKLabColorSpace());
        int elapsed = DateTime.Now.Subtract(t).Milliseconds;
        print($"finished loading, took {elapsed}ms");
        _meanObjects = new();
    }

    void Update()
    {
        if (_init)
        {
            if (_points.Length < _k)
            {
                print("less points than K");
                return;
            }
            foreach (Transform t in _meanObjects)
            {
                Destroy(t.gameObject);
            }
            _meanObjects.Clear();
            _init = false;
            _means = new List<Vector3>();

            for (int i = 0; i < _k; i++)
            {
                _means.Add(_points[UnityEngine.Random.Range(0, _points.Length)].pos);
                _meanObjects.Add(Instantiate(_meanObject, new OKLabColorSpace().Normalize01(_means[i]), Quaternion.identity).transform);
            }
        }
        if (_step)
        {
            _step = false;
            ExecuteStep();
        }
    }

    void ExecuteStep()
    {
        ColorSpace colspace = new OKLabColorSpace();
        int[] closest = new int[0];
        for (int i = 0; i < _iterations; i++)
        {
            closest = Step(_points, _means, 2 * _jitter * ((_iterations - i) / _iterations));
        }
        if (closest.Length == 0) return;
        pd.SetColors(closest, _means.Select((a) => { return OKLab2RGB(a); }).ToArray());
        for (int i = 0; i < _means.Count; i++)
        {
            _meanObjects[i].position = colspace.Normalize01(_means[i]);
        }
    }

    int[] Step(PointDrawer.Point[] points, List<Vector3> means, float jitter)
    {
        ColorSpace colspace = new OKLabColorSpace();

        int[] closest = new int[points.Length];
        Vector3[] newPos = new Vector3[means.Count];
        int[] count = new int[means.Count];

        Vector3[] prevMeans = new Vector3[means.Count];

        for (int i = 0; i < means.Count; i++)
        {
            prevMeans[i] = means[i];
            means[i] += Random.onUnitSphere * Random.Range(0, jitter);
        }

        for (int i = 0; i < points.Length; i++)
        {
            PointDrawer.Point p = points[i];
            float smallestDist = 999;
            Vector3 closestPos = means[0];
            for (int j = 0; j < means.Count; j++)
            {
                float dist = colspace.GetDistance(p.pos, means[j]);
                if (dist < smallestDist)
                {
                    closest[i] = j;
                    smallestDist = dist;
                    closestPos = p.pos;
                }
            }

            newPos[closest[i]] += closestPos * points[i].size;
            count[closest[i]] += points[i].size;
        }

        for (int i = 0; i < means.Count; i++)
        {
            if (count[i] > 0)
            {
                means[i] = newPos[i] / count[i];
            }
            else
            {
                means[i] = prevMeans[i];
            }
        }

        return closest;
    }

    Vector3 RGB2OKLab(Color color)
    {
        // return new Vector3(color.r, color.g, color.b);
        Color c = color.linear;

        float l = 0.4122214708f * c.r
                + 0.5363325363f * c.g
                + 0.0514459929f * c.b;

        float m = 0.2119034982f * c.r
                + 0.6806995451f * c.g
                + 0.1073969566f * c.b;

        float s = 0.0883024619f * c.r
                + 0.2817188376f * c.g
                + 0.6299787005f * c.b;

        float l_ = Mathf.Pow(l, 1f / 3f);
        float m_ = Mathf.Pow(m, 1f / 3f);
        float s_ = Mathf.Pow(s, 1f / 3f);

        Vector3 v = new Vector3(
            0.2104542553f * l_ + 0.7936177850f * m_ - 0.0040720468f * s_,
            1.9779984951f * l_ - 2.4285922051f * m_ + 0.4505937099f * s_,
            0.0259040371f * l_ + 0.7827717662f * m_ - 0.8086757660f * s_
        );

        // v.y *= 2;
        // v.z *= 2;
        return v;
    }

    Color OKLab2RGB(Vector3 lab)
    {
        // return new Color(lab.x, lab.y, lab.z);
        // lab.y /= 2;
        // lab.z /= 2;

        float l_ = lab.x + 0.3963377774f * lab.y + 0.2158037573f * lab.z;
        float m_ = lab.x - 0.1055613458f * lab.y - 0.0638541728f * lab.z;
        float s_ = lab.x - 0.0894841775f * lab.y - 1.2914855480f * lab.z;

        float l = l_ * l_ * l_;
        float m = m_ * m_ * m_;
        float s = s_ * s_ * s_;

        Color linear = new Color(
             4.0767416621f * l - 3.3077115913f * m + 0.2309699292f * s,
            -1.2684380046f * l + 2.6097574011f * m - 0.3413193965f * s,
            -0.0041960863f * l - 0.7034186147f * m + 1.7076147010f * s
        );

        return linear.gamma;
    }

    private PointDrawer.Point[] GetColors(Texture2D tex)
    {
        Color[] pixels = tex.GetPixels();

        Dictionary<Color, int> counts = new();

        foreach (Color c in pixels)
        {
            if (counts.ContainsKey(c))
            {
                counts[c]++;
            }
            else
            {
                counts.Add(c, 1);
            }
        }

        List<PointDrawer.Point> points = new();
        foreach (KeyValuePair<Color, int> kv in counts)
        {
            Color c = kv.Key;
            int count = kv.Value;
            Vector3 pos = RGB2OKLab(c);//new Vector3(c.r, c.g, c.b);
            // pos.y *= 2;
            // pos.z *= 2;
            // pos += new Vector3(0, 0.5f, 0.5f);
            points.Add(new PointDrawer.Point(c, pos, count));
        }

        return points.ToArray();
    }

    Texture2D LoadImage(string path)
    {
        Texture2D tex = null;
        byte[] fileData;

        if (File.Exists(path))
        {
            fileData = File.ReadAllBytes(path);
            tex = new Texture2D(2, 2);
            if (!ImageConversion.LoadImage(tex, fileData))
            {
                print("Failed to load!");
            }
        }
        return tex;
    }
}
