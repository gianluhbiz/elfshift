using Elfshift.Core;
using Elfshift.Orders;
using Elfshift.Player;
using Elfshift.Production;
using Elfshift.Progression;
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
        public CraftingStation[] Stations = new CraftingStation[0];
        public ItemShelf[] Shelves = new ItemShelf[0];
        public Sleigh Sleigh;
        public Mailbox Mailbox;
        public Fireplace Fireplace;
        public DayCycle Day;
        public Transform MarkerObject;
    }

    /// <summary>
    /// Costruisce l'interfaccia di gioco in stile "cozy" pulito: pannelli color carta con angoli tondi e ombra
    /// morbida (niente contorni spessi), testo marrone caldo nel font arrotondato Nunito, icone disegnate in 3D,
    /// animazioni brevi e leggere. Il Canvas scala rispetto a 1280x720 e gli elementi sono ancorati ai bordi.
    /// </summary>
    public static class HudBuilder
    {
        // ------------------------------------------------------------------ colori dell'interfaccia
        private static readonly Color Paper = new Color(1f, 0.969f, 0.918f, 0.97f);
        private static readonly Color PaperDim = new Color(0.953f, 0.902f, 0.827f, 1f);
        private static readonly Color Ink = new Color(0.29f, 0.227f, 0.2f, 1f);
        private static readonly Color InkSoft = new Color(0.55f, 0.455f, 0.4f, 1f);
        private static readonly Color Red = new Color(0.85f, 0.34f, 0.31f, 1f);
        private static readonly Color Green = new Color(0.49f, 0.73f, 0.43f, 1f);
        private static readonly Color Amber = new Color(0.98f, 0.76f, 0.33f, 1f);
        private static readonly Color Track = new Color(0.918f, 0.859f, 0.784f, 1f);
        private static readonly Color Glass = new Color(0.17f, 0.13f, 0.12f, 0.55f);
        private static readonly Color White = Color.white;

        // Raggio degli angoli dello sprite del pannello (pixel), usato per scalarlo con pixelsPerUnitMultiplier.
        private const float PanelSpriteRadius = 24f;

        private static Sprite panel;     // pannello carta con ombra morbida (9-slice)
        private static Sprite plain;     // rettangolo tondo senza ombra (barre, pillole scure)
        private static Sprite disc;      // cerchio pieno
        private static Sprite softDisc;  // cerchio con ombra
        private static Sprite keycap;    // tasto della tastiera
        private static Sprite cookieSprite;
        private static Sprite starSprite;
        private static Sprite flameSprite;
        private static Sprite letterSprite;
        private static Font font;        // Nunito SemiBold: testi
        private static Font fontBold;    // Nunito Bold: numeri e titoli

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
            Stretch(safe, 24f);

            // modello delle scritte volanti (resta spento, viene copiato)
            Text floatingTemplate = NewText("FloatingTemplate", canvasObject.transform, "+0", 30, Amber, TextAnchor.MiddleCenter, true, true);
            floatingTemplate.rectTransform.sizeDelta = new Vector2(200f, 50f);
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
            BuildDayClock(safe, src);
            BuildWarmth(safe, src);
            BuildMailIndicator(safe, src);
            BuildStreak(safe, src);
            BuildResults(canvasObject.transform, src);

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
            Wire.Ref(hud, "fireplace", src.Fireplace);
            Wire.Ref(hud, "day", src.Day);

            if (prompts == null || carried == null) Debug.LogWarning("[Elfshift] HUD incompleto.");
            return canvasObject;
        }

        // ================================================================== risorse
        private static void LoadAssets()
        {
            string ui = AssetUtil.GeneratedTextures + "/UI";
            float panelBorder = ProceduralTextures.SoftPanelPad + PanelSpriteRadius + 2f;
            panel = SaveSprite(ProceduralTextures.SoftPanel(128, PanelSpriteRadius, ProceduralTextures.SoftPanelPad), ui + "/ui_panel_soft.png",
                new Vector4(panelBorder, panelBorder, panelBorder, panelBorder));
            plain = SaveSprite(ProceduralTextures.RoundedRect(64, PanelSpriteRadius), ui + "/ui_rounded.png", new Vector4(26f, 26f, 26f, 26f));
            disc = SaveSprite(ProceduralTextures.Disc(64), ui + "/ui_disc.png", Vector4.zero);
            softDisc = SaveSprite(ProceduralTextures.SoftDisc(128), ui + "/ui_disc_soft.png", Vector4.zero);
            keycap = SaveSprite(ProceduralTextures.Keycap(64, 14f, 5f), ui + "/ui_keycap.png", new Vector4(18f, 18f, 18f, 18f));

            // Icone disegnate in 3D con Blender (Tools/Blender/ui_icons.py); se mancano uso quelle procedurali.
            starSprite = LoadIcon("Assets/UI/Icons/icon_star.png");
            if (starSprite == null) starSprite = SaveSprite(ProceduralTextures.Star(128), ui + "/icon_star.png", Vector4.zero);
            cookieSprite = LoadIcon("Assets/UI/Icons/icon_cookie.png");
            if (cookieSprite == null) cookieSprite = SaveSprite(ProceduralTextures.Cookie(128), ui + "/icon_cookie.png", Vector4.zero);
            flameSprite = LoadIcon("Assets/UI/Icons/icon_flame.png");
            if (flameSprite == null) flameSprite = starSprite;
            letterSprite = LoadIcon("Assets/UI/Icons/icon_letter.png");
            if (letterSprite == null) letterSprite = starSprite;

            font = LoadFont("Assets/Fonts/Nunito-SemiBold.ttf");
            fontBold = LoadFont("Assets/Fonts/Nunito-Bold.ttf");
            if (fontBold == null) fontBold = font;
        }

        private static Font LoadFont(string path)
        {
            Font f = AssetDatabase.LoadAssetAtPath<Font>(path);
            if (f == null)
            {
                Debug.LogWarning("[Elfshift] Font " + path + " non trovato: uso il font standard.");
                f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            return f;
        }

        /// <summary>Carica un'icona PNG del progetto impostandola come Sprite (se serve).</summary>
        private static Sprite LoadIcon(string path)
        {
            if (!System.IO.File.Exists(path)) return null;
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
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
            Anchor(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(7f, 7f));
            Image image = rt.gameObject.AddComponent<Image>();
            image.sprite = disc;
            image.color = new Color(1f, 1f, 1f, 0.85f);
            image.raycastTarget = false;
            Shadow s = rt.gameObject.AddComponent<Shadow>();
            s.effectColor = new Color(0f, 0f, 0f, 0.35f);
            s.effectDistance = new Vector2(0f, -1f);
            return image;
        }

        // ================================================================== carta dell'ordine (prefab)
        private static OrderCardView BuildOrderCardPrefab()
        {
            GameObject root = new GameObject("OrderCard", typeof(RectTransform));
            RectTransform rt = (RectTransform)root.transform;
            rt.sizeDelta = new Vector2(244f, 84f);

            // tutta la grafica sta nel figlio "Visual" (si anima senza disturbare il layout)
            RectTransform visual = NewRect("Visual", rt);
            Stretch(visual, 0f);
            CanvasGroup group = visual.gameObject.AddComponent<CanvasGroup>();
            Image bg = AddPanel(visual, Paper, 18f);

            // icona del giocattolo dentro un tondo color carta più scura
            RectTransform badge = NewRect("IconBadge", visual);
            Anchor(badge, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(11f, 0f), new Vector2(62f, 62f));
            Image badgeImage = badge.gameObject.AddComponent<Image>();
            badgeImage.sprite = disc;
            badgeImage.color = PaperDim;
            RectTransform iconRt = NewRect("Icon", badge);
            Stretch(iconRt, 5f);
            RawImage icon = iconRt.gameObject.AddComponent<RawImage>();
            icon.color = White;
            icon.raycastTarget = false;

            // piccolo sigillo rosso in alto a sinistra (la "ceralacca" della letterina)
            RectTransform seal = NewRect("Seal", visual);
            Anchor(seal, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(12f, -12f), new Vector2(20f, 20f));
            Image sealImage = seal.gameObject.AddComponent<Image>();
            sealImage.sprite = disc;
            sealImage.color = Red;
            RectTransform sealStar = NewRect("Star", seal);
            Stretch(sealStar, 4f);
            Image sealStarImage = sealStar.gameObject.AddComponent<Image>();
            sealStarImage.sprite = starSprite;
            sealStarImage.preserveAspect = true;
            sealStarImage.color = new Color(1f, 0.95f, 0.85f);

            Text title = NewText("Title", visual, "Giocattolo", 20, Ink, TextAnchor.UpperLeft, true, false);
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(84f, -38f), new Vector2(-58f, -11f));
            title.horizontalOverflow = HorizontalWrapMode.Overflow;
            Text child = NewText("Child", visual, "Per Marta", 15, InkSoft, TextAnchor.UpperLeft, false, false);
            SetRect(child.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(84f, -58f), new Vector2(-14f, -37f));

            Text time = NewText("Time", visual, "2:00", 15, InkSoft, TextAnchor.UpperRight, true, false);
            SetRect(time.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-60f, -34f), new Vector2(-14f, -12f));

            // barra del tempo sottile
            RectTransform barBg = NewRect("TimerBack", visual);
            SetRect(barBg, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(84f, 14f), new Vector2(-16f, 22f));
            Image barBgImage = barBg.gameObject.AddComponent<Image>();
            barBgImage.sprite = plain;
            barBgImage.type = Image.Type.Sliced;
            barBgImage.pixelsPerUnitMultiplier = PanelSpriteRadius / 4f;
            barBgImage.color = Track;
            RectTransform fill = NewRect("TimerFill", barBg);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            Image fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.sprite = plain;
            fillImage.type = Image.Type.Sliced;
            fillImage.pixelsPerUnitMultiplier = PanelSpriteRadius / 4f;
            fillImage.color = Green;

            // timbro che compare alla consegna
            RectTransform stamp = NewRect("Stamp", visual);
            Anchor(stamp, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(34f, -2f), new Vector2(180f, 44f));
            stamp.localRotation = Quaternion.Euler(0f, 0f, -8f);
            Text stampText = NewText("StampText", stamp, UiText.StampDone, 26, Green, TextAnchor.MiddleCenter, true, false);
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
            Anchor(bar, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(820f, 90f));
            HorizontalLayoutGroup layout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return bar;
        }

        /// <summary>Una sola pillola in alto a sinistra: stella + punti, biscotto + biscotti.</summary>
        private static void BuildStats(RectTransform parent, out Text pointsText, out Text cookiesText, out UiPop pointsPop, out UiPop cookiesPop)
        {
            RectTransform pill = NewRect("Stats", parent);
            Anchor(pill, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -2f), new Vector2(236f, 48f));
            AddPanel(pill, Paper, 24f);
            UiPop pillPop = pill.gameObject.AddComponent<UiPop>();
            Wire.Float(pillPop, "delay", 0.1f);

            pointsText = BuildStat(pill, "Points", starSprite, 10f, out pointsPop);
            cookiesText = BuildStat(pill, "Cookies", cookieSprite, 124f, out cookiesPop);

            // sottile separatore fra le due voci
            RectTransform divider = NewRect("Divider", pill);
            Anchor(divider, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(117f, 0f), new Vector2(2f, 24f));
            Image d = divider.gameObject.AddComponent<Image>();
            d.sprite = plain;
            d.type = Image.Type.Sliced;
            d.pixelsPerUnitMultiplier = PanelSpriteRadius;
            d.color = Track;
        }

        private static Text BuildStat(RectTransform pill, string name, Sprite icon, float x, out UiPop iconPop)
        {
            RectTransform iconRt = NewRect(name + "Icon", pill);
            Anchor(iconRt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x + 17f, 0f), new Vector2(36f, 36f));
            Image iconImage = iconRt.gameObject.AddComponent<Image>();
            iconImage.sprite = icon;
            iconImage.preserveAspect = true;
            iconPop = iconRt.gameObject.AddComponent<UiPop>();
            Wire.Float(iconPop, "delay", 0.2f);

            Text value = NewText(name, pill, "0", 22, Ink, TextAnchor.MiddleLeft, true, false);
            SetRect(value.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(x + 40f, 0f), new Vector2(x + 104f, -1f));
            value.horizontalOverflow = HorizontalWrapMode.Overflow;
            return value;
        }

        private static ToastView BuildToast(RectTransform parent)
        {
            RectTransform toast = NewRect("Toast", parent);
            Anchor(toast, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(760f, 58f));
            CanvasGroup group = toast.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            AddPanel(toast, Paper, 22f);
            Text label = NewText("Label", toast, "", 21, Ink, TextAnchor.MiddleCenter, false, false);
            Stretch(label.rectTransform, 14f);

            ToastView view = toast.gameObject.AddComponent<ToastView>();
            Wire.Ref(view, "label", label);
            Wire.Ref(view, "group", group);
            return view;
        }

        // ================================================================== obiettivo del tutorial
        private static void BuildGoalBanner(RectTransform parent, HudSources src)
        {
            // pillola che si allarga da sola in base al testo
            RectTransform banner = NewRect("Goal", parent);
            Anchor(banner, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -102f), new Vector2(600f, 42f));
            AddPanel(banner, Paper, 21f);
            HorizontalLayoutGroup layout = banner.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 22, 6, 6);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = banner.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            UiWobble wobble = banner.gameObject.AddComponent<UiWobble>();
            Wire.Float(wobble, "rotationDegrees", 0f);
            Wire.Float(wobble, "scaleAmount", 0f);
            Wire.Float(wobble, "floatPixels", 2f);

            // etichetta rossa "Obiettivo"
            RectTransform tag = NewRect("Tag", banner);
            LayoutElement tagLayout = tag.gameObject.AddComponent<LayoutElement>();
            tagLayout.preferredWidth = 92f;
            tagLayout.preferredHeight = 30f;
            Image tagImage = tag.gameObject.AddComponent<Image>();
            tagImage.sprite = plain;
            tagImage.type = Image.Type.Sliced;
            tagImage.pixelsPerUnitMultiplier = PanelSpriteRadius / 15f;
            tagImage.color = Red;
            Text tagText = NewText("TagText", tag, UiText.GoalTitle, 15, White, TextAnchor.MiddleCenter, true, false);
            Stretch(tagText.rectTransform, 0f);

            Text label = NewText("Label", banner, "Obiettivo", 18, Ink, TextAnchor.MiddleLeft, false, false);
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            UiPop labelPop = label.gameObject.AddComponent<UiPop>();
            Wire.Float(labelPop, "startScale", 0.92f);
            Wire.Float(labelPop, "duration", 0.3f);

            // Il componente sta su un oggetto sempre attivo (il pannello può nascondersi da solo).
            GoalGuideView view = parent.gameObject.AddComponent<GoalGuideView>();
            Wire.Ref(view, "orders", src.Orders);
            Wire.Ref(view, "carrier", src.Carrier);
            Wire.Refs(view, "stations", src.Stations);
            Wire.Refs(view, "shelves", src.Shelves);
            Wire.Ref(view, "sleigh", src.Sleigh);
            Wire.Ref(view, "mailbox", src.Mailbox);
            Wire.Ref(view, "fireplace", src.Fireplace);
            Wire.Ref(view, "root", banner.gameObject);
            Wire.Ref(view, "label", label);
            Wire.Ref(view, "labelPop", labelPop);
            Wire.Ref(view, "marker", src.MarkerObject);
        }

        // ================================================================== giornata, calore, posta, serie
        /// <summary>In alto a destra: "Giorno 1" e il tempo che resta, con un piccolo quadrante che si svuota.</summary>
        private static void BuildDayClock(RectTransform parent, HudSources src)
        {
            RectTransform pill = NewRect("DayClock", parent);
            Anchor(pill, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0f, -2f), new Vector2(176f, 48f));
            AddPanel(pill, Paper, 24f);
            pill.gameObject.AddComponent<UiPop>();

            RectTransform dialBack = NewRect("DialBack", pill);
            Anchor(dialBack, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(26f, 0f), new Vector2(32f, 32f));
            Image back = dialBack.gameObject.AddComponent<Image>();
            back.sprite = disc;
            back.color = Track;
            RectTransform dialRt = NewRect("Dial", dialBack);
            Stretch(dialRt, 3f);
            Image dial = dialRt.gameObject.AddComponent<Image>();
            dial.sprite = disc;
            dial.color = Amber;
            dial.type = Image.Type.Filled;
            dial.fillMethod = Image.FillMethod.Radial360;
            dial.fillOrigin = 2; // dall'alto
            dial.fillClockwise = true;
            dial.fillAmount = 1f;

            Text dayLabel = NewText("DayLabel", pill, UiText.DayTitle + " 1", 13, InkSoft, TextAnchor.UpperLeft, true, false);
            SetRect(dayLabel.rectTransform, Vector2.zero, Vector2.one, new Vector2(0f, 1f), new Vector2(50f, 0f), new Vector2(-10f, -5f));
            Text time = NewText("Time", pill, "8:00", 21, Ink, TextAnchor.LowerLeft, true, false);
            SetRect(time.rectTransform, Vector2.zero, Vector2.one, new Vector2(0f, 0f), new Vector2(50f, 4f), new Vector2(-10f, 0f));

            DayClockView view = pill.gameObject.AddComponent<DayClockView>();
            Wire.Ref(view, "day", src.Day);
            Wire.Ref(view, "dayLabel", dayLabel);
            Wire.Ref(view, "timeLabel", time);
            Wire.Ref(view, "dial", dial);
        }

        /// <summary>Sotto l'orologio: fiammella e barretta del calore del camino.</summary>
        private static void BuildWarmth(RectTransform parent, HudSources src)
        {
            RectTransform pill = NewRect("Warmth", parent);
            Anchor(pill, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0f, -60f), new Vector2(176f, 40f));
            AddPanel(pill, Paper, 20f);

            RectTransform icon = NewRect("Flame", pill);
            Anchor(icon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(24f, 1f), new Vector2(30f, 30f));
            Image iconImage = icon.gameObject.AddComponent<Image>();
            iconImage.sprite = flameSprite;
            iconImage.preserveAspect = true;

            RectTransform bar = NewRect("Bar", pill);
            SetRect(bar, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(46f, -5f), new Vector2(-16f, 5f));
            Image barBg = bar.gameObject.AddComponent<Image>();
            barBg.sprite = plain;
            barBg.type = Image.Type.Sliced;
            barBg.pixelsPerUnitMultiplier = PanelSpriteRadius / 5f;
            barBg.color = Track;
            RectTransform fill = NewRect("Fill", bar);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0.7f, 1f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            Image fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.sprite = plain;
            fillImage.type = Image.Type.Sliced;
            fillImage.pixelsPerUnitMultiplier = PanelSpriteRadius / 5f;
            fillImage.color = Amber;

            WarmthView view = pill.gameObject.AddComponent<WarmthView>();
            Wire.Ref(view, "fireplace", src.Fireplace);
            Wire.Ref(view, "fill", fill);
            Wire.Ref(view, "fillImage", fillImage);
            Wire.Ref(view, "icon", icon);
            if (src.Fireplace == null) pill.gameObject.SetActive(false);
        }

        /// <summary>Busta con il numero di letterine che aspettano nella cassetta (sotto il calore).</summary>
        private static void BuildMailIndicator(RectTransform parent, HudSources src)
        {
            GameObject holder = NewRect("MailHolder", parent).gameObject;
            RectTransform holderRt = (RectTransform)holder.transform;
            Stretch(holderRt, 0f);
            RectTransform pill = NewRect("Mail", holderRt);
            Anchor(pill, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0f, -110f), new Vector2(96f, 40f));
            AddPanel(pill, Paper, 20f);
            UiPop pop = pill.gameObject.AddComponent<UiPop>();
            RectTransform icon = NewRect("Letter", pill);
            Anchor(icon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(26f, 0f), new Vector2(32f, 32f));
            Image iconImage = icon.gameObject.AddComponent<Image>();
            iconImage.sprite = letterSprite;
            iconImage.preserveAspect = true;
            Text count = NewText("Count", pill, "1", 20, Red, TextAnchor.MiddleLeft, true, false);
            SetRect(count.rectTransform, Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), new Vector2(50f, 0f), new Vector2(-8f, -1f));

            MailIndicatorView view = holder.AddComponent<MailIndicatorView>();
            Wire.Ref(view, "orders", src.Orders);
            Wire.Ref(view, "root", pill.gameObject);
            Wire.Ref(view, "countText", count);
            Wire.Ref(view, "pop", pop);
            pill.gameObject.SetActive(false);
        }

        /// <summary>Sotto punti e biscotti: "Serie x1.5" quando si consegna più volte di fila.</summary>
        private static void BuildStreak(RectTransform parent, HudSources src)
        {
            GameObject holder = NewRect("StreakHolder", parent).gameObject;
            Stretch((RectTransform)holder.transform, 0f);
            RectTransform pill = NewRect("Streak", (RectTransform)holder.transform);
            Anchor(pill, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(8f, -60f), new Vector2(132f, 36f));
            AddPanel(pill, Red, 18f);
            UiPop pop = pill.gameObject.AddComponent<UiPop>();
            Text label = NewText("Label", pill, UiText.StreakTitle + " x1.25", 17, White, TextAnchor.MiddleCenter, true, false);
            Stretch(label.rectTransform, 2f);

            StreakView view = holder.AddComponent<StreakView>();
            Wire.Ref(view, "session", src.Session);
            Wire.Ref(view, "root", pill.gameObject);
            Wire.Ref(view, "label", label);
            Wire.Ref(view, "pop", pop);
            pill.gameObject.SetActive(false);
        }

        /// <summary>Riepilogo di fine giornata al centro dello schermo (nascosto finché la giornata non finisce).</summary>
        private static void BuildResults(Transform canvas, HudSources src)
        {
            GameObject holder = NewRect("ResultsHolder", canvas).gameObject;
            Stretch((RectTransform)holder.transform, 0f);

            RectTransform dim = NewRect("Results", (RectTransform)holder.transform);
            Stretch(dim, 0f);
            Image dimImage = dim.gameObject.AddComponent<Image>();
            dimImage.color = new Color(0.08f, 0.06f, 0.1f, 0.45f);
            CanvasGroup group = dim.gameObject.AddComponent<CanvasGroup>();

            RectTransform card = NewRect("Card", dim);
            Anchor(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(540f, 360f));
            AddPanel(card, Paper, 28f);

            Text title = NewText("Title", card, UiText.ResultsTitle, 30, Ink, TextAnchor.UpperCenter, true, false);
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(20f, -66f), new Vector2(-20f, -24f));

            Image[] stars = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                RectTransform star = NewRect("Star" + (i + 1), card);
                float size = i == 1 ? 84f : 66f;
                Anchor(star, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 92f, i == 1 ? -124f : -132f), new Vector2(size, size));
                Image img = star.gameObject.AddComponent<Image>();
                img.sprite = starSprite;
                img.preserveAspect = true;
                stars[i] = img;
            }

            Text points = NewText("Points", card, "0", 44, Ink, TextAnchor.MiddleCenter, true, false);
            SetRect(points.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(20f, -232f), new Vector2(-20f, -176f));
            Text pointsCaption = NewText("PointsCaption", card, "punti", 15, InkSoft, TextAnchor.UpperCenter, false, false);
            SetRect(pointsCaption.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(20f, -256f), new Vector2(-20f, -232f));
            Text details = NewText("Details", card, "", 16, InkSoft, TextAnchor.MiddleCenter, false, false);
            SetRect(details.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(20f, 62f), new Vector2(-20f, 92f));

            RectTransform hintPill = NewRect("HintPill", card);
            Anchor(hintPill, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(360f, 36f));
            Image hintBg = hintPill.gameObject.AddComponent<Image>();
            hintBg.sprite = plain;
            hintBg.type = Image.Type.Sliced;
            hintBg.pixelsPerUnitMultiplier = PanelSpriteRadius / 18f;
            hintBg.color = Green;
            Text hint = NewText("Hint", hintPill, UiText.ResultsHint, 16, White, TextAnchor.MiddleCenter, true, false);
            Stretch(hint.rectTransform, 0f);
            UiWobble wobble = hintPill.gameObject.AddComponent<UiWobble>();
            Wire.Float(wobble, "rotationDegrees", 0f);
            Wire.Float(wobble, "scaleAmount", 0.03f);
            Wire.Float(wobble, "floatPixels", 0f);

            ResultsView view = holder.AddComponent<ResultsView>();
            Wire.Ref(view, "day", src.Day);
            Wire.Ref(view, "session", src.Session);
            Wire.Ref(view, "root", dim.gameObject);
            Wire.Ref(view, "group", group);
            Wire.Ref(view, "pointsText", points);
            Wire.Ref(view, "detailsText", details);
            Wire.Refs(view, "stars", stars);
            dim.gameObject.SetActive(false);
        }

        // ================================================================== suggerimenti contestuali
        private static InteractionPromptView BuildPrompts(RectTransform parent, HudSources src, Image crosshair)
        {
            RectTransform bar = NewRect("Prompts", parent);
            Anchor(bar, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(1000f, 46f));
            HorizontalLayoutGroup layout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.LowerCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            Text pickupKey, pickupLabel, interactKey, interactLabel;
            GameObject pickupChip = BuildChip(bar, "PickupChip", 68f, out pickupKey, out pickupLabel);
            GameObject interactChip = BuildChip(bar, "InteractChip", 32f, out interactKey, out interactLabel);

            // barra di avanzamento sottile sopra i suggerimenti
            RectTransform progress = NewRect("HoldProgress", parent);
            Anchor(progress, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 66f), new Vector2(260f, 10f));
            Image progressBg = progress.gameObject.AddComponent<Image>();
            progressBg.sprite = plain;
            progressBg.type = Image.Type.Sliced;
            progressBg.pixelsPerUnitMultiplier = PanelSpriteRadius / 5f;
            progressBg.color = Glass;
            RectTransform fill = NewRect("Fill", progress);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0f, 1f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            Image fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.sprite = plain;
            fillImage.type = Image.Type.Sliced;
            fillImage.pixelsPerUnitMultiplier = PanelSpriteRadius / 5f;
            fillImage.color = Amber;

            Text hint = NewText("FarHint", parent, "Avvicinati", 19, White, TextAnchor.MiddleCenter, false, true);
            Anchor(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 96f), new Vector2(900f, 34f));
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

        /// <summary>Suggerimento "[tasto] azione": pillola scura semitrasparente, tasto chiaro, testo bianco.</summary>
        private static GameObject BuildChip(RectTransform parent, string name, float keyMinWidth, out Text key, out Text label)
        {
            RectTransform chip = NewRect(name, parent);
            chip.sizeDelta = new Vector2(10f, 46f);
            Image bg = chip.gameObject.AddComponent<Image>();
            bg.sprite = plain;
            bg.type = Image.Type.Sliced;
            bg.pixelsPerUnitMultiplier = PanelSpriteRadius / 23f;
            bg.color = Glass;
            HorizontalLayoutGroup layout = chip.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(7, 20, 7, 7);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = chip.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            UiPop pop = chip.gameObject.AddComponent<UiPop>();
            Wire.Float(pop, "startScale", 0.85f);

            RectTransform keyBox = NewRect("Key", chip);
            Image keyImage = keyBox.gameObject.AddComponent<Image>();
            keyImage.sprite = keycap;
            keyImage.type = Image.Type.Sliced;
            keyImage.pixelsPerUnitMultiplier = 14f / 9f;
            keyImage.color = White;
            LayoutElement keyLayout = keyBox.gameObject.AddComponent<LayoutElement>();
            keyLayout.minWidth = keyMinWidth;
            keyLayout.minHeight = 32f;
            keyLayout.preferredHeight = 32f;
            key = NewText("KeyText", keyBox, "E", 15, Ink, TextAnchor.MiddleCenter, true, false);
            SetRect(key.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(6f, 4f), new Vector2(-6f, -1f));
            key.horizontalOverflow = HorizontalWrapMode.Overflow;

            label = NewText("Label", chip, "Azione", 19, White, TextAnchor.MiddleLeft, false, false);
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            chip.gameObject.SetActive(false);
            return chip.gameObject;
        }

        private static CarriedItemView BuildCarried(RectTransform parent, HudSources src)
        {
            RectTransform root = NewRect("Carried", parent);
            Anchor(root, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 2f), new Vector2(300f, 84f));
            UiPop pop = root.gameObject.AddComponent<UiPop>();
            Wire.Float(pop, "startScale", 0.85f);

            // cartellino con il nome, a destra del tondo
            RectTransform tag = NewRect("NameTag", root);
            Anchor(tag, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(58f, -2f), new Vector2(200f, 48f));
            AddPanel(tag, Paper, 24f);
            Text caption = NewText("Caption", tag, UiText.CarriedTitle, 12, InkSoft, TextAnchor.UpperLeft, true, false);
            SetRect(caption.rectTransform, Vector2.zero, Vector2.one, new Vector2(0f, 1f), new Vector2(36f, 0f), new Vector2(-12f, -5f));
            Text label = NewText("Name", tag, "Oggetto", 19, Ink, TextAnchor.LowerLeft, true, false);
            SetRect(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(0f, 0f), new Vector2(36f, 6f), new Vector2(-12f, 0f));
            label.horizontalOverflow = HorizontalWrapMode.Overflow;

            // tondo con l'icona dell'oggetto
            RectTransform badge = NewRect("IconBadge", root);
            Anchor(badge, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(-8f, 0f), new Vector2(92f, 92f));
            Image badgeImage = badge.gameObject.AddComponent<Image>();
            badgeImage.sprite = softDisc;
            badgeImage.color = Paper;
            RectTransform iconRt = NewRect("Icon", badge);
            Stretch(iconRt, 18f);
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

        /// <summary>
        /// Aggiunge il pannello color carta con ombra morbida come primo figlio "Bg" (così sta dietro al contenuto).
        /// Lo sprite contiene anche l'ombra attorno al pannello: il figlio viene quindi allargato di quel margine.
        /// "radius" è il raggio degli angoli voluto, in pixel dell'interfaccia.
        /// </summary>
        private static Image AddPanel(RectTransform target, Color tint, float radius)
        {
            RectTransform bg = NewRect("Bg", target);
            bg.SetAsFirstSibling();
            float multiplier = PanelSpriteRadius / Mathf.Max(1f, radius);
            float pad = ProceduralTextures.SoftPanelPad / multiplier;
            bg.anchorMin = Vector2.zero;
            bg.anchorMax = Vector2.one;
            bg.offsetMin = new Vector2(-pad, -pad);
            bg.offsetMax = new Vector2(pad, pad);
            LayoutElement ignore = bg.gameObject.AddComponent<LayoutElement>();
            ignore.ignoreLayout = true;
            Image image = bg.gameObject.AddComponent<Image>();
            image.sprite = panel;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = multiplier;
            image.color = tint;
            image.raycastTarget = false;
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

        /// <summary>Testo in Nunito. "bold" usa il peso più deciso; "shadow" aggiunge una leggera ombra (per testi su sfondo 3D).</summary>
        private static Text NewText(string name, Transform parent, string content, int size, Color color, TextAnchor anchor, bool bold, bool shadow)
        {
            RectTransform rt = NewRect(name, parent);
            Text text = rt.gameObject.AddComponent<Text>();
            text.font = bold ? fontBold : font;
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = anchor;
            text.fontStyle = FontStyle.Normal;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            if (shadow)
            {
                Shadow s = rt.gameObject.AddComponent<Shadow>();
                s.effectColor = new Color(0.1f, 0.06f, 0.05f, 0.45f);
                s.effectDistance = new Vector2(0f, -2f);
            }
            return text;
        }
    }
}
