# Il Laboratorio di Babbo Natale — Fase 1: prototipo 3D giocabile

Gioco natalizio 3D per PC (Windows), sviluppato con **Unity 6** e C#.
Tu gestisci il laboratorio degli elfi al Polo Nord: arrivano le letterine dei bambini, costruisci i giocattoli e li porti alla slitta prima che scada il tempo.

> **Stato:** Fase 1 di 6. Il ciclo base è giocabile: *prendi il legno → posalo sul banco → costruisci il trenino → consegnalo alla slitta → ricevi punti e biscotti*.

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

## 4. Costruire il laboratorio (automatico)

Nella barra dei menu in alto è comparso un menu **Elfshift**.

1. **Elfshift → Fase 1 → 1 - Verifica il progetto** → deve dire che è tutto a posto.
2. **Elfshift → Fase 1 → 2 - Costruisci il laboratorio** → attendi 1–2 minuti (barra di avanzamento). Crea da solo modelli 3D, materiali, texture, prefab, dati di gioco, scena, luci, neve e interfaccia.
3. Alla fine compare "*laboratorio pronto*" e la scena `Assets/Scenes/Workshop` è già aperta.

Puoi rilanciare il punto 2 quando vuoi: ricostruisce tutto da zero (le cartelle `…/Generated` vengono rigenerate; i dati in `Assets/Settings/Data` che hai modificato **non** vengono sovrascritti).

## 5. Giocare

Premi il pulsante **Play ▶** (in alto al centro).

| Tasto | Azione |
|---|---|
| **W A S D** o **frecce** (o stick sinistro) | Muoversi |
| **Spazio** (o pulsante A) | Prendere un oggetto / posarlo / consegnarlo |
| **E** (o pulsante X) — *tenuto premuto* | Costruire sul banco |

**Cosa devi vedere:** un laboratorio di legno con tappeto rosso, tre banchi, tre scaffali alla sinistra, un albero di Natale, finestre con neve che cade e, a destra oltre la porta, la slitta sulla banchina. Dopo 2 secondi compare in alto una **letterina** con il trenino richiesto e il tempo.

**Il ciclo da provare:**
1. Avvicinati allo **scaffale in alto a sinistra** (insegna con il tronchetto) → in basso compare `[SPAZIO] Prendi Legno` → premi **Spazio**: il legno è in mano.
2. Vai al **primo banco** (quello con martello e morsa) → `[SPAZIO] Metti sul banco` → **Spazio**.
3. Torna a prendere un **secondo legno** e posalo sul banco. Compare `[E] Tieni premuto: costruisci Trenino`.
4. Tieni premuto **E** per 2,5 secondi: l'elfo martella, la barra si riempie, compaiono scintille e il trenino appare sul banco.
5. Premi **Spazio** vicino al banco per prendere il trenino.
6. Portalo alla **slitta** (esci dalla porta a destra) → `[SPAZIO] Consegna alla slitta` → **Spazio**.
7. Vedi coriandoli, il messaggio *«Trenino consegnato! +… punti +5 biscotti»* e punti/biscotti salgono in alto a sinistra. Poco dopo arriva un nuovo ordine.

**Come verificare che le regole siano corrette:**
- Lontano da scaffali e banchi, **Spazio non fa nulla di utile**: le azioni dipendono dalla posizione.
- Alla slitta con il *legno* in mano: *«Nessun ordine aperto per questo oggetto»*.
- Con il banco pieno o con un oggetto inutile: il banco lo rifiuta con un messaggio.
- Spazio lontano da tutto con un oggetto in mano → lo posa a terra davanti a te; riavvicinati e Spazio per riprenderlo.
- Se lasci scadere la letterina: *«Ordine scaduto!»* e perdi 20 punti.
- Mentre tieni premuto E ti allontani o rilasci il tasto: la costruzione si annulla.

## 6. Struttura del progetto

```
Assets/
  Scripts/                 Il codice del gioco (assembly "Elfshift.Runtime")
    Core/                  Dati (ScriptableObject), ricette, sessione di gioco, testi
    Input/                 Controlli centralizzati (InputReader): qui si cambiano i tasti
    Player/                Elfo: movimento, interazione, mani, animazione, telecamera
    Interaction/           Sistema di interazione (basato sulla distanza)
    Production/            Oggetti nel mondo, scaffali, banchi di lavoro
    Orders/                Ordini, timer, slitta
    UI/                    Interfaccia (HUD, letterine, messaggi, icone)
    Vfx/                   Luci che tremolano, lucine di Natale, oggetti che ondeggiano
    Progression/, Audio/   (vuote: arriveranno nelle fasi successive)
  Editor/                  Strumenti che costruiscono tutto da soli (menu "Elfshift")
  Tests/EditMode/          Test automatici della logica delle ricette
  Scenes/ Prefabs/ Models/ Materials/ Textures/ Animations/ Audio/ Settings/
                           Vengono riempite dagli strumenti (le sottocartelle "Generated"
                           si rigenerano ad ogni costruzione)
```

I **dati di gioco** sono asset che puoi modificare dall'Inspector senza toccare il codice (`Assets/Settings/Data`):
- `Item_*.asset` — oggetti (nome, stato, modello 3D);
- `Recipe_*.asset` — ricette (ingredienti → prodotto, secondi di lavorazione);
- `Order_*.asset` — letterine (oggetto richiesto, tempo, punti, biscotti, penale).

Prova: seleziona `Recipe_Train` e cambia *Craft Seconds* a 1; oppure seleziona `Order_Train` e cambia *Time Limit Seconds*.

Il numero di ordini contemporanei si cambia nell'oggetto `_Game` → componente *Order Manager* → *Max Active*.

## 7. Sostituire i modelli provvisori con modelli definitivi

I modelli attuali sono **generati da codice** (nessun asset esterno, nessun problema di licenza): servono a provare il gioco con un aspetto gradevole. Per sostituirli non serve riscrivere il gameplay:

- **Oggetti (legno, trenino…)**: seleziona l'asset `Item_*` in `Assets/Settings/Data` e trascina il tuo prefab nel campo **Visual Prefab**. L'origine del modello deve essere al centro della base. Vale ovunque: in mano, sul banco, nelle icone della letterina.
- **Elfo**: il corpo è un figlio del prefab `Player` chiamato `ElfVisual`. Sostituiscilo con il tuo personaggio e aggiungi un componente che implementa `ICharacterVisual` (4 metodi: velocità, trasporto, lavoro, raccolta; per esempio imposta i parametri di un Animator). Il resto non cambia.
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
| Il menu **Elfshift** non compare | Ci sono errori di compilazione nella Console: risolvili (di solito è un pacchetto mancante, vedi sopra). |
| Le icone delle letterine sono vuote | Il testo e il timer funzionano comunque. Segnalami il messaggio della Console. |
| "*Errore durante la costruzione*" | Leggi il dettaglio nella Console e mandamelo: lo correggiamo senza ricominciare. |

## 10. Cosa c'è e cosa manca

**Fase 1 (questa):** ambiente 3D, elfo con camminata/trasporto/lavoro, telecamera inclinata morbida, scaffale del legno, banco di costruzione con ricetta, slitta, ordine con timer, punti e biscotti, interfaccia, neve, luci, test della logica.

**Prossime fasi:** 2) tutte le postazioni (vernice, pacchi), più materiali/giocattoli, più ordini; 3) dieci giornate, stelle, salvataggio; 4) negozio e aiutante; 5) imprevisti, menu, pausa, opzioni, audio; 6) build Windows, itch.io, Steam.

Nota sulla visuale: nella richiesta compariva sia "prima persona" sia "visuale dall'alto inclinata". Ho scelto la **vista dall'alto inclinata in terza persona** (con l'elfo visibile e animato), coerente con gli altri punti del documento. Se preferisci davvero la prima persona dimmelo prima di proseguire.

Tutti i modelli, le texture e le icone di questa fase sono **originali, generati da codice**; non contengono contenuti di terzi.
