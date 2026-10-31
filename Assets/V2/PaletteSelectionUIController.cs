using System;
using System.Collections.Generic;
using System.Linq;
using SFB;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class PaletteSelectionUIController : MonoBehaviour
{
    private VisualElement _root, _autoPaletteBody, _manualPaletteBody, _colorSpaceSettingsBody, _palettePreview;
    private DropdownField _paletteTypeDropDown;
    private IntegerField _colorCountField, _iterationCountField, _seedField;

    private Button _loadImageButton, _createPaletteButton;
    private Toggle _toggleColorSpace, _toggleColorSpacePaletteColors;
    private Slider _jitterValue;

    private Label _textOutput;
    private Image _imagePreview;

    public UIDocument renderer;

    private Texture2D _imageTexture;

    private KMeansGenerator _kMeansGenerator;

    public PointDrawer pointDrawer;

    private List<Color> _palette;
    private ColorSpace _colorspace = new OKLabColorSpace();

    public Camera camera;
    private RenderTexture _renderTexture;

    private void Awake()
    {
        _root = renderer.rootVisualElement.Q("paletteCreationBody");

        _autoPaletteBody = _root.Q("autoPaletteBody");
        _manualPaletteBody = _root.Q("manualPaletteBody");

        _colorSpaceSettingsBody = _root.Q("colorSpaceSettingsBody");

        _palettePreview = _root.Q("palettePreview");

        _paletteTypeDropDown = _root.Q<DropdownField>("paletteType");

        _colorCountField = _root.Q<IntegerField>("colorCount");
        _iterationCountField = _root.Q<IntegerField>("iterationCount");
        _jitterValue = _root.Q<Slider>("jitterValue");
        _seedField = _root.Q<IntegerField>("seed");

        _loadImageButton = _root.Q<Button>("loadImageButton");
        _createPaletteButton = _root.Q<Button>("createPaletteButton");

        _toggleColorSpace = _root.Q<Toggle>("toggleColorSpace");
        _toggleColorSpacePaletteColors = _root.Q<Toggle>("togglePaletteColors");

        _textOutput = _root.Q<Label>("textOutput");
        _imagePreview = _root.Q<Image>("imagePreview");

        _toggleColorSpace.RegisterValueChangedCallback((_) => UpdateImagePreview());
        _toggleColorSpacePaletteColors.RegisterValueChangedCallback((_) => UpdatePointDrawer());

        _loadImageButton.clicked += OnLoadImage;
        _createPaletteButton.clicked += OnGenerate;

        _renderTexture = new RenderTexture(512, 512, 16);
        _renderTexture.Create();

        camera.targetTexture = _renderTexture;

        print("done");
    }

    private void UpdateImagePreview()
    {
        print("updating!");
        if (!_toggleColorSpace.value)
        {
            _imagePreview.image = _imageTexture;
            _colorSpaceSettingsBody.style.display = DisplayStyle.None;
        }
        else
        {
            _colorSpaceSettingsBody.style.display = DisplayStyle.Flex;
            _imagePreview.image = _renderTexture;
        }
    }

    private void OnLoadImage()
    {
        print("selecting Image");
        try
        {
            string[] paths = StandaloneFileBrowser.OpenFilePanel("Select an image", "", new ExtensionFilter[] { new("Image Files", "png", "jpg") }, false);
            if (paths.Length == 0)
            {
                return;
            }
            _imageTexture = ImageLoader.LoadImage(paths[0]);
        }
        catch (Exception e)
        {
            Write(e.Message, true);
            return;
        }

        _kMeansGenerator = new KMeansGenerator(_imageTexture, _colorspace);
        SetPointDrawerPoints();

        UpdateImagePreview();
    }

    private void SetPointDrawerPoints()
    {
        pointDrawer.SetPoints(_kMeansGenerator.GetPoints(), _colorspace);
    }

    private void UpdatePointDrawer()
    {
        if (_toggleColorSpacePaletteColors.value)
        {
            pointDrawer.SetColors(_kMeansGenerator.GetClosestIDs(), _kMeansGenerator.GetColors());
        }
        else
        {
            pointDrawer.SetPoints(_kMeansGenerator.GetPoints(), _colorspace);
        }
    }

    private void OnGenerate()
    {
        _kMeansGenerator.Init(_colorCountField.value, _seedField.value);
        _kMeansGenerator.ExecuteSteps(_iterationCountField.value, _jitterValue.value);
        UpdateColors(new List<Color>(_kMeansGenerator.GetColors()));
        UpdatePointDrawer();
    }

    private void UpdateColors(List<Color> colors)
    {
        colors.Sort((a, b) =>
        {
            Color.RGBToHSV(a, out float ha, out _, out _);
            Color.RGBToHSV(b, out float hb, out _, out _);
            return (int)Mathf.Sign(ha - hb);
        });

        _palette = colors;
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
}
