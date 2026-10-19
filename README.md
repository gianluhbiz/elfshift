# Il Laboratorio di Babbo Natale — prototipo 3D giocabile

Gioco natalizio 3D in **prima persona** per PC (Windows), sviluppato con **Unity 6** e C#.
Tu gestisci il laboratorio degli elfi al Polo Nord: arrivano le letterine dei bambini, costruisci i giocattoli e li porti alla slitta prima che scada il tempo.

> **Stato:** prototipo giocabile in prima persona (Fase 1 completa e parte della Fase 2): *ritira le letterine → prendi i materiali → costruisci → dipingi → incarta → carica sulla slitta*, tenendo acceso il camino, in una giornata di 8 minuti con le stelle finali.

---

## 1. Cosa installare (una sola volta)

| Cosa | Dove | Note |
|---|---|---|
| **Unity Hub** | <https://unity.com/download> | Gratuito (licenza *Personal*). Crea un account Unity quando richiesto. |
| **Unity 6.3 LTS** (6000.3.x) | Dentro Unity Hub → *Installs* → *Install Editor* → scheda **Official releases** → scegli la versione con l'etichetta **LTS** più recente | Se Hub propone moduli, spunta **Windows Build Support (Mono)**. |
| Editor di codice (facoltativo) | Visual Studio Community oppure VS Code | Serve solo se vuoi leggere/modificare gli script. Unity Hub può installare Visual Studio insieme all'Editor. |

Il progetto funziona anche con Unity 6.0 LTS, ma 6.3 LTS è la versione consigliata (supportata fino a fine 2027).

## 2. Creare il progetto Unity

1. Apri **Unity Hub** → **Projects** → **New project**.
2. Scegli l'editor **6000.3.x** e il template **Universal 3D** (a volte chiamato *3D (URP)*). Se il template va scaricato, lascia che lo faccia.
3. Nome: `LaboratorioBabboNatale`. Scegli una cartella e premi **Create project**. Attendi che l'Editor si apra.

> È importante usare il template **Universal 3D**: contiene già *URP* (la grafica morbida con ombre e bagliore) e il *nuovo Input System*.

## 3. Copiare i file del gioco nel progetto

1. Scarica questo repository (pulsante verde **Code → Download ZIP** su GitHub, branch `claude/friendly-planck-b1zodj`) e decomprimilo. In alternativa: `git clone -b claude/friendly-planck-b1zodj <indirizzo del repository>`.
2. Apri la cartella del progetto Unity che hai creato (da Unity Hub: icona `…` → *Show in Explorer*).
3. Copia **tutto il contenuto** della cartella `Assets` del repository dentro la cartella `Assets` del tuo progetto. Se il sistema chiede di unire le cartelle, scegli **Unisci / Sostituisci**.
4. Torna alla finestra di Unity: parte da sola una compilazione (barra in basso a destra). Aspetta che finisca.
   - Se compare una finestra su "*Input System… enable new input backends*", premi **Yes** e lascia riavviare Unity.
5. Apri la scheda **Console** (menu *Window → General → Console*): non devono esserci messaggi **rossi**. Se ci sono, vedi la sezione *Problemi comuni*.

### Aggiornare più velocemente (consigliato): collegare il progetto a Git

Invece di riscaricare lo ZIP ogni volta, collega **una volta sola** la cartella del progetto al repository.

1. Installa **Git** da <https://git-scm.com/download/win> (tutte le opzioni predefinite vanno bene). Se il repository è privato, al primo uso ti chiederà di accedere a GitHub.
2. Chiudi Unity. Apri la cartella del progetto, clicca nella barra dell'indirizzo, scrivi `cmd` e premi Invio. Nella finestra nera incolla, una riga alla volta:

```
git init
git remote add origin https://github.com/gianluhbiz/elfshift.git
git fetch origin claude/friendly-planck-b1zodj
git checkout -f -B claude/friendly-planck-b1zodj origin/claude/friendly-planck-b1zodj
```

3. Riapri il progetto in Unity.

**Da quel momento, per aggiornare** basta fare doppio clic su `Aggiorna-Progetto.bat` (si trova nella cartella del progetto) oppure scrivere `git fetch origin` e poi `git checkout -f -B claude/friendly-planck-b1zodj origin/claude/friendly-planck-b1zodj`. Poi torna in Unity, aspetta la compilazione e rilancia *Elfshift → Fase 1 → 2 - Costruisci il laboratorio*.

> Attenzione: l'aggiornamento sovrascrive i file del gioco (cartelle `Scripts`, `Editor`, `Tests`). I tuoi file e quelli creati da Unity (`Library`, scene tue…) non vengono toccati. Se modifichi uno script nostro, fanne prima una copia.

## 4. Costruire il laboratorio (automatico)

Nella barra dei menu in alto è comparso un menu **Elfshift**.

1. **Elfshift → Fase 1 → 1 - Verifica il progetto** → deve dire che è tutto a posto.
2. **Elfshift → Fase 1 → 2 - Costruisci il laboratorio** → attendi 1–2 minuti (barra di avanzamento). Crea da solo modelli 3D, materiali, texture, prefab, dati di gioco, scena, luci, neve e interfaccia.
3. Alla fine compare "*laboratorio pronto*" e la scena `Assets/Scenes/Workshop` è già aperta.

Puoi rilanciare il punto 2 quando vuoi: ricostruisce tutto da zero (le cartelle `…/Generated` vengono rigenerate; i dati in `Assets/Settings/Data` che hai modificato **non** vengono sovrascritti).

## 4b. Grafica e interfaccia

- **Modelli 3D scolpiti in Blender**: mobili, slitta, albero, camino, giocattoli, mani dell'elfo, boiserie e travi sono modellati con Blender (tramite script, quindi ripetibili) con forme morbide, smussi su tutti gli spigoli, piccole irregolarità "fatte a mano" e ombreggiatura negli angoli già calcolata (*ambient occlusion*). I file sono in `Assets/Models/Source/*.bytes` e il costruttore li carica da solo. Se un file mancasse, al suo posto usa la vecchia forma semplice (e lo scrive nella Console).
- **Texture "dipinte"**: pavimento a listoni con venature e nodi, carta da parati crema con rametti di agrifoglio (`Assets/Textures/Source`).
- **Luce più morbida**: oltre alle ombre, la costruzione attiva in URP l'effetto *Screen Space Ambient Occlusion* (ombre leggere dove gli oggetti si avvicinano) e alza la qualità (anti-aliasing 4x, ombre morbide a 4096).
- **Interfaccia in stile "cozy"**: pannelli color carta con angoli tondi e ombra sfumata (niente contorni spessi), testo marrone caldo nel font arrotondato **Nunito** (licenza OFL, in `Assets/Fonts`), icone di stella e biscotto disegnate in 3D, letterine con sigillo di ceralacca e barra del tempo sottile, suggerimenti dei tasti come piccoli "tasti" chiari su pillole scure. Le animazioni sono brevi e leggere.
- **Shader stilizzato** (`Assets/Shaders/ElfshiftStylizedLit.shader`): come quello standard di URP, più variazione di colore "dipinta a mano", venatura del legno, ombreggiatura dei modelli e luce di bordo morbida. Se compare un errore di shader nella Console, il costruttore usa da solo quello standard. Si attiva/disattiva da **Elfshift → Grafica → Shader stilizzato** (poi rilancia la costruzione).
- **Movimento**: partenza e arresto quasi immediati e la direzione segue subito lo sguardo (niente effetto "ghiaccio"); oscillazione della visuale più leggera.

> Per chi vuole modificare i modelli (non serve per giocare): gli script sono in `Tools/Blender` (`build_models.py`, `ui_icons.py`, cartella `models/`) e `Tools/Textures/make_textures.py`. Servono Python 3.13 e il modulo `bpy` (`pip install bpy`).

## 5. Giocare

Premi il pulsante **Play ▶** (in alto al centro). Il cursore del mouse viene bloccato al centro della finestra di gioco: **Esc** lo libera (per uscire dalla finestra), un clic lo blocca di nuovo.

| Tasto | Azione |
|---|---|
| **Mouse** (o stick destro) | Guardarsi intorno |
| **W A S D** o **frecce** (o stick sinistro) | Muoversi |
| **Maiusc sinistro** (o pressione dello stick sinistro) | **Scattare** (tenuto premuto, solo in avanti) |
| **Spazio** (o pulsante A) | **Saltare** |
| **Tasto sinistro del mouse** (o grilletto destro) | **Prendere / posare / consegnare / usare** (es. ritirare le letterine) |
| **E** o **tasto destro del mouse** (tenuto premuto) — o pulsante X | Lavorare sul banco (costruire, dipingere, incartare) |
| **Invio** (o Start) | Nuovo giorno, nel riepilogo di fine giornata |
| **Esc** | Libera il cursore dalla finestra di gioco |

**Cosa devi vedere:** dagli occhi dell'elfo, la **Bottega degli Elfi**: un laboratorio di legno raccolto e pieno di cose (boiserie, travi, festoni, lucine, lampade, un tavolo con i giocattoli, l'albero di Natale), con a ovest un **arco** che porta al **magazzino** e a sud la **porta** sul **cortile innevato**, dove ci sono la **slitta con le renne**, la **cassetta delle letterine**, lanterne, pupazzi di neve e abeti. Vedi le tue muffole rosse e un puntino al centro dello schermo che diventa dorato quando puoi interagire.

In alto: a sinistra **punti e biscotti** (e la **serie** di consegne), al centro le **letterine** aperte con il loro tempo e la riga **Obiettivo**, che dice sempre il prossimo passo; a destra l'**orologio della giornata**, il **calore del camino** e la **posta** in attesa. Una **freccia dorata** fluttua sopra il posto dove andare.

**Importante:** si interagisce con ciò che *guardi*. Per usare scaffali, banchi, camino, cassetta o slitta devi essere vicino **e** rivolto verso di loro.

**Il ciclo di gioco (segui l'Obiettivo in alto):**
1. **Ritira le letterine**: esci dalla porta e fai clic sulla **cassetta delle letterine** (bandierina alzata = c'è posta). Il tempo di una letterina parte solo quando la ritiri. Possono essere aperte fino a 3 letterine insieme.
2. **Prendi i materiali** nel **magazzino** (arco a ovest): legno per il **trenino**, stoffa per l'**orsetto**, metallo per il **robot**. Ne servono **2**.
3. **Banco di costruzione** (il primo a sinistra): posa i 2 materiali e **tieni premuto E** → giocattolo *grezzo*.
4. **Banco dei colori** (al centro): posa il giocattolo grezzo e tieni premuto E → giocattolo **dipinto**.
5. **Banco dei pacchi** (parete est): posa il giocattolo dipinto e tieni premuto E → **pacco**.
6. Porta il **pacco** alla **slitta** nel cortile → clic sinistro. Coriandoli, punti e biscotti!

**Cose che ti tengono occupato:**
- **Il camino** (angolo sud-ovest) si spegne piano piano: portaci un **legno** e fai clic. Con il fuoco vivace i banchi lavorano più in fretta; quasi spento (barretta blu) tutti i banchi diventano **lenti**.
- **Serie di consegne:** ogni consegna di fila vale di più (×1,25, ×1,5 … fino a ×2). Se una letterina scade, la serie riparte da zero e perdi 25 punti.
- **La giornata dura 8 minuti.** A fine giornata non arrivano più letterine, hai qualche secondo per consegnare quelle aperte e poi vedi il **riepilogo** con 1, 2 o 3 **stelle** (300, 700, 1200 punti). Premi **Invio** per un nuovo giorno.

**Come verificare che le regole siano corrette:**
- Alla slitta con un giocattolo non incartato: *«Nessuna letterina chiede questo: va prima incartato?»*.
- Un materiale sbagliato su un banco viene rifiutato (*«Questo non serve qui»*); un banco già pieno dice *«Il banco è pieno»*.
- Se sbagli, fai clic sul banco a mani vuote per riprendere l'ultimo oggetto posato.
- Clic lontano da tutto con un oggetto in mano → lo posa a terra; riavvicinati e clic per riprenderlo.
- Nel camino va solo la legna; se il fuoco è già al massimo te lo dice.
- Mentre tieni premuto E ti allontani o rilasci il tasto: il lavoro si annulla.

## 6. Struttura del progetto

```
Assets/
  Scripts/                 Il codice del gioco (assembly "Elfshift.Runtime")
    Core/                  Dati (ScriptableObject), ricette, sessione di gioco, testi
    Input/                 Controlli centralizzati (InputReader): qui si cambiano i tasti
    Player/                Elfo: movimento, sguardo in prima persona, interazione, mani
    Interaction/           Sistema di interazione (basato sulla distanza)
    Production/            Oggetti nel mondo, scaffali, banchi di lavoro
    Orders/                Letterine (ordini), cassetta della posta, slitta
    UI/                    Interfaccia (HUD, letterine, messaggi, icone)
    Vfx/                   Luci che tremolano, lucine di Natale, oggetti che ondeggiano
    Progression/           Giornata di lavoro e riepilogo con le stelle
    Audio/                 (vuota: arriverà nelle fasi successive)
  Editor/                  Strumenti che costruiscono tutto da soli (menu "Elfshift")
  Tests/EditMode/          Test automatici della logica delle ricette
  Scenes/ Prefabs/ Models/ Materials/ Textures/ Animations/ Audio/ Settings/
                           Vengono riempite dagli strumenti (le sottocartelle "Generated"
                           si rigenerano ad ogni costruzione)
```

I **dati di gioco** sono asset che puoi modificare dall'Inspector senza toccare il codice (`Assets/Settings/Data`):
- `Item_*.asset` — oggetti (nome, stato, modello 3D): materiali, giocattoli grezzi (`*Raw`), dipinti (`*Painted`) e pacchi (`*Gift`);
- `Recipe_*.asset` — ricette (ingredienti → prodotto, banco, secondi di lavorazione): `*Build`, `*Paint`, `*Wrap`;
- `Order_*.asset` — letterine (pacco richiesto, giocattolo disegnato sulla letterina, tempo, punti, biscotti, penale).

Prova: seleziona `Recipe_TrainBuild` e cambia *Craft Seconds* a 1; oppure seleziona `Order_Teddy` e cambia *Time Limit Seconds*.

Nell'oggetto `_Game`: *Order Manager* → *Max Active* (letterine aperte insieme), *Max Pending* (letterine nella cassetta), *Next Order Delay* (ogni quanto arriva posta); *Day Cycle* → *Day Length* (durata della giornata) e *Star Thresholds* (punti per le stelle). Il camino (oggetto `Fireplace`) ha i valori del calore.

## 7. Sostituire i modelli provvisori con modelli definitivi

I modelli attuali sono **creati da script** (C# e Blender, nessun asset esterno, nessun problema di licenza). Per sostituirli con modelli fatti a mano da un artista non serve riscrivere il gameplay:

- **Oggetti (legno, trenino…)**: seleziona l'asset `Item_*` in `Assets/Settings/Data` e trascina il tuo prefab nel campo **Visual Prefab**. L'origine del modello deve essere al centro della base. Vale ovunque: in mano, sul banco, nelle icone della letterina.
- **Elfo**: in prima persona il corpo non si vede (proietta solo l'ombra); le braccia visibili sono `FirstPersonHands`, figlie della telecamera nel prefab `Player`. Il corpo è il figlio `ElfVisual`. Sostituiscili con il tuo personaggio e aggiungi un componente che implementa `ICharacterVisual` (4 metodi: velocità, trasporto, lavoro, raccolta; per esempio imposta i parametri di un Animator). Il resto non cambia.
- **Mobili e slitta**: sono prefab in `Assets/Prefabs/Generated/Stations`. Duplicali e sostituisci i modelli mantenendo i punti di riferimento (`Input1`, `Input2`, `Output`, `InteractionPoint`…) e i collider.

> Attenzione: le cartelle `Generated` vengono rigenerate quando rilanci la costruzione. Se vuoi tenere una tua versione, spostala fuori da `Generated`.

Strumenti gratuiti utili per i modelli definitivi: **Blender** (<https://www.blender.org>, modellazione e animazione) e asset con licenza commerciale (es. *Kenney* in CC0, o altri pack con licenza compatibile con un gioco commerciale: controlla sempre la licenza). Li vedremo nelle fasi successive.

## 8. Test automatici (facoltativi)

*Window → General → Test Runner* → scheda **EditMode** → **Run All**. Verifica la logica delle ricette (ordine degli ingredienti, ingredienti inutili, postazione sbagliata…).

## 9. Problemi comuni

| Sintomo | Cosa fare |
|---|---|
| Tutto **rosa/magenta** | Il progetto non usa URP. Ricrea il progetto con il template **Universal 3D** e ricopia `Assets`. |
| Errori rossi del tipo *"Assembly … references … not found"* | Manca un pacchetto: *Window → Package Manager → Unity Registry*, installa **Input System** e **Universal RP**. Poi *Assets → Reimport All*. |
| Premendo i tasti non succede nulla | *Edit → Project Settings → Player → Other Settings → Active Input Handling* = **Input System Package (New)** (o **Both**), poi riavvia. |
| Il menu **Elfshift** non compare | Controlla che esistano `Assets/Scripts` e `Assets/Editor` nel progetto. Apri *Window → General → Console*, premi *Clear* e aspetta la fine della compilazione: se restano messaggi **rossi**, copiane il testo e mandalo. Dopo l'aggiornamento il menu compare anche con errori, e la voce **Elfshift → Fase 1 → 0 - Aiuto** spiega cosa fare. |
| Le icone delle letterine sono vuote | Il testo e il timer funzionano comunque. Segnalami il messaggio della Console. |
| "*Errore durante la costruzione*" | Leggi il dettaglio nella Console e mandamelo: lo correggiamo senza ricominciare. |

## 10. Cosa c'è e cosa manca

**Adesso:** bottega con magazzino e cortile, 3 materiali, 3 giocattoli, 3 banchi (costruzione, colori, pacchi), letterine nella cassetta della posta, fino a 3 ordini insieme, camino da alimentare, serie di consegne, giornata di 8 minuti con riepilogo e stelle, guida "Obiettivo" sempre attiva, interfaccia, neve, luci, test della logica.

**Prossime fasi:** più giocattoli e ricette miste, dieci giornate di fila con salvataggio, negozio dei miglioramenti con i biscotti, aiutante, imprevisti, menu, pausa, opzioni, audio, build per Windows/Steam.

Nota sulla visuale: la Fase 1 è in **prima persona** (telecamera all'altezza degli occhi, mouse per guardare). Il modello dell'elfo con le sue animazioni resta nel prefab e proietta l'ombra; tornerà visibile se in futuro vorremo una vista in terza persona.

Tutti i modelli, le texture e le icone di questa fase sono **originali, generati da codice** (in C# e con script di Blender); non contengono contenuti di terzi. L'unico elemento esterno è il font **Nunito** (licenza SIL Open Font License, utilizzabile anche in giochi commerciali: il testo della licenza è in `Assets/Fonts/Nunito-OFL.txt`).
