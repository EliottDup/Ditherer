using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

public class UIController : MonoBehaviour
{
    public Ditherer ditherer;


    VisualElement root, autoPaletteSettings, customPaletteSettings, bayerSettings, BlueNoiseSettings, palettePreview;
    DropdownField paletteType, ditherType;
    TextField loadPath, savePath, customColors;
    IntegerField colorCount, iterationCount, matrixSize, paletteFrame;
    Button genPaletteButton, runButton;
    Label errorOutput;

    delegate void MatrixFunction(int size);

    void OnEnable()
    {
        ditherer = GetComponent<Ditherer>();

        root = GetComponent<UIDocument>().rootVisualElement;

        loadPath = root.Q<TextField>("loadPath");
        savePath = root.Q<TextField>("savePath");


        paletteType = root.Q<DropdownField>("paletteType");

        autoPaletteSettings = root.Q<VisualElement>("auto");
        colorCount = root.Q<IntegerField>("colorCount");
        iterationCount = root.Q<IntegerField>("iterationCount");

        customPaletteSettings = root.Q<VisualElement>("custom");
        customColors = root.Q<TextField>("customColors");
        paletteFrame = root.Q<IntegerField>("paletteFrame");

        ditherType = root.Q<DropdownField>("ditherType");

        bayerSettings = root.Q<VisualElement>("bayerSettings");
        matrixSize = root.Q<IntegerField>("matrixSize");

        BlueNoiseSettings = root.Q<VisualElement>("blueNoiseSettings");

        genPaletteButton = root.Q<Button>("paletteGeneration");
        palettePreview = root.Q<VisualElement>("palettePreview");

        runButton = root.Q<Button>("runButton");



        paletteType.RegisterValueChangedCallback(evt => PaletteTypeValueChanged(evt.newValue));

        ditherType.RegisterValueChangedCallback(evt => DitherTypeValueChanged(evt.newValue));

        genPaletteButton.clicked += GeneratePalette;


        runButton.clicked += () => StartCoroutine(RunDitherer());

        savePath.value = Application.persistentDataPath + "/output";
        if (!Directory.Exists(savePath.value))
        {
            Directory.CreateDirectory(savePath.value);
        }

        errorOutput = root.Q<Label>("errorOutput");
    }

    void PaletteTypeValueChanged(string selectedValue)
    {
        if (selectedValue != "auto")
        {
            autoPaletteSettings.style.display = DisplayStyle.None;
            customPaletteSettings.style.display = DisplayStyle.Flex;
            return;
        }
        autoPaletteSettings.style.display = DisplayStyle.Flex;
        customPaletteSettings.style.display = DisplayStyle.None;
        return;
    }

    void DitherTypeValueChanged(string selectedValue)
    {
        if (selectedValue != "bayer")
        {
            matrixSize.label = "Texture Size";
            return;
        }
        matrixSize.label = "Matrix Size";
        return;
    }

    void GeneratePalette()
    {
        WriteError("generating palette");
        List<Color> palette = new List<Color>();
        if (paletteType.value == "auto")
        {
            if (File.Exists(loadPath.value))
            {
                Texture2D img = LoadPNG(loadPath.value);
                if (img == null)
                {
                    WriteError("Error: couldn't load image");
                    return;
                }
                palette = ditherer.InitPalette(img, colorCount.value, iterationCount.value);
                WriteError("generated palette for " + loadPath.value);
                Destroy(img);
            }
            else if (Directory.Exists(loadPath.value))
            {
                string[] images = Directory.GetFiles(loadPath.value, "*.png");
                if (images.Length == 0)
                {
                    WriteError("Error: no images found in folder");
                    return;
                }
                string imgPath = images[(int)Mathf.Clamp(paletteFrame.value, 0, images.Length - 1)];
                Texture2D img = LoadPNG(imgPath);
                if (img == null)
                {
                    WriteError("Error: couldn't load image");
                    return;
                }
                palette = ditherer.InitPalette(img, colorCount.value, iterationCount.value);
                WriteError("generated palette for " + imgPath);
                Destroy(img);
            }
        }
        else
        {
            List<string> colors = customColors.value.Replace(" ", "").Split(",").ToList();
            foreach (string hex in colors)
            {
                Color color;
                if (ColorUtility.TryParseHtmlString(hex, out color))
                {
                    palette.Add(color);
                }
            }
            ditherer.palette = palette;
        }
        palettePreview.Clear();
        for (int i = 0; i < palette.Count; i++)
        {
            VisualElement elem = new VisualElement();
            elem.AddToClassList("previewColor");
            elem.style.backgroundColor = new StyleColor(palette[i]);
            palettePreview.Add(elem);
        }

    }

    Texture2D LoadPNG(string path)
    {
        Texture2D tex = null;
        byte[] fileData;

        if (File.Exists(path))
        {
            fileData = File.ReadAllBytes(path);
            tex = new Texture2D(2, 2);
            tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
        }
        return tex;
    }

    IEnumerator RunDitherer()
    {
        WriteError("starting");

        if (!Directory.Exists(savePath.value))
        {
            WriteError("Error: save folder not found");
        }

        if (!IsPowerOfTwo(matrixSize.value) && ditherType.value == "bayer")
        {
            WriteError("matrixSize is not a power of two");
            yield break;
        }

        if (ditherType.value == "bayer")
        {
            ditherer.InitThresholdMatrix(matrixSize.value);
        }
        else
        {
            print("starting blue noise gen");
            ditherer.InitBlueNoise(matrixSize.value, 0.1f, 1.9f);
            print("blue noise generated");
        }

        ditherer.InitBuffers();

        if (File.Exists(loadPath.value))
        {
            Dither(loadPath.value);
        }
        else if (Directory.Exists(loadPath.value))
        {
            string[] images = Directory.GetFiles(loadPath.value, "*.png");
            if (images.Length == 0)
            {
                WriteError("no images found in given folder");
                ditherer.DisposeBuffers();
                yield break;

            }

            foreach (string img in images)
            {
                yield return null;
                Dither(img);
            }
        }
        else
        {
            WriteError("Error: save path not found");
            ditherer.DisposeBuffers();
            yield break;
        }
        ditherer.DisposeBuffers();
        WriteError("done");
    }

    bool IsPowerOfTwo(int v)
    {
        return v != 0 && ((v & (v - 1)) == 0);
    }

    void Dither(string imgPath)
    {
        Texture2D img = LoadPNG(imgPath);
        if (img == null)
        {
            WriteError("Error: image " + imgPath + " wasnt found");
            return;
        }
        if (!Directory.Exists(savePath.value))
        {
            WriteError("Error: save path not found");
            return;
        }
        WriteError("currently dithering " + Path.GetFileName(imgPath));
        RenderTexture result = ditherer.Dither(img);

        savePNG(savePath.value, Path.GetFileName(imgPath), result);
        result.Release();
        Destroy(img);
    }

    void savePNG(string path, string name, RenderTexture rt)
    {
        // Create a new texture of the same size as the render texture
        Texture2D texture2D = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);

        // Read the pixels from the render texture
        RenderTexture.active = rt;
        texture2D.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        texture2D.Apply();

        // Reset active render texture
        RenderTexture.active = null;

        // Convert the texture to a PNG byte array
        byte[] bytes = texture2D.EncodeToPNG();

        // Define the file path
        string filePath = path + "/" + name;

        // Write the PNG byte array to a file
        File.WriteAllBytes(filePath, bytes);

        Debug.Log("Render texture saved to: " + filePath);
        Destroy(texture2D);
    }

    void WriteError(string msg)
    {
        print(msg);
        errorOutput.text = msg;
    }
}
