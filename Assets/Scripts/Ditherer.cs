using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using System.IO;
using Random = UnityEngine.Random;

public class Ditherer : MonoBehaviour
{
    [SerializeField]
    ComputeShader shader;
    [SerializeField] RenderTexture result;
    ComputeBuffer matrixBuffer;
    ComputeBuffer paletteBuffer;
    public Texture2D tmp;

    float[,] thresholdMatrix;
    public List<Color> palette;

    Vector3 RGB2OKLab(Color color)
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

        return new Vector3(
            0.2104542553f * l_ + 0.7936177850f * m_ - 0.0040720468f * s_,
            1.9779984951f * l_ - 2.4285922051f * m_ + 0.4505937099f * s_,
            0.0259040371f * l_ + 0.7827717662f * m_ - 0.8086757660f * s_
        );
    }

    Color OKLab2RGB(Vector3 lab)
    {
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

    struct ImageColor
    {
        public Vector4 color;
        public int frequency;

        public ImageColor(Vector4 color, int frequency)
        {
            this.color = color;
            this.frequency = frequency;
        }
    }

    public void InitThresholdMatrix(int twoN)
    {
        float[,] m = MakeThresholdMatrix(twoN);
        thresholdMatrix = AddMatrix(m, -0.5f);
        printMatrix(thresholdMatrix);
    }

    public List<Color> InitPalette(Texture2D image, int colorCount, int iterationCount, int seed)
    {
        print("remove duplicate pixels");
        Color[] pixels = image.GetPixels();
        Dictionary<Color, int> colorFrequencies = new Dictionary<Color, int>();

        foreach (Color pixelColor in pixels)
        {
            if (colorFrequencies.ContainsKey(pixelColor))
            {
                colorFrequencies[pixelColor]++;
                continue;
            }
            colorFrequencies[pixelColor] = 1;
        }

        List<ImageColor> allColors = new List<ImageColor>();
        foreach (KeyValuePair<Color, int> entry in colorFrequencies)
        {
            Vector4 c = entry.Key;
            // Vector4 c = RGB2OKLab(entry.Key);
            allColors.Add(new ImageColor(c, entry.Value));
        }

        List<Color> tmpPaletteColors = GenerateKMeansPalette(allColors, colorCount, iterationCount, seed);
        List<Color> paletteColors = new List<Color>();
        for (int i = 0; i < tmpPaletteColors.Count; i++)
        {
            if (!paletteColors.Contains(tmpPaletteColors[i]))
            {
                // paletteColors.Add(OKLab2RGB(tmpPaletteColors[i]));
                paletteColors.Add((tmpPaletteColors[i]));
            }
        }
        print(paletteColors.Count);

        palette = paletteColors;
        return palette;
    }

    List<Color> GenerateKMeansPalette(List<ImageColor> colors, int k, int iterationCount, int seed)
    {
        List<Vector3> means = new List<Vector3>();
        List<List<int>> closestIds = new List<List<int>>();
        Random.InitState(seed);
        for (int i = 0; i < k; i++)
        {
            closestIds.Add(new List<int>());
            means.Add(new Vector3(Random.value, Random.value, Random.value));   //init means
        }

        for (int i = 0; i < colors.Count; i++)
        {
            int id = getClosestID(colors[i].color, means);
            closestIds[id].Add(i);
        }

        for (int i = 0; i < iterationCount; i++)
        {
            List<Vector3> newMeans = new List<Vector3>();
            for (int j = 0; j < k; j++)
            {
                List<ImageColor> closeCols = closestIds[j].Select(c => colors[c]).ToList();
                if (closeCols.Count == 0)
                {
                    means[j] = new Vector3(Random.value, Random.value, Random.value);
                    continue;
                }
                Color avgColor = GetAvgColor(closeCols);

                means[j] = new Vector3(avgColor.r, avgColor.g, avgColor.b);
                closestIds[j] = new List<int>();
            }

            // means = new List<Vector3>(newMeans);
            for (int c = 0; c < colors.Count; c++)
            {
                int id = getClosestID(colors[c].color, means);
                closestIds[id].Add(c);
            }
        }

        List<Color> palette = new List<Color>();
        for (int i = 0; i < k; i++)
        {
            palette.Add(new Color(means[i].x, means[i].y, means[i].z));
        }

        palette.Sort((a, b) => (int)Mathf.Sign(a.grayscale - b.grayscale));

        return palette;
    }

    int getClosestID(Color c, List<Vector3> positions)
    {
        float smallest = 999;
        int smallestID = 0;
        for (int i = 0; i < positions.Count; i++)
        {
            float dist = Vector3.Distance(new Vector3(c.r, c.g, c.b), positions[i]);
            if (dist < smallest)
            {
                smallest = dist;
                smallestID = i;
            }
        }
        return smallestID;
    }

    Color GetAvgColor(List<ImageColor> colors)
    {
        Vector4 avg = new Vector4(0, 0, 0, 0);
        int tot = 0;
        foreach (ImageColor c in colors)
        {
            avg += c.color * c.frequency;
            tot += c.frequency;
        }
        return avg / tot;
    }

    float[,] MakeThresholdMatrix(float twoN)
    {
        if (twoN == 1)
        {
            float[,] grid = {
                {0}
            };
            return MultiplyMatrix(grid, 1 / 4f);
        }
        float n = twoN / 2;
        float[,] m = MultiplyMatrix(MakeThresholdMatrix(n), twoN * twoN);
        float[,] arr = MatrixConcatVertical(MatrixConcatHorizontal(m, AddMatrix(m, 2)), MatrixConcatHorizontal(AddMatrix(m, 3), AddMatrix(m, 1)));
        return MultiplyMatrix(arr, 1 / (twoN * twoN));
    }

    void printMatrix(float[,] m)
    {
        string str = "";
        for (int i = 0; i < m.GetLength(0); i++)
        {
            for (int j = 0; j < m.GetLength(1); j++)
            {
                str += m[i, j].ToString() + " ";
            }
            str += "\n";
        }
        print(str);
    }

    float[,] MultiplyMatrix(float[,] matrix, float val)
    {
        float[,] result = new float[matrix.GetLength(0), matrix.GetLength(1)];

        for (int i = 0; i < matrix.GetLength(0); i++)
        {
            for (int j = 0; j < matrix.GetLength(1); j++)
            {
                result[i, j] = matrix[i, j] * val;
            }
        }
        return result;
    }

    float[,] AddMatrix(float[,] matrix, float addend)
    {
        float[,] result = new float[matrix.GetLength(0), matrix.GetLength(1)];

        for (int i = 0; i < matrix.GetLength(0); i++)
        {
            for (int j = 0; j < matrix.GetLength(1); j++)
            {
                result[i, j] = matrix[i, j] + addend;
            }
        }
        return result;
    }

    float[,] MatrixConcatHorizontal(float[,] m1, float[,] m2)
    {
        int rows1 = m1.GetLength(0);
        int cols1 = m1.GetLength(1);

        int rows2 = m2.GetLength(0);
        int cols2 = m2.GetLength(1);

        //error handling
        if (rows1 != rows2)
        {
            throw new Exception("matrices must have equal row count, m1: " + rows1.ToString() + ", m2: " + rows2.ToString());
        }

        float[,] m3 = new float[rows1, cols1 + cols2];

        for (int i = 0; i < rows1; i++)
        {
            for (int j = 0; j < cols1; j++)
            {
                m3[i, j] = m1[i, j];
            }

            for (int j = 0; j < cols2; j++)
            {
                m3[i, j + cols1] = m2[i, j];
            }
        }
        return m3;
    }

    float[,] MatrixConcatVertical(float[,] m1, float[,] m2)
    {
        int rows1 = m1.GetLength(0);
        int cols1 = m1.GetLength(1);

        int rows2 = m2.GetLength(0);
        int cols2 = m2.GetLength(1);

        //error handling
        if (cols1 != cols2)
        {
            throw new Exception("matrices must have equal row count, m1: " + cols1.ToString() + ", m2: " + cols2.ToString());
        }

        float[,] m3 = new float[rows1 + rows2, cols1];

        for (int i = 0; i < rows1; i++)
        {
            for (int j = 0; j < cols1; j++)
            {
                m3[i, j] = m1[i, j];
            }
        }

        for (int i = 0; i < rows2; i++)
        {
            for (int j = 0; j < cols2; j++)
            {
                m3[i + rows1, j] = m2[i, j];
            }
        }

        return m3;
    }

    public void InitBuffers()
    {
        if (thresholdMatrix == null)
        {
            InitThresholdMatrix(8);
        }

        shader.SetInt("thresholdMatrixSize", thresholdMatrix.GetLength(0));
        shader.SetInt("paletteSize", palette.Count);

        matrixBuffer = new ComputeBuffer(thresholdMatrix.GetLength(0) * thresholdMatrix.GetLength(0), sizeof(float));
        matrixBuffer.SetData(thresholdMatrix);
        shader.SetBuffer(0, "tMatrix", matrixBuffer);

        paletteBuffer = new ComputeBuffer(palette.Count, sizeof(float) * 4);
        paletteBuffer.SetData(palette);
        shader.SetBuffer(0, "palette", paletteBuffer);
    }

    public RenderTexture Dither(Texture2D image, float scalefactor)
    {
        if (palette == null)
        {
            palette = new List<Color>();
            palette.Add(Color.black);
            palette.Add(Color.white);
        }
        if (result != null)
        {
            result.Release();
        }


        int width = (int)(image.width * scalefactor);
        int height = (int)(image.height * scalefactor);
        result = new RenderTexture(width, height, 24);  //create Result and blit intput to it
        print("ReBuilding Rentertexture");
        result.enableRandomWrite = true;
        result.Create();
        shader.SetTexture(0, "result", result);
        image.wrapMode = TextureWrapMode.Clamp;
        RenderTexture.active = result;
        Graphics.Blit(image, result);


        shader.SetInt("width", width);
        shader.SetInt("height", height);

        shader.Dispatch(0, (int)MathF.Ceiling(width / 8f), (int)MathF.Ceiling(height / 8f), 1);
        return result;
    }

    public void DisposeBuffers()
    {
        matrixBuffer?.Dispose();
        paletteBuffer?.Dispose();
        if (result != null)
        {
            result.Release();
        }
    }

    public void OnDisable()
    {
        DisposeBuffers();
    }
}
