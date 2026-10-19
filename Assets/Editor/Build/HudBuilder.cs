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
        public Elfshift.Production.CraftingStation Bench;
        public Transform ShelfAnchor;
        public Transform SleighAnchor;
        public Transform MarkerObject;
    }

    /// <summary>
    /// Costruisce l'interfaccia di gioco in stile "adesivo cartoon": pannelli tondi con contorno marrone spesso,
    /// colori caldi e saturi, font arrotondato e tante piccole animazioni (rimbalzi, dondolii, scritte volanti).
    /// Il Canvas scala rispetto a 1280x720 e gli elementi sono ancorati ai bordi dello schermo.
    /// </summary>
    public static class HudBuilder
    {
        private static readonly Color Ink = new Color(0.23f, 0.15f, 0.13f, 1f);
        private static readonly Color Cream = new Color(1f, 0.95f, 0.8f, 1f);
        private static readonly Color Gold = new Color(1f, 0.82f, 0.3f, 1f);
        private static readonly Color Red = new Color(0.93f, 0.3f, 0.32f, 1f);
        private static readonly Color Green = new Color(0.36f, 0.76f, 0.45f, 1f);
        private static readonly Color White = Color.white;

        private static Sprite panel;
        private static Sprite circle;
        private static Sprite ring;
        private static Sprite cookieSprite;
        private static Sprite starSprite;
        private static Font font;

        // ================================================================== punto d'ingresso
        public static GameObject Build(HudSources src)
        {
            LoadAssets();

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
            Stretch(safe, 26f);

            // modello delle scritte volanti (resta spento, viene copiato)
            Text floatingTemplate = NewText("FloatingTemplate", canvasObject.transform, "+0", 46, Gold, TextAnchor.MiddleCenter, true);
            floatingTemplate.rectTransform.sizeDelta = new Vector2(220f, 70f);
            floatingTemplate.gameObject.SetActive(false);
            FloatingTextSpawner spawner = canvasObject.AddComponent<FloatingTextSpawner>();
            Wire.Ref(spawner, "template", floatingTemplate);

            Image crosshair = BuildCrosshair(canvasObject.transform);
            OrderCardView cardPrefab = BuildOrderCardPrefab();
            RectTransform ordersContainer = BuildOrdersBar(safe);
            Text pointsText, cookiesText;
            UiPop pointsPop, cookiesPop;
            BuildStats(safe, out pointsText, out cookiesText, out pointsPop, out cookiesPop);
            ToastView toast = BuildToast(safe);
            InteractionPromptView prompts = BuildPrompts(safe, src, crosshair);
            CarriedItemView carried = BuildCarried(safe, src);
            BuildGoalBanner(safe, src);

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
            Wire.Ref(hud, "floatingTexts", spawner);
            Wire.Ref(hud, "pointsIconPop", pointsPop);
            Wire.Ref(hud, "cookiesIconPop", cookiesPop);

            if (prompts == null || carried == null) Debug.LogWarning("[Elfshift] HUD incompleto.");
            return canvasObject;
        }

        // ================================================================== risorse
        private static void LoadAssets()
        {
            string ui = AssetUtil.GeneratedTextures + "/UI";
            panel = SaveSprite(ProceduralTextures.CartoonPanel(128, 40f, 9f), ui + "/ui_panel.png", new Vector4(44f, 44f, 44f, 44f));
            circle = SaveSprite(ProceduralTextures.CartoonCircle(128, 9f), ui + "/ui_circle.png", Vector4.zero);
            ring = SaveSprite(ProceduralTextures.RingSprite(64, 7f), ui + "/ui_ring.png", Vector4.zero);
            cookieSprite = SaveSprite(ProceduralTextures.Cookie(128), ui + "/icon_cookie.png", Vector4.zero);
            starSprite = SaveSprite(ProceduralTextures.Star(128), ui + "/icon_star.png", Vector4.zero);

            font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/LilitaOne-Regular.ttf");
            if (font == null)
            {
                Debug.LogWarning("[Elfshift] Font Assets/Fonts/LilitaOne-Regular.ttf non trovato: uso il font standard.");
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
        }

        private static Sprite SaveSprite(Texture2D texture, string path, Vector4 border)
        {
            ProceduralTextures.SavePng(texture, path, true, border);
            Object.DestroyImmediate(texture);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ================================================================== mirino
        private static Image BuildCrosshair(Transform parent)
        {
            RectTransform rt = NewRect("Crosshair", parent);
            Anchor(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(20f, 20f));
            Image image = rt.gameObject.AddComponent<Image>();
            image.sprite = ring;
            image.color = new Color(1f, 1f, 1f, 0.8f);
            image.raycastTarget = false;
            return image;
        }

        // ================================================================== carta dell'ordine (prefab)
        private static OrderCardView BuildOrderCardPrefab()
        {
            GameObject root = new GameObject("OrderCard", typeof(RectTransform));
            RectTransform rt = (RectTransform)root.transform;
            rt.sizeDelta = new Vector2(280f, 116f);

            // tutta la grafica sta nel figlio "Visual" (si anima senza disturbare il layout)
            RectTransform visual = NewRect("Visual", rt);
            Stretch(visual, 0f);
            CanvasGroup group = visual.gameObject.AddComponent<CanvasGroup>();
            Image bg = AddPanelImage(visual.gameObject, Cream, 7f);

            // fiocco/sigillo rosso in alto a sinistra
            RectTransform seal = NewRect("Seal", visual);
            Anchor(seal, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(10f, -6f), new Vector2(40f, 40f));
            Image sealImage = seal.gameObject.AddComponent<Image>();
            sealImage.sprite = circle;
            sealImage.color = Red;
            RectTransform sealStar = NewRect("Star", seal);
            Stretch(sealStar, 9f);
            Image sealStarImage = sealStar.gameObject.AddComponent<Image>();
            sealStarImage.sprite = starSprite;
            sealStarImage.preserveAspect = true;

            // icona del giocattolo dentro un distintivo tondo
            RectTransform badge = NewRect("IconBadge", visual);
            Anchor(badge, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, -2f), new Vector2(88f, 88f));
            Image badgeImage = badge.gameObject.AddComponent<Image>();
            badgeImage.sprite = circle;
            badgeImage.color = new Color(1f, 0.9f, 0.55f);
            RectTransform iconRt = NewRect("Icon", badge);
            Stretch(iconRt, 10f);
            RawImage icon = iconRt.gameObject.AddComponent<RawImage>();
            icon.color = White;
            icon.raycastTarget = false;

            Text title = NewText("Title", visual, "Giocattolo", 32, Ink, TextAnchor.UpperLeft, false);
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(112f, -48f), new Vector2(-14f, -12f));
            Text child = NewText("Child", visual, "Per Marta", 21, new Color(0.5f, 0.32f, 0.22f), TextAnchor.UpperLeft, false);
            SetRect(child.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(112f, -72f), new Vector2(-14f, -46f));

            // barra del tempo "a pillola"
            RectTransform barBg = NewRect("TimerBack", visual);
            SetRect(barBg, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(112f, 14f), new Vector2(-78f, 36f));
            AddPanelImage(barBg.gameObject, new Color(0.45f, 0.3f, 0.22f, 0.9f), 0f);
            RectTransform fill = NewRect("TimerFill", barBg);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.offsetMin = new Vector2(3f, 3f);
            fill.offsetMax = new Vector2(-3f, -3f);
            Image fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.sprite = panel;
            fillImage.type = Image.Type.Sliced;
            fillImage.color = Green;

            Text time = NewText("Time", visual, "2:00", 28, White, TextAnchor.MiddleRight, true);
            time.rectTransform.anchorMin = new Vector2(1f, 0f);
            time.rectTransform.anchorMax = new Vector2(1f, 0f);
            time.rectTransform.pivot = new Vector2(1f, 0f);
            time.rectTransform.anchoredPosition = new Vector2(-14f, 10f);
            time.rectTransform.sizeDelta = new Vector2(76f, 34f);

            // timbro "OK!" che compare alla consegna
            RectTransform stamp = NewRect("Stamp", visual);
            Anchor(stamp, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(40f, 0f), new Vector2(190f, 90f));
            stamp.localRotation = Quaternion.Euler(0f, 0f, -12f);
            Text stampText = NewText("StampText", stamp, "OK!", 78, new Color(0.35f, 0.85f, 0.4f), TextAnchor.MiddleCenter, true);
            Stretch(stampText.rectTransform, 0f);
            stamp.gameObject.SetActive(false);

            OrderCardView view = root.AddComponent<OrderCardView>();
            Wire.Ref(view, "visual", visual);
            Wire.Ref(view, "visualGroup", group);
            Wire.Ref(view, "icon", icon);
            Wire.Ref(view, "titleText", title);
            Wire.Ref(view, "childText", child);
            Wire.Ref(view, "timeText", time);
            Wire.Ref(view, "timerFill", fill);
            Wire.Ref(view, "timerFillImage", fillImage);
            Wire.Ref(view, "background", bg);
            Wire.Ref(view, "stamp", stamp);
            Wire.Ref(view, "stampText", stampText);

            AssetUtil.EnsureFolder(AssetUtil.UiPrefabs);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, AssetUtil.UiPrefabs + "/OrderCard.prefab");
            Object.DestroyImmediate(root);
            return prefab.GetComponent<OrderCardView>();
        }

        // ================================================================== ordini, statistiche, messaggi
        private static RectTransform BuildOrdersBar(RectTransform parent)
        {
            RectTransform bar = NewRect("OrdersBar", parent);
            Anchor(bar, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(900f, 124f));
            HorizontalLayoutGroup layout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return bar;
        }

        private static void BuildStats(RectTransform parent, out Text pointsText, out Text cookiesText, out UiPop pointsPop, out UiPop cookiesPop)
        {
            pointsText = BuildStatBadge(parent, "Points", starSprite, new Color(1f, 0.85f, 0.35f), 0f, 0.1f, out pointsPop);
            cookiesText = BuildStatBadge(parent, "Cookies", cookieSprite, new Color(0.95f, 0.7f, 0.45f), -74f, 0.2f, out cookiesPop);
        }

        private static Text BuildStatBadge(RectTransform parent, string name, Sprite icon, Color badgeColor, float y, float popDelay, out UiPop iconPop)
        {
            RectTransform pill = NewRect(name, parent);
            Anchor(pill, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, y - 8f), new Vector2(190f, 58f));
            AddPanelImage(pill.gameObject, Cream, 6f);
            UiPop pillPop = pill.gameObject.AddComponent<UiPop>();
            Wire.Float(pillPop, "delay", popDelay);

            RectTransform badge = NewRect("Badge", pill);
            Anchor(badge, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-4f, 2f), new Vector2(76f, 76f));
            Image badgeImage = badge.gameObject.AddComponent<Image>();
            badgeImage.sprite = circle;
            badgeImage.color = badgeColor;
            Shadow s = badge.gameObject.AddComponent<Shadow>();
            s.effectColor = new Color(0f, 0f, 0f, 0.3f);
            s.effectDistance = new Vector2(0f, -5f);
            iconPop = badge.gameObject.AddComponent<UiPop>();
            Wire.Float(iconPop, "delay", popDelay + 0.1f);

            RectTransform iconRt = NewRect("Icon", badge);
            Stretch(iconRt, 7f);
            Image iconImage = iconRt.gameObject.AddComponent<Image>();
            iconImage.sprite = icon;
            iconImage.preserveAspect = true;

            Text value = NewText("Value", pill, "0", 40, Ink, TextAnchor.MiddleRight, false);
            value.rectTransform.anchorMin = new Vector2(0f, 0f);
            value.rectTransform.anchorMax = new Vector2(1f, 1f);
            value.rectTransform.offsetMin = new Vector2(54f, 0f);
            value.rectTransform.offsetMax = new Vector2(-18f, -2f);
            return value;
        }

        private static ToastView BuildToast(RectTransform parent)
        {
            RectTransform toast = NewRect("Toast", parent);
            Anchor(toast, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(1000f, 82f));
            CanvasGroup group = toast.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            AddPanelImage(toast.gameObject, new Color(1f, 0.97f, 0.85f), 8f);
            Text label = NewText("Label", toast, "", 36, Ink, TextAnchor.MiddleCenter, false);
            Stretch(label.rectTransform, 12f);

            ToastView view = toast.gameObject.AddComponent<ToastView>();
            Wire.Ref(view, "label", label);
            Wire.Ref(view, "group", group);
            return view;
        }

        // ================================================================== obiettivo del tutorial
        private static void BuildGoalBanner(RectTransform parent, HudSources src)
        {
            RectTransform banner = NewRect("Goal", parent);
            Anchor(banner, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -136f), new Vector2(980f, 56f));
            AddPanelImage(banner.gameObject, Red, 7f);
            UiWobble wobble = banner.gameObject.AddComponent<UiWobble>();
            Wire.Float(wobble, "rotationDegrees", 0.6f);
            Wire.Float(wobble, "scaleAmount", 0.012f);
            Wire.Float(wobble, "floatPixels", 3f);

            Text label = NewText("Label", banner, "Obiettivo", 28, White, TextAnchor.MiddleCenter, true);
            Stretch(label.rectTransform, 8f);
            UiPop labelPop = label.gameObject.AddComponent<UiPop>();
            Wire.Float(labelPop, "startScale", 0.7f);
            Wire.Float(labelPop, "duration", 0.4f);

            // Il componente sta su un oggetto sempre attivo (il pannello può nascondersi da solo).
            TutorialHintView view = parent.gameObject.AddComponent<TutorialHintView>();
            Wire.Ref(view, "carrier", src.Carrier);
            Wire.Ref(view, "bench", src.Bench);
            Wire.Ref(view, "session", src.Session);
            Wire.Ref(view, "orders", src.Orders);
            Wire.Ref(view, "root", banner.gameObject);
            Wire.Ref(view, "label", label);
            Wire.Ref(view, "labelPop", labelPop);
            Wire.Ref(view, "marker", src.MarkerObject);
            Wire.Ref(view, "shelfTarget", src.ShelfAnchor);
            Wire.Ref(view, "benchTarget", src.Bench != null ? src.Bench.transform : null);
            Wire.Ref(view, "sleighTarget", src.SleighAnchor);
        }

        // ================================================================== suggerimenti contestuali
        private static InteractionPromptView BuildPrompts(RectTransform parent, HudSources src, Image crosshair)
        {
            RectTransform bar = NewRect("Prompts", parent);
            Anchor(bar, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(1000f, 70f));
            HorizontalLayoutGroup layout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 28f;
            layout.childAlignment = TextAnchor.LowerCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            Text pickupKey, pickupLabel, interactKey, interactLabel;
            GameObject pickupChip = BuildChip(bar, "PickupChip", 130f, out pickupKey, out pickupLabel);
            GameObject interactChip = BuildChip(bar, "InteractChip", 66f, out interactKey, out interactLabel);

            // barra di avanzamento a pillola sopra i suggerimenti
            RectTransform progress = NewRect("HoldProgress", parent);
            Anchor(progress, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 96f), new Vector2(400f, 38f));
            AddPanelImage(progress.gameObject, new Color(0.4f, 0.27f, 0.22f), 6f);
            RectTransform fill = NewRect("Fill", progress);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0f, 1f);
            fill.offsetMin = new Vector2(5f, 5f);
            fill.offsetMax = new Vector2(-5f, -5f);
            Image fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.sprite = panel;
            fillImage.type = Image.Type.Sliced;
            fillImage.color = Gold;
            UiWobble progressWobble = progress.gameObject.AddComponent<UiWobble>();
            Wire.Float(progressWobble, "rotationDegrees", 0.8f);
            Wire.Float(progressWobble, "scaleAmount", 0.02f);
            Wire.Float(progressWobble, "speed", 9f);

            Text hint = NewText("FarHint", parent, "Avvicinati", 32, White, TextAnchor.MiddleCenter, true);
            Anchor(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(900f, 48f));
            hint.gameObject.AddComponent<UiPop>();
            hint.gameObject.SetActive(false);

            InteractionPromptView view = parent.gameObject.AddComponent<InteractionPromptView>();
            Wire.Ref(view, "interactor", src.Interactor);
            Wire.Ref(view, "hintLabel", hint);
            Wire.Ref(view, "crosshair", crosshair);
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

        private static GameObject BuildChip(RectTransform parent, string name, float keyMinWidth, out Text key, out Text label)
        {
            RectTransform chip = NewRect(name, parent);
            chip.sizeDelta = new Vector2(10f, 70f);
            AddPanelImage(chip.gameObject, Cream, 7f);
            HorizontalLayoutGroup layout = chip.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(12, 30, 8, 10);
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = chip.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            chip.gameObject.AddComponent<UiPop>();

            // tasto dorato che "respira"
            RectTransform keyBox = NewRect("Key", chip);
            AddPanelImage(keyBox.gameObject, Gold, 5f);
            LayoutElement keyLayout = keyBox.gameObject.AddComponent<LayoutElement>();
            keyLayout.minWidth = keyMinWidth;
            keyLayout.minHeight = 50f;
            keyLayout.preferredHeight = 50f;
            UiWobble keyWobble = keyBox.gameObject.AddComponent<UiWobble>();
            Wire.Float(keyWobble, "rotationDegrees", 0f);
            Wire.Float(keyWobble, "scaleAmount", 0.06f);
            Wire.Float(keyWobble, "speed", 4.5f);
            key = NewText("KeyText", keyBox, "E", 30, Ink, TextAnchor.MiddleCenter, false);
            Stretch(key.rectTransform, 6f);
            key.horizontalOverflow = HorizontalWrapMode.Overflow;

            label = NewText("Label", chip, "Azione", 32, Ink, TextAnchor.MiddleLeft, false);
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            chip.gameObject.SetActive(false);
            return chip.gameObject;
        }

        private static CarriedItemView BuildCarried(RectTransform parent, HudSources src)
        {
            RectTransform root = NewRect("Carried", parent);
            Anchor(root, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 4f), new Vector2(330f, 110f));
            root.gameObject.AddComponent<UiPop>();

            // cartellino con il nome
            RectTransform tag = NewRect("NameTag", root);
            Anchor(tag, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(66f, -4f), new Vector2(262f, 58f));
            AddPanelImage(tag.gameObject, Cream, 6f);
            Text label = NewText("Name", tag, "Oggetto", 32, Ink, TextAnchor.MiddleCenter, false);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(40f, 0f);
            label.rectTransform.offsetMax = new Vector2(-10f, -2f);

            // distintivo tondo con l'icona
            RectTransform badge = NewRect("IconBadge", root);
            Anchor(badge, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(104f, 104f));
            Image badgeImage = badge.gameObject.AddComponent<Image>();
            badgeImage.sprite = circle;
            badgeImage.color = new Color(1f, 0.9f, 0.55f);
            Shadow shadow = badge.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.3f);
            shadow.effectDistance = new Vector2(0f, -6f);
            RectTransform iconRt = NewRect("Icon", badge);
            Stretch(iconRt, 12f);
            RawImage icon = iconRt.gameObject.AddComponent<RawImage>();
            icon.raycastTarget = false;

            // Il componente sta sul Safe Area (sempre attivo); il riquadro si accende da solo.
            CarriedItemView view = parent.gameObject.AddComponent<CarriedItemView>();
            Wire.Ref(view, "carrier", src.Carrier);
            Wire.Ref(view, "icons", src.Icons);
            Wire.Ref(view, "root", root.gameObject);
            Wire.Ref(view, "icon", icon);
            Wire.Ref(view, "label", label);
            root.gameObject.SetActive(false);
            return view;
        }

        // ================================================================== piccoli aiuti per uGUI
        private static RectTransform NewRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>Aggiunge a un oggetto l'immagine "adesivo" (9-slice colorabile) con ombra sotto.</summary>
        private static Image AddPanelImage(GameObject go, Color tint, float shadowOffset)
        {
            Image image = go.AddComponent<Image>();
            image.sprite = panel;
            image.type = Image.Type.Sliced;
            image.color = tint;
            if (shadowOffset > 0f)
            {
                Shadow s = go.AddComponent<Shadow>();
                s.effectColor = new Color(0f, 0f, 0f, 0.32f);
                s.effectDistance = new Vector2(0f, -shadowOffset);
            }
            return image;
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

        /// <summary>Testo nel font cartoon. Con "outlined" ha contorno marrone spesso (per testi chiari su fondi colorati).</summary>
        private static Text NewText(string name, Transform parent, string content, int size, Color color, TextAnchor anchor, bool outlined)
        {
            RectTransform rt = NewRect(name, parent);
            Text text = rt.gameObject.AddComponent<Text>();
            text.font = font;
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = anchor;
            text.fontStyle = FontStyle.Normal;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            if (outlined)
            {
                Outline o = rt.gameObject.AddComponent<Outline>();
                o.effectColor = Ink;
                o.effectDistance = new Vector2(3f, -3f);
                Shadow s = rt.gameObject.AddComponent<Shadow>();
                s.effectColor = new Color(0f, 0f, 0f, 0.35f);
                s.effectDistance = new Vector2(0f, -5f);
            }
            return text;
        }
    }
}
