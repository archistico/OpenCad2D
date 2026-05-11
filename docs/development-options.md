Secondo me ora OpenCad2D ha superato la fase “prototipo base”. Le alternative interessanti sono queste.

## Alternativa 1 — Performance e robustezza con molti oggetti

Visto che stai già provando file con migliaia di entità, questa è la scelta più “ingegneristica”.

Cosa farei:

```text
1. Profilare pan/zoom/rendering con 2.000-10.000 entità
2. Migliorare rendering delle griglie
3. Disegnare solo entità visibili nel viewport
4. Ottimizzare selezione e snap
5. Migliorare/attivare davvero lo spatial index
```

Perché ha senso: se il CAD rallenta appena il disegno cresce, tutte le altre funzioni diventano meno piacevoli.

Prossimo sviluppo concreto:

```text
Viewport culling:
renderizzare solo le entità il cui bounding box interseca il viewport visibile.
```

Questa è probabilmente la prima ottimizzazione utile.

---

## Alternativa 2 — Property panel

Ora che hai selezione, grip, layer, salvataggio e diversi tipi di entità, un pannello proprietà diventa molto utile.

Cosa mostrerebbe:

```text
Per una linea:
- tipo: Line
- layer
- start X/Y
- end X/Y
- lunghezza
- DX / DY

Per un cerchio:
- tipo: Circle
- layer
- centro X/Y
- raggio
- diametro

Per più entità:
- numero selezionate
- layer comuni/diversi
- bounding box totale
```

Perché ha senso: rende il CAD più leggibile e prepara la modifica numerica delle proprietà.

Prossimo sviluppo concreto:

```text
Pannello destro iniziale in sola lettura.
```

Poi fase successiva: editing numerico.

---

## Alternativa 3 — Layer manager vero

Oggi hai combo layer + visible + locked. Funziona, ma non scala.

Cosa aggiungere:

```text
- creare layer
- rinominare layer
- cambiare colore
- cambiare lineweight
- visibile/nascosto
- locked/unlocked
- eliminare layer vuoti
- impostare layer corrente
```

Perché ha senso: è una base CAD fondamentale, soprattutto ora che hai persistence.

Prossimo sviluppo concreto:

```text
Layer Manager dialog semplice con tabella layer.
```

Io lo farei dopo il property panel, non prima.

---

## Alternativa 4 — PolylineTool

Molto CAD-like e utile.

Comportamento:

```text
1. Clicco Polyline
2. Specifico primo punto
3. Specifico secondo punto
4. Specifico terzo punto
5. ESC termina
6. C chiude la polilinea
```

Dovrebbe supportare:

```text
- click mouse
- coordinate assolute
- coordinate relative
- distanza diretta
- Ortho
- Polar Tracking
- snap
- preview dell’ultimo segmento
```

Perché ha senso: riusa gran parte del lavoro già fatto su command line e input tecnico.

Rischio: richiede un tool multi-punto, quindi è più complesso di `CircleTool`.

---

## Alternativa 5 — ArcTool

Utile, ma va deciso bene il paradigma.

Varianti possibili:

```text
A. Centro, punto iniziale, punto finale
B. Tre punti sull’arco
C. Punto iniziale, punto finale, raggio
D. Punto iniziale, centro, angolo
```

Per iniziare sceglierei:

```text
Arc by 3 points
```

Perché è intuitivo e non richiede subito input angolare.

Però lo metterei dopo PolylineTool o Property Panel.

---

## Alternativa 6 — Editing numerico da command line

Ora la command line accetta punti e distanze. Il passo successivo sarebbe renderla più “comandabile”.

Esempi:

```text
LINE
CIRCLE
MOVE
COPY
ZOOM
LAYER
```

Oppure durante un comando:

```text
@100,0
50
100,50
```

Per ora non lo farei. Prima consoliderei UI, property panel e performance.

---

## Alternativa 7 — Migliorare Grip Editing

I grip funzionano, ma si possono rendere più CAD-like.

Possibili rifiniture:

```text
- grip su Polyline quando arriverà
- grip editing con direct distance più esplicito
- grip center/move per rettangoli/polilinee
- grip multipli
- grip colorati per tipo
- tooltip sul grip: Start, End, Center, Radius
```

Ha senso, ma lo farei quando aggiungiamo più entità modificabili.

---

# La mia raccomandazione

Io continuerei così:

```text
1. Property panel iniziale in sola lettura
2. Viewport culling / performance rendering
3. Layer manager
4. PolylineTool
5. Editing numerico dal property panel
```

Il passo migliore adesso, secondo me, è:

```text
Property panel iniziale in sola lettura
```

Motivo: hai già selezione, grip, cerchio, linea, layer e file JSON. Un pannello proprietà ti fa “vedere” il modello CAD e prepara bene modifiche future senza complicare subito gli strumenti di disegno.
