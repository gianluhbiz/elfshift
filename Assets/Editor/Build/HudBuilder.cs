using Elfshift.Core;
using Elfshift.Orders;
using Elfshift.Player;
using Elfshift.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Elfshift.EditorTools
{
    /// <summary>Riferimenti agli oggetti di gioco che l'interfaccia deve conoscere.</summary>
    public sealed class HudSources
    {
        public GameSession Session;
        public OrderManager Orders;
        public PlayerInteractor Interactor;
        public Carrier Carrier;
        public ItemIconService Icons;
    }

    /// <summary>
    /// Costruisce l'interfaccia di gioco (HUD) con uGUI: ordini in alto, punteggio e biscotti,
    /// suggerimenti in basso, messaggi, oggetto in mano. Funziona con qualsiasi risoluzione:
    /// il Canvas scala rispetto a 1280x720 e gli elementi sono ancorati ai bordi.
    /// </summary>
    public static class HudBuilder
    {
        private static readonly Color PanelColor = new Color(0.09f, 0.09f, 0.17f, 0.8f);
        private static readonly Color Cream = new Color(1f, 0.97f, 0.9f, 1f);
        private static readonly Color Gold = new Color(0.98f, 0.78f, 0.25f, 1f);
        private static readonly Color Ink = new Color(0.28f, 0.17f, 0.1f, 1f);

        private static Sprite rounded;
        private static Sprite cookieSprite;
        private static Sprite starSprite;
        private static Font font;

        // ================================================================== punto d'ingresso
        public static GameObject Build(HudSources src)
        {
            LoadSprites();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GameObject canvasObject = new GameObject("HUD");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform safe = NewRect("SafeArea", canvasObject.transform);
            Stretch(safe, 24f);

            // mirino al centro dello schermo
            RectTransform crosshair = NewRect("Crosshair", canvasObject.transform);
            Anchor(crosshair, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(8f, 8f));
            Image crossImage = crosshair.gameObject.AddComponent<Image>();
            crossImage.sprite = rounded;
            crossImage.type = Image.Type.Sliced;
            crossImage.color = new Color(1f, 1f, 1f, 0.8f);
            crossImage.raycastTarget = false;
            Outline crossOutline = crosshair.gameObject.AddComponent<Outline>();
            crossOutline.effectColor = new Color(0f, 0f, 0f, 0.6f);
            crossOutline.effectDistance = new Vector2(1f, -1f);

            OrderCardView cardPrefab = BuildOrderCardPrefab();
            RectTransform ordersContainer = BuildOrdersBar(safe);
            Text pointsText, cookiesText;
            BuildStatsPanel(safe, out pointsText, out cookiesText);
            ToastView toast = BuildToast(safe);
            InteractionPromptView prompts = BuildPrompts(safe, src);
            CarriedItemView carried = BuildCarried(safe, src);

            // Collegamenti
            OrderBarView bar = ordersContainer.gameObject.AddComponent<OrderBarView>();
            Wire.Ref(bar, "orders", src.Orders);
            Wire.Ref(bar, "icons", src.Icons);
            Wire.Ref(bar, "cardPrefab", cardPrefab);
            Wire.Ref(bar, "container", ordersContainer);

            HudController hud = canvasObject.AddComponent<HudController>();
            Wire.Ref(hud, "session", src.Session);
            Wire.Ref(hud, "orders", src.Orders);
            Wire.Ref(hud, "interactor", src.Interactor);
            Wire.Ref(hud, "toast", toast);
            Wire.Ref(hud, "pointsText", pointsText);
            Wire.Ref(hud, "cookiesText", cookiesText);

            if (prompts == null || carried == null) Debug.LogWarning("[Elfshift] HUD incompleto.");
            return canvasObject;
        }

        // ================================================================== sprite e icone
        private static void LoadSprites()
        {
            string ui = AssetUtil.GeneratedTextures + "/UI";
            rounded = SaveSprite(ProceduralTextures.RoundedRect(64, 20f), ui + "/rounded.png", new Vector4(24f, 24f, 24f, 24f));
            cookieSprite = SaveSprite(ProceduralTextures.Cookie(128), ui + "/icon_cookie.png", Vector4.zero);
            starSprite = SaveSprite(ProceduralTextures.Star(128), ui + "/icon_star.png", Vector4.zero);
        }

        private static Sprite SaveSprite(Texture2D texture, string path, Vector4 border)
        {
            ProceduralTextures.SavePng(texture, path, true, border);
            Object.DestroyImmediate(texture);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ================================================================== carta dell'ordine (prefab)
        private static OrderCardView BuildOrderCardPrefab()
        {
            GameObject root = new GameObject("OrderCard", typeof(RectTransform));
            RectTransform rt = (RectTransform)root.transform;
            rt.sizeDelta = new Vector2(250f, 96f);

            Image bg = root.AddComponent<Image>();
            bg.sprite = rounded;
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.99f, 0.94f, 0.8f, 1f);
            Outline outline = root.AddComponent<Outline>();
            outline.effectColor = new Color(0.55f, 0.3f, 0.15f, 0.9f);
            outline.effectDistance = new Vector2(2f, -2f);

            // fiocco rosso in alto a sinistra
            RectTransform seal = NewRect("Seal", rt);
            Anchor(seal, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(8f, -8f), new Vector2(22f, 22f));
            Image sealImage = seal.gameObject.AddComponent<Image>();
            sealImage.sprite = rounded;
            sealImage.type = Image.Type.Sliced;
            sealImage.color = new Color(0.82f, 0.18f, 0.25f, 1f);

            RectTransform iconRt = NewRect("Icon", rt);
            Anchor(iconRt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, 4f), new Vector2(76f, 76f));
            RawImage icon = iconRt.gameObject.AddComponent<RawImage>();
            icon.color = Color.white;

            Text title = NewText("Title", rt, "Giocattolo", 26, Ink, TextAnchor.UpperLeft, FontStyle.Bold, false);
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(96f, -36f), new Vector2(-8f, -8f));
            Text child = NewText("Child", rt, "Per Marta", 19, new Color(0.45f, 0.3f, 0.2f), TextAnchor.UpperLeft, FontStyle.Normal, false);
            SetRect(child.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(96f, -60f), new Vector2(-8f, -36f));

            // barra del tempo
            RectTransform barBg = NewRect("TimerBack", rt);
            SetRect(barBg, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(96f, 12f), new Vector2(-62f, 26f));
            Image barBgImage = barBg.gameObject.AddComponent<Image>();
            barBgImage.sprite = rounded;
            barBgImage.type = Image.Type.Sliced;
            barBgImage.color = new Color(0.35f, 0.22f, 0.12f, 0.35f);

            RectTransform fill = NewRect("TimerFill", barBg);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            Image fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.sprite = rounded;
            fillImage.type = Image.Type.Sliced;
            fillImage.color = new Color(0.35f, 0.78f, 0.45f);

            Text time = NewText("Time", rt, "2:00", 20, Ink, TextAnchor.MiddleRight, FontStyle.Bold, false);
            time.rectTransform.anchorMin = new Vector2(1f, 0f);
            time.rectTransform.anchorMax = new Vector2(1f, 0f);
            time.rectTransform.pivot = new Vector2(1f, 0f);
            time.rectTransform.anchoredPosition = new Vector2(-8f, 6f);
            time.rectTransform.sizeDelta = new Vector2(60f, 26f);

            OrderCardView view = root.AddComponent<OrderCardView>();
            Wire.Ref(view, "icon", icon);
            Wire.Ref(view, "titleText", title);
            Wire.Ref(view, "childText", child);
            Wire.Ref(view, "timeText", time);
            Wire.Ref(view, "timerFill", fill);
            Wire.Ref(view, "timerFillImage", fillImage);
            Wire.Ref(view, "background", bg);

            AssetUtil.EnsureFolder(AssetUtil.UiPrefabs);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, AssetUtil.UiPrefabs + "/OrderCard.prefab");
            Object.DestroyImmediate(root);
            return prefab.GetComponent<OrderCardView>();
        }

        // ================================================================== ordini, statistiche, messaggi
        private static RectTransform BuildOrdersBar(RectTransform parent)
        {
            RectTransform bar = NewRect("OrdersBar", parent);
            Anchor(bar, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(800f, 100f));
            HorizontalLayoutGroup layout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return bar;
        }

        private static void BuildStatsPanel(RectTransform parent, out Text pointsText, out Text cookiesText)
        {
            RectTransform panel = NewRect("Stats", parent);
            Anchor(panel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(205f, 100f));
            Image bg = panel.gameObject.AddComponent<Image>();
            bg.sprite = rounded;
            bg.type = Image.Type.Sliced;
            bg.color = PanelColor;

            pointsText = BuildStatRow(panel, "Points", starSprite, -14f);
            cookiesText = BuildStatRow(panel, "Cookies", cookieSprite, -54f);
        }

        private static Text BuildStatRow(RectTransform parent, string name, Sprite icon, float y)
        {
            RectTransform row = NewRect(name, parent);
            Anchor(row, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(-24f, 38f));
            RectTransform iconRt = NewRect("Icon", row);
            Anchor(iconRt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(36f, 36f));
            Image iconImage = iconRt.gameObject.AddComponent<Image>();
            iconImage.sprite = icon;
            iconImage.preserveAspect = true;
            Text value = NewText("Value", row, "0", 30, Cream, TextAnchor.MiddleLeft, FontStyle.Bold, true);
            SetRect(value.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f), new Vector2(46f, 0f), new Vector2(0f, 0f));
            value.rectTransform.offsetMin = new Vector2(46f, 0f);
            value.rectTransform.offsetMax = Vector2.zero;
            return value;
        }

        private static ToastView BuildToast(RectTransform parent)
        {
            RectTransform toast = NewRect("Toast", parent);
            Anchor(toast, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(980f, 72f));
            CanvasGroup group = toast.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            Image bg = toast.gameObject.AddComponent<Image>();
            bg.sprite = rounded;
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.1f, 0.1f, 0.18f, 0.88f);
            Text label = NewText("Label", toast, "", 30, Gold, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            Stretch(label.rectTransform, 8f);

            ToastView view = toast.gameObject.AddComponent<ToastView>();
            Wire.Ref(view, "label", label);
            Wire.Ref(view, "group", group);
            return view;
        }

        // ================================================================== suggerimenti contestuali
        private static InteractionPromptView BuildPrompts(RectTransform parent, HudSources src)
        {
            RectTransform bar = NewRect("Prompts", parent);
            Anchor(bar, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(900f, 56f));
            HorizontalLayoutGroup layout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 24f;
            layout.childAlignment = TextAnchor.LowerCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            Text pickupKey, pickupLabel, interactKey, interactLabel;
            GameObject pickupChip = BuildChip(bar, "PickupChip", out pickupKey, out pickupLabel);
            GameObject interactChip = BuildChip(bar, "InteractChip", out interactKey, out interactLabel);

            // barra di avanzamento sopra i suggerimenti
            RectTransform progress = NewRect("HoldProgress", parent);
            Anchor(progress, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 74f), new Vector2(340f, 22f));
            Image progressBg = progress.gameObject.AddComponent<Image>();
            progressBg.sprite = rounded;
            progressBg.type = Image.Type.Sliced;
            progressBg.color = new Color(0.09f, 0.09f, 0.17f, 0.85f);
            RectTransform fill = NewRect("Fill", progress);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0f, 1f);
            fill.offsetMin = new Vector2(3f, 3f);
            fill.offsetMax = new Vector2(-3f, -3f);
            Image fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.sprite = rounded;
            fillImage.type = Image.Type.Sliced;
            fillImage.color = Gold;

            InteractionPromptView view = parent.gameObject.AddComponent<InteractionPromptView>();
            Wire.Ref(view, "interactor", src.Interactor);
            Wire.Ref(view, "pickupChip", pickupChip);
            Wire.Ref(view, "pickupKeyText", pickupKey);
            Wire.Ref(view, "pickupLabel", pickupLabel);
            Wire.Ref(view, "interactChip", interactChip);
            Wire.Ref(view, "interactKeyText", interactKey);
            Wire.Ref(view, "interactLabel", interactLabel);
            Wire.Ref(view, "progressRoot", progress.gameObject);
            Wire.Ref(view, "progressFill", fill);
            return view;
        }

        private static GameObject BuildChip(RectTransform parent, string name, out Text key, out Text label)
        {
            RectTransform chip = NewRect(name, parent);
            chip.sizeDelta = new Vector2(10f, 56f);
            Image bg = chip.gameObject.AddComponent<Image>();
            bg.sprite = rounded;
            bg.type = Image.Type.Sliced;
            bg.color = PanelColor;
            HorizontalLayoutGroup layout = chip.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 22, 6, 6);
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = chip.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            RectTransform keyBox = NewRect("Key", chip);
            Image keyImage = keyBox.gameObject.AddComponent<Image>();
            keyImage.sprite = rounded;
            keyImage.type = Image.Type.Sliced;
            keyImage.color = Cream;
            LayoutElement keyLayout = keyBox.gameObject.AddComponent<LayoutElement>();
            keyLayout.minWidth = 64f;
            keyLayout.minHeight = 42f;
            keyLayout.preferredHeight = 42f;
            key = NewText("KeyText", keyBox, "E", 22, Ink, TextAnchor.MiddleCenter, FontStyle.Bold, false);
            Stretch(key.rectTransform, 6f);
            key.horizontalOverflow = HorizontalWrapMode.Overflow;

            label = NewText("Label", chip, "Azione", 26, Cream, TextAnchor.MiddleLeft, FontStyle.Bold, true);
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            chip.gameObject.SetActive(false);
            return chip.gameObject;
        }

        private static CarriedItemView BuildCarried(RectTransform parent, HudSources src)
        {
            RectTransform panel = NewRect("Carried", parent);
            Anchor(panel, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(230f, 92f));
            Image bg = panel.gameObject.AddComponent<Image>();
            bg.sprite = rounded;
            bg.type = Image.Type.Sliced;
            bg.color = PanelColor;

            RectTransform iconRt = NewRect("Icon", panel);
            Anchor(iconRt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(72f, 72f));
            RawImage icon = iconRt.gameObject.AddComponent<RawImage>();
            Text caption = NewText("Caption", panel, "IN MANO", 16, new Color(1f, 0.97f, 0.9f, 0.7f), TextAnchor.UpperLeft, FontStyle.Bold, false);
            SetRect(caption.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(92f, -36f), new Vector2(-8f, -14f));
            Text label = NewText("Name", panel, "Oggetto", 28, Cream, TextAnchor.MiddleLeft, FontStyle.Bold, true);
            SetRect(label.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f), new Vector2(92f, -8f), new Vector2(-8f, -34f));
            label.rectTransform.offsetMin = new Vector2(92f, 10f);
            label.rectTransform.offsetMax = new Vector2(-8f, -34f);

            // Il componente sta su un oggetto SEMPRE attivo (il Safe Area): se stesse sul riquadro disattivato non si accenderebbe mai.
            CarriedItemView view = parent.gameObject.AddComponent<CarriedItemView>();
            Wire.Ref(view, "carrier", src.Carrier);
            Wire.Ref(view, "icons", src.Icons);
            Wire.Ref(view, "root", panel.gameObject);
            Wire.Ref(view, "icon", icon);
            Wire.Ref(view, "label", label);
            panel.gameObject.SetActive(false);
            return view;
        }

        // ================================================================== piccoli aiuti per uGUI
        private static RectTransform NewRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Stretch(RectTransform rt, float margin)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(margin, margin);
            rt.offsetMax = new Vector2(-margin, -margin);
        }

        private static void Anchor(RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        /// <summary>Imposta ancore e margini: offsetMin/offsetMax sono i margini rispetto alle ancore.</summary>
        private static void SetRect(RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.pivot = pivot;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        private static Text NewText(string name, Transform parent, string content, int size, Color color, TextAnchor anchor, FontStyle style, bool shadow)
        {
            RectTransform rt = NewRect(name, parent);
            Text text = rt.gameObject.AddComponent<Text>();
            text.font = font;
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = anchor;
            text.fontStyle = style;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            if (shadow)
            {
                Shadow s = rt.gameObject.AddComponent<Shadow>();
                s.effectColor = new Color(0f, 0f, 0f, 0.55f);
                s.effectDistance = new Vector2(1.5f, -1.5f);
            }
            return text;
        }
    }
}
