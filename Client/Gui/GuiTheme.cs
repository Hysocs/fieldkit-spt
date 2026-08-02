
namespace FieldKit
{
    public sealed partial class Plugin
    {
        private void EnsureGuiTheme()
        {
            if (_adminSkin != null)
                return;

            Color32 window = new Color32(17, 21, 28, 255);
            Color32 surface = new Color32(18, 22, 30, 255);
            Color32 raised = new Color32(24, 29, 39, 255);
            Color32 hover = new Color32(32, 38, 51, 255);
            Color32 border = new Color32(43, 50, 66, 255);
            Color32 text = new Color32(230, 233, 240, 255);
            Color32 muted = new Color32(137, 145, 167, 255);
            Color accentColor = _guiPrimaryColor == null
                ? new Color32(120, 207, 245, 255)
                : ParseVisualColor(
                    _guiPrimaryColor.Value,
                    new Color32(120, 207, 245, 255));
            Color32 accent = accentColor;
            Color32 accentHover = Color.Lerp(
                accentColor, Color.white, 0.18f);
            Color32 accentDeep = Color.Lerp(
                accentColor, Color.black, 0.34f);

            Texture2D windowTexture =
                CreateSolidThemeTexture(window);
            Texture2D surfaceTexture =
                CreateThemeTexture(surface, border, 5);
            Texture2D raisedTexture =
                CreateThemeTexture(raised, border, 5);
            Texture2D hoverTexture =
                CreateThemeTexture(hover, accent, 5);
            Texture2D accentTexture =
                CreateThemeTexture(accentDeep, accent, 5);
            Texture2D accentHoverTexture =
                CreateThemeTexture(accent, accentHover, 5);
            Texture2D sliderTexture =
                CreateSliderTrackTexture(border);
            Texture2D thumbTexture =
                CreateSliderThumbTexture(accent, accentHover);
            Texture2D checkboxTexture =
                CreateCheckboxTexture(raised, border, text, false);
            Texture2D checkboxHoverTexture =
                CreateCheckboxTexture(raised, accent, text, false);
            Texture2D checkboxCheckedTexture =
                CreateCheckboxTexture(accent, accent, Color.white, true);
            Texture2D checkboxCheckedHoverTexture =
                CreateCheckboxTexture(
                    accentHover,
                    accentHover,
                    Color.white,
                    true);

            _adminSkin = Instantiate(GUI.skin);
            _adminSkin.name = "FieldKit Skin";
            Font menuFont = LoadMenuFont();
            if (menuFont != null)
                ApplyMenuFont(_adminSkin, menuFont);

            ConfigureStyle(
                _adminSkin.window,
                windowTexture,
                windowTexture,
                windowTexture,
                text,
                accent,
                accent);
            _adminSkin.window.border = new RectOffset(0, 0, 0, 0);
            _adminSkin.window.padding = new RectOffset(14, 14, 34, 14);
            _adminSkin.window.fontSize = 14;
            _adminSkin.window.fontStyle = FontStyle.Bold;
            _adminSkin.window.alignment = TextAnchor.UpperLeft;

            ConfigureStyle(
                _adminSkin.box,
                surfaceTexture,
                surfaceTexture,
                surfaceTexture,
                text,
                text,
                text);
            _adminSkin.box.border = new RectOffset(4, 4, 4, 4);
            _adminSkin.box.padding = new RectOffset(14, 14, 12, 14);
            _adminSkin.box.margin = new RectOffset(5, 5, 6, 6);

            ConfigureStyle(
                _adminSkin.button,
                raisedTexture,
                hoverTexture,
                accentTexture,
                text,
                accentHover,
                Color.white);
            _adminSkin.button.border = new RectOffset(4, 4, 4, 4);
            _adminSkin.button.padding = new RectOffset(12, 12, 6, 6);
            _adminSkin.button.margin = new RectOffset(3, 3, 3, 3);
            _adminSkin.button.fixedHeight = 30f;

            _adminSkin.toggle.normal.background = checkboxTexture;
            _adminSkin.toggle.normal.textColor = text;
            _adminSkin.toggle.hover.background = checkboxHoverTexture;
            _adminSkin.toggle.hover.textColor = text;
            _adminSkin.toggle.active.background = checkboxHoverTexture;
            _adminSkin.toggle.active.textColor = text;
            _adminSkin.toggle.focused.background = checkboxTexture;
            _adminSkin.toggle.focused.textColor = text;
            _adminSkin.toggle.onNormal.background =
                checkboxCheckedTexture;
            _adminSkin.toggle.onNormal.textColor = text;
            _adminSkin.toggle.onHover.background =
                checkboxCheckedHoverTexture;
            _adminSkin.toggle.onHover.textColor = text;
            _adminSkin.toggle.onActive.background =
                checkboxCheckedHoverTexture;
            _adminSkin.toggle.onActive.textColor = text;
            _adminSkin.toggle.onFocused.background =
                checkboxCheckedTexture;
            _adminSkin.toggle.onFocused.textColor = text;
            _adminSkin.toggle.border =
                new RectOffset(18, 45, 0, 0);
            _adminSkin.toggle.padding = new RectOffset(25, 4, 4, 4);
            _adminSkin.toggle.margin = new RectOffset(2, 2, 2, 2);
            _adminSkin.toggle.fontSize = 13;
            _adminSkin.toggle.wordWrap = false;
            _adminSkin.toggle.fixedHeight = Mathf.Ceil(
                Mathf.Max(
                    30f,
                    _adminSkin.toggle.CalcHeight(
                        new GUIContent("Ag"),
                        400f) + 8f));
            _adminSkin.toggle.alignment = TextAnchor.MiddleLeft;

            _adminSkin.label.normal.textColor = text;
            _adminSkin.label.hover.textColor = text;
            _adminSkin.label.fontSize = 13;
            _adminSkin.label.padding = new RectOffset(3, 3, 2, 2);
            _adminSkin.label.wordWrap = true;

            ConfigureStyle(
                _adminSkin.horizontalSlider,
                sliderTexture,
                sliderTexture,
                sliderTexture,
                text,
                text,
                text);
            _adminSkin.horizontalSlider.border =
                new RectOffset(8, 8, 8, 8);
            _adminSkin.horizontalSlider.fixedHeight = 18f;
            _adminSkin.horizontalSlider.margin =
                new RectOffset(5, 5, 4, 8);
            _adminSkin.horizontalSlider.padding =
                new RectOffset(0, 0, 0, 0);
            _adminSkin.horizontalSlider.overflow =
                new RectOffset(0, 0, 0, 0);

            ConfigureStyle(
                _adminSkin.horizontalSliderThumb,
                thumbTexture,
                accentHoverTexture,
                accentTexture,
                text,
                text,
                text);
            _adminSkin.horizontalSliderThumb.border =
                new RectOffset(8, 8, 8, 8);
            _adminSkin.horizontalSliderThumb.fixedWidth = 18f;
            _adminSkin.horizontalSliderThumb.fixedHeight = 18f;
            _adminSkin.horizontalSliderThumb.margin =
                new RectOffset(0, 0, 0, 0);
            _adminSkin.horizontalSliderThumb.padding =
                new RectOffset(0, 0, 0, 0);
            _adminSkin.horizontalSliderThumb.overflow =
                new RectOffset(0, 0, 0, 0);

            ConfigureStyle(
                _adminSkin.scrollView,
                windowTexture,
                windowTexture,
                windowTexture,
                text,
                text,
                text);
            _adminSkin.scrollView.border = new RectOffset(0, 0, 0, 0);
            _adminSkin.scrollView.padding = new RectOffset(5, 5, 5, 5);

            ConfigureStyle(
                _adminSkin.verticalScrollbar,
                windowTexture,
                windowTexture,
                windowTexture,
                muted,
                muted,
                muted);
            ConfigureStyle(
                _adminSkin.verticalScrollbarThumb,
                raisedTexture,
                hoverTexture,
                accentTexture,
                muted,
                accent,
                accent);
            _adminSkin.verticalScrollbar.fixedWidth = 10f;
            _adminSkin.verticalScrollbarThumb.fixedWidth = 10f;

            _tabStyle = new GUIStyle(_adminSkin.button);
            _tabStyle.fixedHeight = 36f;
            _tabStyle.fontStyle = FontStyle.Bold;
            _tabStyle.normal.background = windowTexture;
            _tabStyle.normal.textColor = muted;
            _tabStyle.hover.background = hoverTexture;
            _tabStyle.hover.textColor = text;
            _tabStyle.onNormal.background = raisedTexture;
            _tabStyle.onNormal.textColor = accent;
            _tabStyle.onHover.background = raisedTexture;
            _tabStyle.onHover.textColor = accentHover;
            _tabStyle.onActive.background = accentTexture;
            _tabStyle.onActive.textColor = Color.white;
            _tabStyle.focused.background = windowTexture;
            _tabStyle.focused.textColor = muted;
            _tabStyle.onFocused.background = raisedTexture;
            _tabStyle.onFocused.textColor = accent;

            _sectionTitleStyle = new GUIStyle(_adminSkin.label);
            _sectionTitleStyle.normal.textColor = accent;
            _sectionTitleStyle.fontStyle = FontStyle.Bold;
            _sectionTitleStyle.fontSize = 14;
            _sectionTitleStyle.margin = new RectOffset(0, 0, 0, 6);

            _resetButtonStyle = new GUIStyle(_adminSkin.button);
            _resetButtonStyle.fixedWidth = 28f;
            _resetButtonStyle.fixedHeight = 28f;
            _resetButtonStyle.padding = new RectOffset(0, 0, 0, 1);
            _resetButtonStyle.margin = new RectOffset(4, 0, 0, 0);
            _resetButtonStyle.alignment = TextAnchor.MiddleCenter;
            _resetButtonStyle.fontSize = 18;
            _resetButtonStyle.fontStyle = FontStyle.Normal;

            _dropdownButtonStyle = new GUIStyle(_adminSkin.button);
            _dropdownButtonStyle.alignment = TextAnchor.MiddleLeft;
            _dropdownButtonStyle.padding =
                new RectOffset(12, 32, 5, 5);
            _dropdownButtonStyle.fixedHeight = 32f;

            _dropdownArrowStyle = new GUIStyle(_adminSkin.label);
            _dropdownArrowStyle.alignment = TextAnchor.MiddleCenter;
            _dropdownArrowStyle.normal.textColor = muted;
            _dropdownArrowStyle.fontSize = 12;

            _dropdownMenuStyle = new GUIStyle(_adminSkin.box);
            _dropdownMenuStyle.padding =
                new RectOffset(5, 5, 5, 5);
            _dropdownMenuStyle.border =
                new RectOffset(4, 4, 4, 4);

            _dropdownItemStyle = new GUIStyle(_adminSkin.button);
            _dropdownItemStyle.fixedHeight = 30f;
            _dropdownItemStyle.margin =
                new RectOffset(0, 0, 0, 0);
            _dropdownItemStyle.alignment = TextAnchor.MiddleLeft;
            _dropdownItemStyle.padding =
                new RectOffset(10, 10, 4, 4);
            _dropdownItemStyle.onNormal.background = accentTexture;
            _dropdownItemStyle.onNormal.textColor = Color.white;
            _dropdownItemStyle.onHover.background =
                accentHoverTexture;
            _dropdownItemStyle.onHover.textColor = Color.white;

            _optionTooltipStyle = new GUIStyle(_adminSkin.box);
            _optionTooltipStyle.normal.background = hoverTexture;
            _optionTooltipStyle.normal.textColor = text;
            _optionTooltipStyle.padding = new RectOffset(10, 10, 8, 8);
            _optionTooltipStyle.border = new RectOffset(4, 4, 4, 4);
            _optionTooltipStyle.wordWrap = true;
            _optionTooltipStyle.fontSize = 12;
        }

        private Texture2D CreateThemeTexture(
            Color32 fill,
            Color32 border,
            int size)
        {
            size = Mathf.Max(size, 12);
            Texture2D texture = new Texture2D(
                size,
                size,
                TextureFormat.RGBA32,
                false);
            Color32[] pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    const int radius = 3;
                    int nearestX = Mathf.Clamp(
                        x,
                        radius,
                        size - radius - 1);
                    int nearestY = Mathf.Clamp(
                        y,
                        radius,
                        size - radius - 1);
                    int deltaX = x - nearestX;
                    int deltaY = y - nearestY;
                    int distanceSquared =
                        deltaX * deltaX + deltaY * deltaY;
                    bool outside =
                        distanceSquared > radius * radius;
                    bool edge =
                        !outside &&
                        (x == 0 ||
                         y == 0 ||
                         x == size - 1 ||
                         y == size - 1 ||
                         distanceSquared >=
                            (radius - 1) * (radius - 1));
                    pixels[y * size + x] = outside
                        ? new Color32(0, 0, 0, 0)
                        : edge ? border : fill;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.hideFlags = HideFlags.HideAndDontSave;
            _themeTextures.Add(texture);
            return texture;
        }

        private Texture2D CreateSolidThemeTexture(
            Color32 color)
        {
            Texture2D texture = new Texture2D(
                2,
                2,
                TextureFormat.RGBA32,
                false);
            texture.SetPixels32(
                new[] { color, color, color, color });
            texture.Apply(false, true);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.hideFlags = HideFlags.HideAndDontSave;
            _themeTextures.Add(texture);
            return texture;
        }

        private Texture2D CreateSliderTrackTexture(
            Color32 color)
        {
            const int width = 24;
            const int height = 18;
            Texture2D texture = new Texture2D(
                width,
                height,
                TextureFormat.RGBA32,
                false);
            Color32[] pixels = new Color32[width * height];
            Color32 clear = new Color32(0, 0, 0, 0);
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = clear;

            for (int y = 7; y <= 10; y++)
            {
                for (int x = 0; x < width; x++)
                    pixels[y * width + x] = color;
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.hideFlags = HideFlags.HideAndDontSave;
            _themeTextures.Add(texture);
            return texture;
        }

        private Texture2D CreateSliderThumbTexture(
            Color32 fill,
            Color32 border)
        {
            const int size = 18;
            const float center = 8.5f;
            Texture2D texture = new Texture2D(
                size,
                size,
                TextureFormat.RGBA32,
                false);
            Color32[] pixels = new Color32[size * size];
            Color32 clear = new Color32(0, 0, 0, 0);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float deltaX = x - center;
                    float deltaY = y - center;
                    float distance = Mathf.Sqrt(
                        deltaX * deltaX + deltaY * deltaY);
                    pixels[y * size + x] =
                        distance > 8f
                            ? clear
                            : distance > 6.5f
                                ? border
                                : fill;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.hideFlags = HideFlags.HideAndDontSave;
            _themeTextures.Add(texture);
            return texture;
        }

        private Texture2D CreateCheckboxTexture(
            Color32 fill,
            Color32 border,
            Color32 check,
            bool isChecked)
        {
            const int width = 64;
            const int height = 22;
            Texture2D texture = new Texture2D(
                width,
                height,
                TextureFormat.RGBA32,
                false);
            Color32[] pixels = new Color32[width * height];
            Color32 clear = new Color32(0, 0, 0, 0);

            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = clear;

            for (int y = 4; y <= 17; y++)
            {
                for (int x = 2; x <= 15; x++)
                {
                    bool edge =
                        x == 2 ||
                        x == 15 ||
                        y == 4 ||
                        y == 17;
                    pixels[y * width + x] = edge ? border : fill;
                }
            }

            if (isChecked)
            {
                SetCheckboxPixel(pixels, width, 5, 10, check);
                SetCheckboxPixel(pixels, width, 6, 9, check);
                SetCheckboxPixel(pixels, width, 7, 8, check);
                SetCheckboxPixel(pixels, width, 7, 9, check);
                SetCheckboxPixel(pixels, width, 8, 9, check);
                SetCheckboxPixel(pixels, width, 8, 10, check);
                SetCheckboxPixel(pixels, width, 9, 10, check);
                SetCheckboxPixel(pixels, width, 9, 11, check);
                SetCheckboxPixel(pixels, width, 10, 11, check);
                SetCheckboxPixel(pixels, width, 10, 12, check);
                SetCheckboxPixel(pixels, width, 11, 12, check);
                SetCheckboxPixel(pixels, width, 11, 13, check);
                SetCheckboxPixel(pixels, width, 12, 13, check);
                SetCheckboxPixel(pixels, width, 12, 14, check);
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.hideFlags = HideFlags.HideAndDontSave;
            _themeTextures.Add(texture);
            return texture;
        }

        private static void SetCheckboxPixel(
            Color32[] pixels,
            int width,
            int x,
            int y,
            Color32 color)
        {
            pixels[y * width + x] = color;
        }

        private static void ConfigureStyle(
            GUIStyle style,
            Texture2D normal,
            Texture2D hover,
            Texture2D active,
            Color normalText,
            Color hoverText,
            Color activeText)
        {
            style.normal.background = normal;
            style.normal.textColor = normalText;
            style.hover.background = hover;
            style.hover.textColor = hoverText;
            style.active.background = active;
            style.active.textColor = activeText;
            style.focused.background = hover;
            style.focused.textColor = hoverText;
            style.onNormal.background = active;
            style.onNormal.textColor = activeText;
            style.onHover.background = active;
            style.onHover.textColor = activeText;
            style.onActive.background = active;
            style.onActive.textColor = activeText;
            style.onFocused.background = active;
            style.onFocused.textColor = activeText;
        }

        private static Font FindTarkovMenuFont()
        {
            Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
            Font fallback = null;
            for (int i = 0; i < fonts.Length; i++)
            {
                Font font = fonts[i];
                if (font == null || string.IsNullOrEmpty(font.name))
                    continue;

                string name = font.name.ToLowerInvariant();
                if (name.Contains("bender"))
                    return font;
                if (fallback == null &&
                    (name.Contains("din") ||
                     name.Contains("neusa") ||
                     name.Contains("tarkov")))
                    fallback = font;
            }

            return fallback;
        }

        private Font LoadMenuFont()
        {
            string name = _menuFontName == null
                ? "Segoe UI"
                : _menuFontName.Value;
            if (string.Equals(
                    name,
                    "Tarkov (Native)",
                    StringComparison.OrdinalIgnoreCase))
            {
                Font native = FindTarkovMenuFont();
                if (native != null)
                    return native;
            }

            try
            {
                return Font.CreateDynamicFontFromOSFont(name, 16);
            }
            catch
            {
                return GUI.skin == null ? null : GUI.skin.font;
            }
        }

        private void OnMenuFontSettingChanged(
            object sender,
            EventArgs args)
        {
            _guiThemeRefreshRequested = true;
        }

        private static void ApplyMenuFont(
            GUISkin skin,
            Font font)
        {
            skin.font = font;
            GUIStyle[] styles =
            {
                skin.box,
                skin.button,
                skin.horizontalScrollbar,
                skin.horizontalScrollbarLeftButton,
                skin.horizontalScrollbarRightButton,
                skin.horizontalScrollbarThumb,
                skin.horizontalSlider,
                skin.horizontalSliderThumb,
                skin.label,
                skin.scrollView,
                skin.textArea,
                skin.textField,
                skin.toggle,
                skin.verticalScrollbar,
                skin.verticalScrollbarDownButton,
                skin.verticalScrollbarThumb,
                skin.verticalScrollbarUpButton,
                skin.verticalSlider,
                skin.verticalSliderThumb,
                skin.window
            };
            for (int i = 0; i < styles.Length; i++)
                if (styles[i] != null)
                    styles[i].font = font;
        }

        private void DisposeGuiTheme()
        {
            if (_menuCursorApplied)
            {
                Cursor.SetCursor(
                    null,
                    Vector2.zero,
                    CursorMode.Auto);
                _menuCursorApplied = false;
            }

            if (_adminSkin != null)
                Destroy(_adminSkin);

            _adminSkin = null;
            _tabStyle = null;
            _sectionTitleStyle = null;
            _resetButtonStyle = null;
            _dropdownButtonStyle = null;
            _dropdownArrowStyle = null;
            _dropdownMenuStyle = null;
            _dropdownItemStyle = null;
            _optionTooltipStyle = null;
            _menuCursorTexture = null;

            for (int i = 0; i < _themeTextures.Count; i++)
            {
                if (_themeTextures[i] != null)
                    Destroy(_themeTextures[i]);
            }

            _themeTextures.Clear();
        }

        private void OnGuiPrimaryColorChanged(
            object sender,
            System.EventArgs args)
        {
            _guiThemeRefreshRequested = true;
        }

    }
}
