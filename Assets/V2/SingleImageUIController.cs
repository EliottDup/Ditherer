using System;
using System.Collections.Generic;
using System.IO;
using SFB;
using UnityEngine;
using UnityEngine.UIElements;

public class SingleImageUIController : MonoBehaviour
{
    private VisualElement _root, _palettePreview;
    private IntegerField _matrixSize;

    private Button _loadImageButton, _ditherButton, _loadPaletteButton;

    private Label _textOutput, _outputConfirmation;
    private Image _imagePreview;

    public UIDocument renderer;

    private Texture2D _imageTexture;

    private List<Color> _palette;

    private string _outputPath = "";

    private void Awake()
    {
        _root = renderer.rootVisualElement.Q("singleImageBody");

        _palettePreview = _root.Q("palettePreview");

        _matrixSize = _root.Q<IntegerField>("matrixSizeField");

        _loadImageButton = _root.Q<Button>("loadImageButton");
        _ditherButton = _root.Q<Button>("ditherButton");
        _loadPaletteButton = _root.Q<Button>("loadPaletteButton");

        _outputConfirmation = _root.Q<Label>("outputConfirmation");
        _textOutput = _root.Q<Label>("textOutput");
        _imagePreview = _root.Q<Image>("imagePreview");

        _loadImageButton.clicked += OnLoadImage;
        _loadPaletteButton.clicked += OnLoadPalette;
        _ditherButton.clicked += OnDither;
    }

    private void OnDither()
    {
        throw new NotImplementedException();
    }

    private void OnLoadPalette()
    {
        PaletteSelectionUIController psc = GetComponent<PaletteSelectionUIController>();
        _palette = psc.getPalette();
        UpdatePalettePreview();
    }

    private void UpdatePalettePreview()
    {
        _palettePreview.Clear();
        for (int i = 0; i < _palette.Count; i++)
        {
            VisualElement elem = new();
            elem.AddToClassList("previewColor");
            elem.style.backgroundColor = new StyleColor(_palette[i]);
            _palettePreview.Add(elem);
        }
    }

    private void Write(string message, bool error = false)
    {
        _textOutput.text = message;
        _textOutput.style.color = new StyleColor(error ? Color.red : Color.black);
    }

    private void OnLoadImage()
    {
        string path = "";
        try
        {
            string[] paths = StandaloneFileBrowser.OpenFilePanel("Select an image", "", new ExtensionFilter[] { new("Image Files", "png", "jpg") }, false);
            if (paths.Length == 0)
            {
                return;
            }
            path = paths[0];
            _imageTexture = ImageLoader.LoadImage(paths[0]);
        }
        catch (Exception e)
        {
            Write(e.Message, true);
            return;
        }
        _imagePreview.image = _imageTexture;

        _outputPath = Path.Join(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + "_dithered.png");
        _outputConfirmation.text = $"Output will be saved to: {_outputPath}";
    }
}
