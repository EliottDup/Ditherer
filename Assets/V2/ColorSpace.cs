using UnityEngine;

public abstract class ColorSpace
{
    public abstract Vector3 minValues { get; }
    public abstract Vector3 maxValues { get; }

    public abstract Vector3 FromRGB(Color c);

    public Vector3 FromRGB(Vector3 color)
    {
        return FromRGB(new Color(color.x, color.y, color.z));
    }

    public Vector3 Normalize01(Vector3 position)
    {
        Vector3 a = position - minValues;
        Vector3 b = maxValues - minValues;

        return new Vector3(a.x / b.x, a.y / b.y, a.z / b.z);
    }


    public abstract Color ToRGB(Vector3 color);

    public virtual float GetDistance(Vector3 a, Vector3 b)
    {
        return Vector3.Distance(a, b);
    }
}

public class RGBColorSpace : ColorSpace
{
    public override Vector3 minValues => Vector3.zero;

    public override Vector3 maxValues => Vector3.one;

    public override Vector3 FromRGB(Color color)
    {
        return new Vector3(color.r, color.g, color.b);
    }

    public override Color ToRGB(Vector3 color)
    {
        return new Color(color.x, color.y, color.z);
    }
}

public class OKLabColorSpace : ColorSpace
{
    public override Vector3 minValues => new(0, -.5f, -.5f);

    public override Vector3 maxValues => new(1, .5f, .5f);

    public override float GetDistance(Vector3 a, Vector3 b)
    {
        a.z *= 0.5f;
        b.z *= 0.5f;
        return base.GetDistance(a, b);
    }

    public override Vector3 FromRGB(Color color)
    {
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
        return v;
    }

    public override Color ToRGB(Vector3 color)
    {
        Vector3 lab = color;
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
}