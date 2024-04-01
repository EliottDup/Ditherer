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
using UnityEngine.Rendering;

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

    struct ImageColor
    {
        public Color color;
        public int frequency;

        public ImageColor(Color color, int frequency)
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

    struct VoidAndCluster
    {
        public Vector2Int voidPos;
        public Vector2Int clusterPos;
        public VoidAndCluster(Vector2Int voidPos, Vector2Int clusterPos)
        {
            this.voidPos = voidPos;
            this.clusterPos = clusterPos;
        }

    }

    bool[,] DeepCopy(bool[,] original)
    {
        int rows = original.GetLength(0);
        int cols = original.GetLength(1);

        // Create a new array with the same dimensions as the original
        bool[,] copy = new bool[rows, cols];

        // Iterate through each element of the original array
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                // Copy the value of each element to the corresponding position in the new array
                copy[i, j] = original[i, j];
            }
        }

        return copy;
    }

    public void InitBlueNoise(int size, float fill, float sigma)
    {
        int MN = size * size;

        bool[,] initialBinaryMatrix = fillBoolMatrix(size, fill);
        initialBinaryMatrix = dispersebinaryMatrix(initialBinaryMatrix, sigma);

        float[,] ditherArray = new float[size, size];

        bool[,] BinaryPattern = new bool[size, size]; //Init Phase 1
        float[,] tmp = new float[size, size];
        int ones = 0;
        for (int i = 0; i < initialBinaryMatrix.GetLength(0); i++)
        {
            for (int j = 0; j < initialBinaryMatrix.GetLength(1); j++)
            {
                if (initialBinaryMatrix[i, j])
                {
                    ones++;
                    BinaryPattern[i, j] = true;
                    tmp[i, j] = 1f;
                }
            }
        }

        thresholdMatrix = tmp;
        return;

        int rank = ones - 1;
        print(ones);
        print("s1");
        while (rank >= 0)                                                         //Phase 1 //todo: one of these stages (or more) is breaki
        {
            VoidAndCluster VaC = FindVoidAndCluster(BinaryPattern, sigma);
            BinaryPattern[VaC.clusterPos.x, VaC.clusterPos.y] = false;
            ditherArray[VaC.clusterPos.x, VaC.clusterPos.y] = ((float)rank / (float)MN);
            rank--;
        }

        print("s2");
        BinaryPattern = DeepCopy(initialBinaryMatrix); //Init Phase 2
        rank = ones;
        while (rank < (MN / 2))
        {                                                        //Phase 2
            VoidAndCluster VaC = FindVoidAndCluster(BinaryPattern, sigma);
            BinaryPattern[VaC.voidPos.x, VaC.voidPos.y] = true;
            ditherArray[VaC.voidPos.x, VaC.voidPos.y] = ((float)rank / (float)MN);
            rank++;

        }

        print("s3");
        while (rank < MN)       //phase 3
        {
            VoidAndCluster VaC = FindVoidAndCluster(BinaryPattern, sigma, true);
            BinaryPattern[VaC.clusterPos.x, VaC.clusterPos.y] = true;
            ditherArray[VaC.clusterPos.x, VaC.clusterPos.y] = ((float)rank / (float)MN);
            rank++;
        }

        thresholdMatrix = ditherArray;
        printMatrix(ditherArray);

        return;
    }

    bool[,] dispersebinaryMatrix(bool[,] binaryMatrix, float sigma)
    {
        Vector2Int lastCluster = new Vector2Int(-1, -1);
        while (true)
        {
            VoidAndCluster VaC = FindVoidAndCluster(binaryMatrix, sigma);
            Vector2Int cp = VaC.clusterPos;
            Vector2Int vp = VaC.voidPos;

            if (vp == lastCluster)
            {
                return binaryMatrix;

            }

            binaryMatrix[cp.x, cp.y] = false;
            binaryMatrix[vp.x, vp.y] = true;
            lastCluster = cp;
        }


        bool stable = false;
        while (!stable)
        {
            VoidAndCluster VaC = FindVoidAndCluster(binaryMatrix, sigma);
            Vector2Int cp = VaC.clusterPos;
            Vector2Int vp = VaC.voidPos;
            print("cluster" + cp.ToString());
            print("void" + VaC.voidPos.ToString());
            if (lastCluster.x != -1 && vp == lastCluster)
            {
                binaryMatrix[vp.x, vp.y] = true;
                print(lastCluster);
                print(cp);
                printMatrix(binaryMatrix);
                stable = true;
            }
            lastCluster = cp;
            binaryMatrix[cp.x, cp.y] = false;

            binaryMatrix[vp.x, vp.y] = true;
            printMatrix(binaryMatrix);
        }
        return binaryMatrix;
    }

    bool[,] fillBoolMatrix(int size, float fill)
    {
        bool[,] binMat = new bool[size, size];
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                binMat[i, j] = true;
                if ((i * size + (j + 1)) >= (size * size) * fill)
                {
                    return binMat;
                }
            }
        }
        return binMat;
    }

    float WraparoundDistanceSquared(Vector2 v1, Vector2 v2, float size)
    {
        float xDiff = Mathf.Abs(v1.x - v2.x);
        if (xDiff > size / 2)
        {
            xDiff = size - xDiff;
        }

        float yDiff = Mathf.Abs(v1.y - v2.y);
        if (yDiff > size / 2)
        {
            yDiff = size - yDiff;
        }
        return xDiff * xDiff + yDiff * yDiff;
    }

    VoidAndCluster FindVoidAndCluster(bool[,] binPat, float sigma, bool invert = false)
    {
        int size = binPat.GetLength(0);
        float[,] energyMatrix = new float[size, size];
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                Vector2 v1 = new Vector2(x, y);
                for (int i = 0; i < size; i++)
                {
                    for (int j = 0; j < size; j++)
                    {
                        Vector2 v2 = new Vector2(i, j);
                        if ((binPat[i, j] ^ invert) && v1 != v2)
                        {
                            float energy = 1 / (WraparoundDistanceSquared(v1, v2, size) / (2 * sigma * sigma) + 1);
                            energyMatrix[x, y] += energy;
                        }
                    }
                }
            }
        }

        float lowestE = float.PositiveInfinity;
        Vector2Int voidPos = new Vector2Int();

        float highestE = float.NegativeInfinity;
        Vector2Int clusterPos = new Vector2Int();

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                if (energyMatrix[x, y] < lowestE && !(binPat[x, y] ^ invert))
                {
                    lowestE = energyMatrix[x, y];

                    voidPos = new Vector2Int(x, y);
                }

                if (energyMatrix[x, y] > highestE && (binPat[x, y] ^ invert))
                {
                    highestE = energyMatrix[x, y];

                    clusterPos = new Vector2Int(x, y);
                }
            }
        }
        return new VoidAndCluster(voidPos, clusterPos);
    }

    public List<Color> InitPalette(Texture2D image, int colorCount, int iterationCount)
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
            allColors.Add(new ImageColor(entry.Key, entry.Value));
        }

        print("done removing duplicate pixels");
        print("making palette");

        List<Color> tmpPaletteColors = GenerateKMeansPalette(allColors, colorCount, iterationCount);
        List<Color> paletteColors = new List<Color>();
        for (int i = 0; i < tmpPaletteColors.Count; i++)
        {
            if (!paletteColors.Contains(tmpPaletteColors[i]))
            {
                paletteColors.Add(tmpPaletteColors[i]);
            }
        }
        print(paletteColors.Count);
        print("palette made");

        palette = paletteColors;
        return palette;
    }

    /*
        List<Color> MedianCutColors(List<Color> colors, int n)
        {
            if (n == 0)
            {
                Color[] col = { GetAvgColor(colors) };
                return col.ToList();
            }
            (float, float)[] ranges = { (1f, 0f), (1f, 0f), (1f, 0f), (1f, 0f) };
            foreach (Color c in colors)
            {
                for (int i = 0; i < 4; i++)
                {
                    float v = c[i];
                    ranges[i] = (Mathf.Min(ranges[i].Item1, v), Mathf.Max(ranges[i].Item2, v));
                }
            }
            float[] totalRanges = { ranges[0].Item2 - ranges[0].Item1, ranges[1].Item2 - ranges[1].Item1, ranges[2].Item2 - ranges[2].Item1, ranges[3].Item2 - ranges[3].Item1 };
            int dimIndex = totalRanges.ToList().IndexOf(totalRanges.Max());

            List<Color> sortedColors = new List<Color>(colors);
            sortedColors.Sort((c1, c2) => c1[dimIndex].CompareTo(c2[dimIndex]));

            int midpoint = sortedColors.Count / 2;
            List<Color> colors1 = sortedColors.Take(midpoint).ToList();
            List<Color> colors2 = sortedColors.Skip(midpoint).ToList();

            return MedianCutColors(colors1, n - 1).Concat(MedianCutColors(colors2, n - 1)).ToList();
        }
    */

    List<Color> GenerateKMeansPalette(List<ImageColor> colors, int k, int iterationCount)
    {
        List<Vector3> means = new List<Vector3>();
        List<List<int>> closestIds = new List<List<int>>();
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
        Color avg = new Color(0, 0, 0, 0);
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

    void printMatrix<T>(T[,] m)
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

    public RenderTexture Dither(Texture2D image)
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
        result = new RenderTexture(image.width, image.height, 24);  //create Result and blit intput to it
        print("ReBuilding Rentertexture");
        result.enableRandomWrite = true;
        result.Create();
        shader.SetTexture(0, "result", result);
        RenderTexture.active = result;
        Graphics.Blit(image, result);


        shader.SetInt("width", image.width);
        shader.SetInt("height", image.height);

        shader.Dispatch(0, (int)MathF.Ceiling(image.width / 8f), (int)MathF.Ceiling(image.height / 8f), 1);
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
