# OpenCad2D — Checklist completa di verifica manuale

Checklist pensata per controllare in modo sistematico che **ogni entità**, **ogni tool** e le principali funzioni applicative lavorino correttamente prima di una release o di una nuova fase di sviluppo.

> Versione progetto usata come base: `20260527_OpenCad2d.zip`.
>
> Convenzione consigliata: segnare ogni punto con uno di questi esiti: `OK`, `KO`, `PARZIALE`, `DA RIVEDERE`, aggiungendo note e screenshot quando serve.

---

## 0. Dati del collaudo

- [ ] Versione / commit testato: `______________________________`
- [ ] Sistema operativo: `______________________________`
- [ ] Runtime .NET: `______________________________`
- [ ] Data verifica: `______________________________`
- [ ] Tester: `______________________________`
- [ ] File di prova usato: `______________________________`
- [ ] Cartella screenshot / prove: `______________________________`

---

## 1. Gate tecnico iniziale

Eseguire dalla root del repository.

```powershell
dotnet clean
dotnet restore
dotnet build
dotnet test
```

Oppure, se disponibile:

```powershell
make check
```

- [ ] Il progetto viene pulito senza errori.
- [ ] Il restore termina correttamente.
- [ ] La build termina con `Errori: 0`.
- [ ] Non compaiono nuovi warning critici.
- [ ] Tutti i test passano.
- [ ] Il numero totale dei test è coerente con l’ultima esecuzione nota.
- [ ] L’app parte con `make run` o `dotnet run --project src/OpenCad2D.App`.
- [ ] Non compaiono eccezioni all’avvio.
- [ ] Non compaiono finestre vuote, controlli mancanti o errori XAML evidenti.

---

## 2. Avvio applicazione e interfaccia generale

- [ ] L’app mostra il titolo corretto.
- [ ] L’icona applicazione è corretta nella finestra e nella barra applicazioni.
- [ ] Il logo è visualizzato dove previsto.
- [ ] La finestra principale si apre con dimensioni utilizzabili.
- [ ] Il canvas è visibile.
- [ ] Il cursore CAD/crosshair è visibile e leggibile.
- [ ] La griglia è visibile quando attiva.
- [ ] La barra strumenti sinistra non copre inutilmente la griglia.
- [ ] La barra superiore è leggibile.
- [ ] La status bar ha stile coerente con il resto dell’app.
- [ ] Il pannello proprietà appare quando seleziono un’entità.
- [ ] Il pannello proprietà si svuota o cambia correttamente quando cambio selezione.
- [ ] Undo e Redo sono visibili e coerenti con lo stato del documento.
- [ ] Il selettore layer è visibile.
- [ ] I controlli layer visibilità/blocco funzionano dalla UI principale.
- [ ] L’indicatore comando mostra lo stato corretto del tool attivo.
- [ ] Lo zoom con rotellina è fluido.
- [ ] Il pan funziona.
- [ ] Zoom Extents riporta correttamente il disegno in vista.
- [ ] Reset View funziona.
- [ ] ESC annulla il tool corrente.
- [ ] Un secondo ESC svuota la selezione quando non c’è un tool attivo.
- [ ] DELETE elimina le entità selezionate.
- [ ] La UI resta reattiva dopo operazioni ripetute.

---

## 3. Documento nuovo e impostazioni iniziali

- [ ] Nuovo documento crea un file vuoto senza errori.
- [ ] Il documento contiene i layer di default previsti.
- [ ] `Layer 0` è presente.
- [ ] `Annotations` è presente.
- [ ] `Walls` è presente.
- [ ] `Axis` è presente.
- [ ] `Construction lines` è presente.
- [ ] Il layer corrente è valido.
- [ ] I formati linea di default sono presenti.
- [ ] I formati testo di default sono presenti.
- [ ] Gli stili quota di default sono presenti.
- [ ] Le impostazioni griglia sono coerenti al primo avvio.
- [ ] Le impostazioni snap sono coerenti al primo avvio.
- [ ] Le impostazioni vengono mantenute tra una sessione e l’altra quando previsto.

---

## 4. Command line e alias

### 4.1 Comportamento generale

- [ ] La command line riceve focus automaticamente quando digito un numero durante un comando che accetta input numerico.
- [ ] La command line accetta coordinate assolute.
- [ ] La command line accetta coordinate relative, se previste.
- [ ] La command line accetta distanze durante tool a due punti.
- [ ] La command line accetta angoli quando il tool lo prevede.
- [ ] INVIO conferma il comando o l’opzione corrente.
- [ ] Click destro conferma quando previsto.
- [ ] ESC annulla senza lasciare stati interni sporchi.
- [ ] La cronologia comandi con freccia su/giù funziona.
- [ ] TAB completa o suggerisce il comando quando previsto.
- [ ] Input non valido mostra un messaggio chiaro e non rompe il tool.
- [ ] Dopo un input non valido posso continuare il comando.
- [ ] Il prompt del comando è sempre coerente con lo step corrente.

### 4.2 Alias da verificare

- [ ] `SELECT`, `SEL` attivano Selection.
- [ ] `ZOOMWINDOW`, `ZW` attivano Zoom Window.
- [ ] `POINT`, `PO` attivano Point.
- [ ] `TEXT`, `T` attivano Text.
- [ ] `MTEXT`, `MT` attivano MText.
- [ ] `LINE`, `L` attivano Line.
- [ ] `POLYLINE`, `PL` attivano Polyline.
- [ ] `SPLINE`, `SPL` attivano Spline.
- [ ] `POLYGON`, `PG` attivano Polygon.
- [ ] `RECTANGLE`, `REC` attivano Rectangle.
- [ ] `RECTANGLESIDES`, `RECTSIDES`, `RSIDES` attivano Rect Sides.
- [ ] `CIRCLE`, `C` attivano Circle.
- [ ] `ELLIPSE`, `EL` attivano Ellipse.
- [ ] `ARC`, `A` attivano Arc.
- [ ] `ARC3P`, `A3P` attivano Arc 3P.
- [ ] `HDIM`, `H`, `HORIZONTALDIM`, `HORIZONTALDIMENSION` attivano Horizontal Dim.
- [ ] `VDIM`, `V`, `VERTICALDIM`, `VERTICALDIMENSION` attivano Vertical Dim.
- [ ] `ADIM`, `AL`, `ALIGNEDDIM`, `ALIGNEDDIMENSION` attivano Aligned Dim.
- [ ] `RDIM`, `RAD`, `RADIUSDIM`, `RADIUSDIMENSION` attivano Radius Dim.
- [ ] `DDIM`, `DIA`, `DIAMETERDIM`, `DIAMETERDIMENSION` attivano Diameter Dim.
- [ ] `ANGDIM`, `ANG`, `ANGULARDIM`, `ANGULARDIMENSION` attivano Angular Dim.
- [ ] `MOVE`, `M` attivano Move.
- [ ] `COPY`, `CO` attivano Copy.
- [ ] `ROTATE`, `RO` attivano Rotate.
- [ ] `SCALE`, `SC` attivano Scale.
- [ ] `ALIGN` attiva Align.
- [ ] `TRIM`, `TR` attivano Trim.
- [ ] `OFFSET`, `O` attivano Offset.
- [ ] `FILLET`, `F` attivano Fillet.
- [ ] `MIRROR`, `MI` attivano Mirror.
- [ ] `EXTEND`, `EX` attivano Extend.
- [ ] `BREAKPOINT`, `BP` attivano Break Point.
- [ ] `BREAKSEGMENT`, `BREAK`, `BR`, `BS` attivano Break Segment.
- [ ] `EXPLODE`, `X` attivano Explode.
- [ ] `JOIN`, `J` attivano Join.
- [ ] `DELETE`, `DEL` attivano Delete.
- [ ] `DISTANCE`, `DI`, `MEASUREDISTANCE` attivano Measure Distance.
- [ ] `MEASURE`, `ME`, `MEASUREENTITY` attivano Measure Entity.
- [ ] `MEASUREANGLE`, `MANG` attivano Measure Angle.
- [ ] `MEASUREAREA`, `MAREA` attivano Measure Area.

---

## 5. Entità — verifica creazione, rendering, selezione, proprietà, salvataggio

Per ogni entità sotto, verificare almeno: creazione, rendering, hit test, selezione, proprietà, grip, move/copy/rotate/scale/mirror dove applicabile, undo/redo, salvataggio/riapertura, SVG, PDF, DXF dove supportato.

### 5.1 PointEntity

- [ ] Creo un punto con il tool Point.
- [ ] Il punto è visibile a zoom normale.
- [ ] Il punto resta visibile a zoom molto alto/basso.
- [ ] Il punto è selezionabile con click.
- [ ] Il punto è selezionabile con finestra.
- [ ] Il pannello proprietà mostra dati coerenti.
- [ ] Posso modificare coordinate dal pannello proprietà.
- [ ] Grip editing sposta il punto.
- [ ] Move funziona.
- [ ] Copy funziona.
- [ ] Rotate non produce errori.
- [ ] Scale non produce errori.
- [ ] Mirror funziona.
- [ ] Salva/riapri mantiene il punto.

### 5.2 LineEntity

- [ ] Creo una linea con due click.
- [ ] Creo una linea con coordinate/distanza da command line.
- [ ] La preview segue correttamente il puntatore.
- [ ] La status bar mostra lunghezza, ΔX e ΔY quando previsto.
- [ ] Ortho vincola correttamente la linea.
- [ ] Polar Tracking vincola correttamente la linea.
- [ ] Endpoint snap funziona su entrambi gli estremi.
- [ ] Midpoint snap funziona.
- [ ] Nearest snap funziona.
- [ ] Perpendicular snap funziona quando previsto.
- [ ] La linea è selezionabile con click.
- [ ] La linea è selezionabile con finestra/crossing.
- [ ] Il pannello proprietà mostra start/end/layer/stile.
- [ ] Modifica coordinate da pannello proprietà aggiorna il canvas.
- [ ] Grip endpoint modifica l’estremo corretto.
- [ ] Grip midpoint sposta la linea.
- [ ] Move funziona.
- [ ] Copy funziona.
- [ ] Rotate funziona con angolo digitato.
- [ ] Scale funziona con fattore digitato.
- [ ] Mirror funziona.
- [ ] Offset funziona da entrambi i lati.
- [ ] Trim funziona usando altra linea come tagliente.
- [ ] Extend funziona verso un bordo valido.
- [ ] Break Point divide la linea.
- [ ] Break Segment rimuove il segmento scelto.
- [ ] Fillet raggio 0 crea angolo netto.
- [ ] Fillet raggio > 0 crea arco tangente.
- [ ] Join unisce linee connesse.
- [ ] Salva/riapri mantiene geometria e layer.
- [ ] Export SVG mantiene spessore layer.
- [ ] Export DXF mantiene layer/linetype/lineweight.

### 5.3 CircleEntity

- [ ] Creo un cerchio centro-raggio.
- [ ] Creo un cerchio con raggio digitato.
- [ ] La preview del cerchio è corretta.
- [ ] Center snap funziona.
- [ ] Quadrant snap funziona se previsto.
- [ ] Nearest snap funziona sul bordo.
- [ ] Il cerchio è selezionabile.
- [ ] Il pannello proprietà mostra centro/raggio/diametro se previsto.
- [ ] Modifica raggio da pannello proprietà aggiorna il canvas.
- [ ] Grip centro sposta il cerchio.
- [ ] Grip raggio modifica il raggio.
- [ ] Move funziona.
- [ ] Copy funziona.
- [ ] Rotate non altera il cerchio in modo errato.
- [ ] Scale modifica correttamente il raggio.
- [ ] Mirror mantiene un cerchio valido.
- [ ] Offset interno funziona.
- [ ] Offset esterno funziona.
- [ ] Break Segment su cerchio crea geometria coerente quando supportato.
- [ ] Fill/solid fill funziona se attivabile.
- [ ] Salva/riapri mantiene cerchio e fill.
- [ ] SVG mostra cerchio e fill.
- [ ] PDF mostra cerchio e fill supportato.
- [ ] DXF mostra cerchio e HATCH quando supportato.

### 5.4 ArcEntity

- [ ] Creo un arco con tool Arc.
- [ ] Creo un arco con tool Arc 3P.
- [ ] La preview è coerente durante la definizione.
- [ ] Endpoint snap funziona sui due estremi.
- [ ] Center snap funziona.
- [ ] Midpoint/nearest su arco funziona dove previsto.
- [ ] L’arco è selezionabile.
- [ ] Il pannello proprietà mostra centro/raggio/angoli.
- [ ] Modifica proprietà aggiorna l’arco.
- [ ] Grip estremi modifica in modo coerente.
- [ ] Grip centro sposta l’arco.
- [ ] Grip raggio modifica il raggio.
- [ ] Move funziona.
- [ ] Copy funziona.
- [ ] Rotate funziona.
- [ ] Scale funziona.
- [ ] Mirror mantiene orientamento e angoli coerenti.
- [ ] Offset arco funziona.
- [ ] Trim su arco funziona.
- [ ] Extend su arco funziona.
- [ ] Break Point su arco funziona.
- [ ] Break Segment su arco funziona.
- [ ] Salva/riapri mantiene arco.
- [ ] SVG/PDF/DXF esportano l’arco correttamente.

### 5.5 EllipseEntity

- [ ] Creo un’ellisse.
- [ ] La preview è corretta.
- [ ] L’ellisse è selezionabile.
- [ ] Snap centro funziona.
- [ ] Snap nearest sul bordo funziona.
- [ ] Il pannello proprietà mostra centro/assi/rotazione.
- [ ] Modifica proprietà aggiorna l’ellisse.
- [ ] Grip editing modifica centro/assi/rotazione in modo coerente.
- [ ] Move funziona.
- [ ] Copy funziona.
- [ ] Rotate funziona.
- [ ] Scale funziona.
- [ ] Mirror funziona.
- [ ] Trim usa ellisse come tagliente quando previsto.
- [ ] Break Segment funziona se supportato.
- [ ] Fill/solid fill funziona se attivabile.
- [ ] Salva/riapri mantiene ellisse e fill.
- [ ] SVG/PDF/DXF esportano ellisse correttamente dove supportato.

### 5.6 EllipticalArcEntity

- [ ] Creo o ottengo un arco ellittico tramite tool/funzione supportata.
- [ ] L’arco ellittico viene renderizzato correttamente.
- [ ] È selezionabile.
- [ ] Snap endpoint funziona.
- [ ] Snap nearest funziona.
- [ ] Il pannello proprietà mostra dati coerenti.
- [ ] Grip editing funziona dove supportato.
- [ ] Move funziona.
- [ ] Copy funziona.
- [ ] Rotate funziona.
- [ ] Scale funziona.
- [ ] Mirror funziona.
- [ ] Break Point funziona se supportato.
- [ ] Break Segment funziona se supportato.
- [ ] Salva/riapri mantiene l’arco ellittico.
- [ ] SVG/PDF/DXF gestiscono correttamente l’entità o degradano in modo documentato.

### 5.7 PolylineEntity

- [ ] Creo una polyline aperta.
- [ ] Creo una polyline chiusa.
- [ ] Uso `Undo` dentro il comando polyline.
- [ ] Uso `Close` dentro il comando polyline.
- [ ] INVIO termina correttamente una polyline aperta.
- [ ] Click destro termina correttamente quando previsto.
- [ ] La preview segue correttamente l’ultimo segmento.
- [ ] Endpoint snap funziona su ogni vertice.
- [ ] Midpoint snap funziona sui segmenti.
- [ ] Nearest snap funziona.
- [ ] La polyline è selezionabile.
- [ ] Il pannello proprietà mostra dati coerenti.
- [ ] Grip vertice modifica il vertice corretto.
- [ ] Grip segmento sposta/modifica in modo coerente se previsto.
- [ ] Move funziona.
- [ ] Copy funziona.
- [ ] Rotate funziona.
- [ ] Scale funziona.
- [ ] Mirror funziona.
- [ ] Offset polyline aperta funziona.
- [ ] Offset polyline chiusa funziona.
- [ ] Offset su angoli stretti non genera spike eccessivi.
- [ ] Trim su polyline funziona.
- [ ] Extend su polyline aperta funziona.
- [ ] Break Point funziona.
- [ ] Break Segment funziona.
- [ ] Explode trasforma segmenti rettilinei in linee.
- [ ] Join ricrea polyline da linee connesse.
- [ ] Fill/solid fill funziona su polyline chiusa.
- [ ] Salva/riapri mantiene vertici, chiusura e fill.
- [ ] SVG/PDF/DXF esportano correttamente polyline e fill supportato.

### 5.8 BezierSplineEntity

- [ ] Creo una spline aperta.
- [ ] Creo una spline chiusa se supportato.
- [ ] Uso `Undo` dentro il comando spline.
- [ ] Uso `Close` dentro il comando spline.
- [ ] INVIO termina correttamente.
- [ ] La preview dei punti di controllo è corretta.
- [ ] La spline è selezionabile.
- [ ] Snap endpoint funziona dove previsto.
- [ ] Snap nearest funziona sul tracciato.
- [ ] Il pannello proprietà mostra dati coerenti.
- [ ] Grip editing dei punti di controllo funziona.
- [ ] Move funziona.
- [ ] Copy funziona.
- [ ] Rotate funziona.
- [ ] Scale funziona.
- [ ] Mirror funziona.
- [ ] Break Point funziona su spline aperta se supportato.
- [ ] Break Segment funziona su spline aperta se supportato.
- [ ] Salva/riapri mantiene la spline.
- [ ] Export gestisce la spline correttamente o la approssima in modo documentato.

### 5.9 TextEntity

- [ ] Creo un testo monoriga.
- [ ] La finestra di input testo si apre correttamente.
- [ ] Conferma testo inserisce l’entità.
- [ ] Annulla non inserisce nulla.
- [ ] Il testo è leggibile sul canvas.
- [ ] Il testo rispetta formato testo corrente.
- [ ] Il testo rispetta layer corrente.
- [ ] Selezione con click funziona.
- [ ] Il pannello proprietà mostra contenuto, altezza, rotazione, layer.
- [ ] Modifica contenuto da pannello proprietà aggiorna il canvas.
- [ ] Modifica altezza aggiorna il canvas.
- [ ] Modifica rotazione aggiorna il canvas.
- [ ] Grip editing sposta il testo.
- [ ] Move funziona.
- [ ] Copy funziona.
- [ ] Rotate funziona.
- [ ] Scale funziona.
- [ ] Mirror funziona senza corrompere il contenuto.
- [ ] Salva/riapri mantiene testo, altezza, rotazione e formato.
- [ ] SVG/PDF/DXF esportano il testo in modo leggibile.

### 5.10 MultilineTextEntity

- [ ] Creo un testo multilinea con `MTEXT`.
- [ ] La finestra di input consente più righe.
- [ ] Il testo va a capo correttamente.
- [ ] Aumentando il font size non crea sovrapposizioni impreviste.
- [ ] Il box/testo rispetta l’allineamento previsto.
- [ ] Il testo è selezionabile.
- [ ] Il pannello proprietà mostra contenuto, dimensioni, rotazione e formato.
- [ ] Modifica contenuto aggiorna il canvas.
- [ ] Modifica altezza/font size aggiorna il layout.
- [ ] Grip editing sposta o ridimensiona dove supportato.
- [ ] Move funziona.
- [ ] Copy funziona.
- [ ] Rotate funziona.
- [ ] Scale funziona.
- [ ] Mirror funziona senza corrompere il contenuto.
- [ ] Salva/riapri mantiene righe e formattazione base.
- [ ] SVG/PDF/DXF gestiscono il multilinea correttamente o con limitazioni documentate.

### 5.11 LinearDimensionEntity — Horizontal Dimension

- [ ] Creo una quota orizzontale da due punti.
- [ ] La linea di quota è orizzontale.
- [ ] Il testo è sopra la linea di quota come previsto.
- [ ] Frecce/terminatori sono visibili.
- [ ] Il valore quota è corretto.
- [ ] La quota usa lo stile quota corrente.
- [ ] La quota è selezionabile.
- [ ] Il pannello proprietà mostra dati coerenti.
- [ ] Move funziona.
- [ ] Copy funziona.
- [ ] Rotate/Scale/Mirror non corrompono la quota.
- [ ] La quota diventa stale quando la geometria originale cambia, se previsto.
- [ ] Salva/riapri mantiene quota e stato stale.
- [ ] SVG/PDF/DXF esportano la quota correttamente o con limitazioni documentate.

### 5.12 LinearDimensionEntity — Vertical Dimension

- [ ] Creo una quota verticale da due punti.
- [ ] La linea di quota è verticale.
- [ ] Il testo è spostato a destra della linea di quota come previsto.
- [ ] Frecce/terminatori sono visibili.
- [ ] Il valore quota è corretto.
- [ ] La quota usa lo stile quota corrente.
- [ ] La quota è selezionabile.
- [ ] Il pannello proprietà mostra dati coerenti.
- [ ] Move funziona.
- [ ] Copy funziona.
- [ ] Rotate/Scale/Mirror non corrompono la quota.
- [ ] La quota diventa stale quando la geometria originale cambia, se previsto.
- [ ] Salva/riapri mantiene quota e stato stale.
- [ ] SVG/PDF/DXF esportano la quota correttamente o con limitazioni documentate.

### 5.13 AlignedDimensionEntity

- [ ] Creo una quota allineata.
- [ ] La quota segue l’angolo dei due punti.
- [ ] Il valore è la distanza reale tra i punti.
- [ ] Offset quota funziona durante il posizionamento.
- [ ] Testo e frecce sono leggibili.
- [ ] La quota usa lo stile quota corrente.
- [ ] Selezione e pannello proprietà funzionano.
- [ ] Move/Copy/Rotate/Scale/Mirror funzionano.
- [ ] Salva/riapri mantiene la quota.
- [ ] Export gestisce correttamente la quota o la limitazione è documentata.

### 5.14 RadiusDimensionEntity

- [ ] Creo una quota raggio su cerchio.
- [ ] Creo una quota raggio su arco.
- [ ] Il valore `R` è corretto.
- [ ] Leader/testo/freccia sono leggibili.
- [ ] La quota usa lo stile quota corrente.
- [ ] Selezione e pannello proprietà funzionano.
- [ ] Move/Copy/Rotate/Scale/Mirror funzionano.
- [ ] Salva/riapri mantiene la quota.
- [ ] Export gestisce correttamente la quota o la limitazione è documentata.

### 5.15 DiameterDimensionEntity

- [ ] Creo una quota diametro su cerchio.
- [ ] Creo una quota diametro su arco se supportato.
- [ ] Il valore `Ø` è corretto.
- [ ] Leader/testo/freccia sono leggibili.
- [ ] La quota usa lo stile quota corrente.
- [ ] Selezione e pannello proprietà funzionano.
- [ ] Move/Copy/Rotate/Scale/Mirror funzionano.
- [ ] Salva/riapri mantiene la quota.
- [ ] Export gestisce correttamente la quota o la limitazione è documentata.

### 5.16 AngularDimensionEntity

- [ ] Creo una quota angolare tra due linee.
- [ ] Creo una quota angolare con punti validi.
- [ ] Il valore angolare è corretto.
- [ ] L’arco di quota è disegnato correttamente.
- [ ] Testo e frecce sono leggibili.
- [ ] La quota usa lo stile quota corrente.
- [ ] Selezione e pannello proprietà funzionano.
- [ ] Move/Copy/Rotate/Scale/Mirror funzionano.
- [ ] Salva/riapri mantiene la quota.
- [ ] Export gestisce correttamente la quota o la limitazione è documentata.

### 5.17 ImageReferenceEntity

- [ ] Inserisco un’immagine PNG.
- [ ] Inserisco un’immagine JPG/JPEG.
- [ ] L’immagine appare con dimensioni corrette.
- [ ] L’immagine mantiene proporzioni iniziali corrette.
- [ ] L’immagine è selezionabile.
- [ ] Il pannello proprietà mostra percorso, origine, dimensioni e rotazione.
- [ ] Modifica origine aggiorna posizione.
- [ ] Modifica larghezza/altezza aggiorna dimensioni.
- [ ] Modifica rotazione aggiorna orientamento.
- [ ] Reset Aspect ripristina proporzioni corrette.
- [ ] Replace Image sostituisce l’immagine mantenendo dati coerenti.
- [ ] Snap agli angoli funziona.
- [ ] Snap ai midpoint dei lati funziona.
- [ ] Snap al centro funziona.
- [ ] Snap al bordo nearest funziona.
- [ ] Move funziona.
- [ ] Copy funziona.
- [ ] Rotate funziona.
- [ ] Scale funziona.
- [ ] Mirror funziona.
- [ ] Salva/riapri mantiene il riferimento.
- [ ] Se l’immagine manca, compare warning o stato Missing.
- [ ] Relink Missing funziona.
- [ ] Manage Refs mostra stato, percorso, pixel, dimensione CAD, rotazione, numero istanze.
- [ ] Collect Refs crea cartella `images/` accanto al file salvato.
- [ ] Dopo Collect Refs i path nel JSON sono relativi quando previsto.
- [ ] Spostando file disegno e cartella `images/` insieme, la riapertura funziona.
- [ ] SVG esporta o collega correttamente l’immagine se previsto.
- [ ] PDF/DXF gestiscono la limitazione raster in modo documentato.

### 5.18 BlockReferenceEntity

- [ ] Creo una definizione blocco da entità selezionate.
- [ ] Inserisco un riferimento blocco.
- [ ] Il blocco appare nella posizione corretta.
- [ ] Scala blocco corretta.
- [ ] Rotazione blocco corretta.
- [ ] Punto base blocco corretto.
- [ ] Il blocco è selezionabile come singola entità.
- [ ] Il pannello proprietà mostra dati coerenti.
- [ ] Move funziona.
- [ ] Copy funziona.
- [ ] Rotate funziona.
- [ ] Scale funziona.
- [ ] Mirror funziona.
- [ ] Explode scompone il blocco dove supportato.
- [ ] Salva/riapri mantiene definizione e riferimenti.
- [ ] Export gestisce il blocco correttamente o esplode/approssima in modo documentato.

---

## 6. Tool di disegno

### 6.1 Selection

- [ ] Click singolo seleziona un’entità.
- [ ] Click su spazio vuoto deseleziona quando previsto.
- [ ] Finestra left-to-right seleziona solo entità completamente interne.
- [ ] Crossing right-to-left seleziona entità intersecate.
- [ ] SHIFT/CTRL per modificare selezione funziona secondo comportamento previsto.
- [ ] CTRL+click cicla tra entità sovrapposte.
- [ ] Entità su layer nascosto non sono selezionabili.
- [ ] Entità su layer bloccato non sono selezionabili.
- [ ] Entità selezionata cambia solo colore/highlight senza alterare lineweight reale.
- [ ] La selezione multipla mostra proprietà aggregate quando previsto.

### 6.2 Zoom Window

- [ ] Il tool si attiva da pulsante.
- [ ] Il tool si attiva da alias.
- [ ] Primo click imposta primo angolo.
- [ ] Secondo click imposta finestra zoom.
- [ ] Preview rettangolo è visibile.
- [ ] ESC annulla senza cambiare vista.
- [ ] Zoom risultante include tutta l’area selezionata.

### 6.3 Point

- [ ] Tool attivabile da pulsante.
- [ ] Tool attivabile da command line.
- [ ] Inserimento con click funziona.
- [ ] Inserimento con coordinate funziona.
- [ ] Undo rimuove il punto.
- [ ] Redo ripristina il punto.

### 6.4 Text

- [ ] Tool attivabile da pulsante.
- [ ] Tool attivabile da alias.
- [ ] Click definisce punto inserimento.
- [ ] Dialog testo si apre correttamente.
- [ ] Conferma inserisce testo.
- [ ] Annulla non inserisce testo.
- [ ] ESC durante il workflow annulla.

### 6.5 MText

- [ ] Tool attivabile da pulsante.
- [ ] Tool attivabile da alias.
- [ ] Dialog multilinea si apre correttamente.
- [ ] Inserisco testo su più righe.
- [ ] Testo lungo si organizza correttamente.
- [ ] Annulla non inserisce entità.
- [ ] ESC annulla.

### 6.6 Line

- [ ] Tool attivabile da pulsante.
- [ ] Tool attivabile da alias.
- [ ] Due click creano linea.
- [ ] Coordinate da command line creano linea precisa.
- [ ] Distanza digitata crea linea con lunghezza corretta.
- [ ] Ortho funziona.
- [ ] Polar Tracking funziona.
- [ ] Snap durante il secondo punto funziona.
- [ ] ESC al primo punto annulla.
- [ ] ESC al secondo punto annulla senza creare linea parziale.

### 6.7 Rectangle

- [ ] Tool attivabile da pulsante.
- [ ] Tool attivabile da alias.
- [ ] Due angoli creano rettangolo corretto.
- [ ] Coordinate da command line funzionano.
- [ ] Distanze/ortho funzionano dove previsto.
- [ ] Il rettangolo genera una polyline chiusa o entità coerente.
- [ ] Undo/Redo funzionano.
- [ ] Salva/riapri mantiene chiusura.

### 6.8 Rect Sides

- [ ] Tool attivabile da pulsante.
- [ ] Tool attivabile da alias.
- [ ] Inserimento dimensioni lati funziona.
- [ ] Preview corrisponde alle dimensioni.
- [ ] Orientamento è coerente con i punti scelti.
- [ ] Undo/Redo funzionano.

### 6.9 Circle

- [ ] Tool attivabile da pulsante.
- [ ] Tool attivabile da alias.
- [ ] Centro + punto su raggio crea cerchio.
- [ ] Centro + raggio digitato crea cerchio preciso.
- [ ] Preview raggio è corretta.
- [ ] ESC annulla correttamente.

### 6.10 Ellipse

- [ ] Tool attivabile da pulsante.
- [ ] Tool attivabile da alias.
- [ ] Workflow assi/punti crea ellisse corretta.
- [ ] Preview assi è coerente.
- [ ] Input numerico funziona dove previsto.
- [ ] ESC annulla correttamente.

### 6.11 Arc

- [ ] Tool attivabile da pulsante.
- [ ] Tool attivabile da alias.
- [ ] Workflow crea arco corretto.
- [ ] Preview arco è coerente.
- [ ] Angoli e verso sono corretti.
- [ ] ESC annulla correttamente.

### 6.12 Arc 3P

- [ ] Tool attivabile da pulsante.
- [ ] Tool attivabile da alias.
- [ ] Tre punti non allineati creano arco corretto.
- [ ] Tre punti quasi allineati sono gestiti senza crash.
- [ ] Preview al terzo punto è coerente.
- [ ] ESC annulla correttamente.

### 6.13 Polyline

- [ ] Tool attivabile da pulsante.
- [ ] Tool attivabile da alias.
- [ ] Creo più segmenti consecutivi.
- [ ] `Undo` interno rimuove ultimo punto.
- [ ] `Close` chiude la polyline.
- [ ] INVIO termina polyline aperta.
- [ ] Click destro termina polyline aperta.
- [ ] Segmenti zero-length sono ignorati o gestiti senza errori.

### 6.14 Spline

- [ ] Tool attivabile da pulsante.
- [ ] Tool attivabile da alias.
- [ ] Creo spline con più punti di controllo.
- [ ] `Undo` interno funziona.
- [ ] `Close` funziona se supportato.
- [ ] INVIO termina spline aperta.
- [ ] Punti insufficienti sono gestiti senza crash.

### 6.15 Polygon

- [ ] Tool attivabile da pulsante.
- [ ] Tool attivabile da alias.
- [ ] Creo poligono con numero lati valido.
- [ ] Input numero lati non valido viene respinto.
- [ ] Preview poligono è coerente.
- [ ] Orientamento e dimensioni sono corretti.
- [ ] Salva/riapri mantiene il poligono.

---

## 7. Tool quote/dimensioni

- [ ] Tutti i tool quota sono nel gruppo corretto.
- [ ] Tutti i tool quota sono attivabili da pulsante.
- [ ] Tutti i tool quota sono attivabili da alias.
- [ ] Tutti i tool quota rispettano lo stile quota corrente.
- [ ] Tutti i tool quota producono entità selezionabili.
- [ ] Tutti i tool quota gestiscono ESC senza creare entità incomplete.
- [ ] Tutti i tool quota funzionano con snap.
- [ ] Tutti i tool quota funzionano con coordinate digitate quando previsto.
- [ ] Horizontal Dimension misura correttamente solo distanza orizzontale.
- [ ] Vertical Dimension misura correttamente solo distanza verticale.
- [ ] Aligned Dimension misura la distanza inclinata reale.
- [ ] Radius Dimension funziona su cerchi.
- [ ] Radius Dimension funziona su archi.
- [ ] Diameter Dimension funziona su cerchi.
- [ ] Angular Dimension funziona tra due linee.
- [ ] Angular Dimension gestisce angoli acuti/ottusi.
- [ ] Angular Dimension gestisce linee quasi parallele senza crash.

---

## 8. Tool modifica, trasformazione e editing

### 8.1 Delete

- [ ] Elimina selezione corrente.
- [ ] Se non c’è selezione entra in selezione o mostra messaggio coerente.
- [ ] Undo ripristina tutte le entità eliminate.
- [ ] Redo le elimina nuovamente.
- [ ] Non elimina entità su layer bloccato/nascosto.

### 8.2 Move

- [ ] Funziona con selezione già presente.
- [ ] Se non c’è selezione permette di selezionare entità.
- [ ] Punto base + punto destinazione muove correttamente.
- [ ] Distanza digitata muove correttamente.
- [ ] Coordinate digitate funzionano.
- [ ] Preview spostamento è coerente.
- [ ] ESC annulla senza modificare.
- [ ] Undo/Redo funzionano.

### 8.3 Copy

- [ ] Funziona con selezione già presente.
- [ ] Se non c’è selezione permette di selezionare entità.
- [ ] Crea copie senza modificare originali.
- [ ] Punto base + punto destinazione funzionano.
- [ ] Distanza digitata funziona.
- [ ] Copie multiple funzionano se previste.
- [ ] Undo rimuove solo le copie.
- [ ] Redo le ripristina.

### 8.4 Rotate

- [ ] Funziona con selezione già presente.
- [ ] Se non c’è selezione permette di selezionare entità.
- [ ] Punto base corretto.
- [ ] Angolo tramite mouse corretto.
- [ ] Angolo digitato corretto.
- [ ] Preview rotazione corretta.
- [ ] Undo/Redo funzionano.

### 8.5 Scale

- [ ] Funziona con selezione già presente.
- [ ] Se non c’è selezione permette di selezionare entità.
- [ ] Punto base corretto.
- [ ] Fattore tramite mouse corretto.
- [ ] Fattore digitato corretto.
- [ ] Fattore zero o negativo viene gestito correttamente.
- [ ] Preview scala corretta.
- [ ] Undo/Redo funzionano.

### 8.6 Align

- [ ] Tool attivabile da pulsante e alias.
- [ ] Selezione origine funziona.
- [ ] Coppie di punti di riferimento funzionano.
- [ ] Allineamento senza scala funziona.
- [ ] Allineamento con scala funziona se previsto.
- [ ] Preview è coerente.
- [ ] Undo/Redo funzionano.

### 8.7 Break Point

- [ ] Funziona su Line.
- [ ] Funziona su Arc.
- [ ] Funziona su Ellipse se supportato.
- [ ] Funziona su Polyline.
- [ ] Funziona su Spline aperta se supportato.
- [ ] Punto fuori entità viene respinto.
- [ ] Entità risultanti sono selezionabili.
- [ ] Undo/Redo funzionano.

### 8.8 Break Segment

- [ ] Funziona su Line.
- [ ] Funziona su Arc.
- [ ] Funziona su Circle.
- [ ] Funziona su Ellipse.
- [ ] Funziona su Polyline.
- [ ] Funziona su Spline aperta se supportato.
- [ ] Segmento nullo o troppo piccolo viene gestito senza crash.
- [ ] Undo/Redo funzionano.

### 8.9 Extend

- [ ] Estende Line verso bordo valido.
- [ ] Estende Arc se supportato.
- [ ] Estende Polyline aperta se supportato.
- [ ] Non modifica se non c’è intersezione valida.
- [ ] Gestisce target quasi paralleli.
- [ ] Preview evidenzia risultato o target.
- [ ] Undo/Redo funzionano.

### 8.10 Trim

- [ ] Trim con una linea tagliente funziona.
- [ ] Trim con più taglienti funziona.
- [ ] Opzione `All` funziona.
- [ ] Undo interno al comando funziona.
- [ ] Trim su Line funziona.
- [ ] Trim su Arc funziona.
- [ ] Trim su Polyline funziona.
- [ ] Taglienti ellittici funzionano se previsti.
- [ ] Click su segmento non trimmabile mostra messaggio coerente.
- [ ] Undo/Redo documento funzionano.

### 8.11 Offset

- [ ] Offset Line funziona su entrambi i lati.
- [ ] Offset Circle funziona interno/esterno.
- [ ] Offset Arc funziona.
- [ ] Offset Polyline aperta funziona.
- [ ] Offset Polyline chiusa funziona.
- [ ] Distanza da due punti funziona.
- [ ] Distanza digitata funziona.
- [ ] Ultima distanza viene riusata come default.
- [ ] Preview usa la stessa geometria del risultato finale.
- [ ] Distanza zero/non valida viene gestita.
- [ ] Undo/Redo funzionano.

### 8.12 Fillet

- [ ] Fillet Line-Line con raggio 0 funziona.
- [ ] Fillet Line-Line con raggio > 0 funziona.
- [ ] Opzione Radius aggiorna il raggio.
- [ ] Opzione Trim funziona.
- [ ] Opzione NoTrim funziona.
- [ ] Preview su seconda linea è coerente.
- [ ] Linee parallele sono gestite senza crash.
- [ ] Raggio troppo grande viene respinto o gestito correttamente.
- [ ] Undo/Redo funzionano.

### 8.13 Mirror

- [ ] Funziona con selezione corrente.
- [ ] Se non c’è selezione entra in fase di selezione.
- [ ] Primo punto asse mirror funziona.
- [ ] Secondo punto asse mirror funziona.
- [ ] Preview mirror corretta.
- [ ] Default mantiene sorgenti.
- [ ] Opzione Yes elimina/sostituisce sorgenti.
- [ ] Opzione No mantiene sorgenti.
- [ ] Asse degenerato viene gestito.
- [ ] Undo/Redo funzionano.

### 8.14 Explode

- [ ] Explode polyline aperta in linee.
- [ ] Explode polyline chiusa in linee.
- [ ] Explode blocco se supportato.
- [ ] Entità non supportate vengono ignorate o segnalate.
- [ ] Layer/stile delle entità risultanti sono coerenti.
- [ ] Undo/Redo funzionano.

### 8.15 Join

- [ ] Join due linee connesse crea polyline.
- [ ] Join catena di linee crea polyline aperta.
- [ ] Join catena chiusa crea polyline chiusa.
- [ ] Gruppi disconnessi generano più polyline.
- [ ] Linee non connesse non vengono unite erroneamente.
- [ ] Tolleranza connessione è coerente.
- [ ] Undo/Redo funzionano.

### 8.16 Draw order

- [ ] To Front porta entità sopra le altre.
- [ ] To Back porta entità sotto le altre.
- [ ] Forward sposta avanti di un livello.
- [ ] Backward sposta indietro di un livello.
- [ ] Draw order è indipendente dal layer.
- [ ] Salva/riapri mantiene draw order.
- [ ] Export rispetta l’ordine dove visivamente rilevante.

### 8.17 Align/Distribute da pannello o comandi UI

- [ ] Align Left funziona.
- [ ] Align Right funziona.
- [ ] Align Top funziona.
- [ ] Align Bottom funziona.
- [ ] Distribute Horizontally funziona con almeno 3 entità.
- [ ] Distribute Vertically funziona con almeno 3 entità.
- [ ] Con meno di 3 entità distribute mostra errore o disabilitazione coerente.
- [ ] Undo/Redo funzionano.

---

## 9. Grip editing

- [ ] I grip appaiono solo sulle entità selezionate.
- [ ] I grip sono leggibili a zoom normale.
- [ ] I grip sono utilizzabili a zoom alto.
- [ ] I grip non compaiono su entità layer nascosto.
- [ ] I grip non compaiono o non sono modificabili su entità layer bloccato.
- [ ] Grip Point funziona.
- [ ] Grip Line endpoint funziona.
- [ ] Grip Line midpoint funziona.
- [ ] Grip Circle center/radius funziona.
- [ ] Grip Arc funziona.
- [ ] Grip Ellipse funziona.
- [ ] Grip Polyline vertici funziona.
- [ ] Grip BezierSpline punti controllo funziona.
- [ ] Grip Text funziona.
- [ ] Grip MultilineText funziona.
- [ ] Grip ImageReference funziona.
- [ ] Grip editing genera comandi undoable.
- [ ] ESC durante grip editing annulla.

---

## 10. Snapping

- [ ] Snap endpoint funziona su linee.
- [ ] Snap endpoint funziona su archi.
- [ ] Snap endpoint funziona su polyline.
- [ ] Snap midpoint funziona su linee.
- [ ] Snap midpoint funziona su segmenti polyline.
- [ ] Snap center funziona su circle.
- [ ] Snap center funziona su arc.
- [ ] Snap center funziona su ellipse.
- [ ] Snap nearest funziona su linee.
- [ ] Snap nearest funziona su cerchi.
- [ ] Snap nearest funziona su archi.
- [ ] Snap nearest funziona su ellissi.
- [ ] Snap nearest funziona su polyline.
- [ ] Snap perpendicular funziona dove previsto.
- [ ] Snap intersection funziona tra due linee.
- [ ] Snap intersection funziona linea/cerchio dove previsto.
- [ ] Snap intersection funziona linea/arco dove previsto.
- [ ] Snap marker è visibile.
- [ ] Marker snap selection è un rettangolo semplice dove previsto.
- [ ] Il marker cambia posizione correttamente muovendo il mouse.
- [ ] Snap non considera layer nascosti.
- [ ] Snap può considerare layer bloccati solo se questa è la regola prevista; in caso contrario no.
- [ ] Snap su entità sovrapposte è stabile.
- [ ] Snap funziona durante Line.
- [ ] Snap funziona durante Polyline.
- [ ] Snap funziona durante Move/Copy.
- [ ] Snap funziona durante Dimension.
- [ ] Snap funziona su ImageReference: angoli, midpoint lati, centro, bordo nearest.

---

## 11. Ortho, Polar Tracking e griglia

### 11.1 Ortho

- [ ] Ortho attivabile/disattivabile dalla UI.
- [ ] Ortho vincola Line a 0/90°.
- [ ] Ortho vincola Move/Copy quando previsto.
- [ ] Ortho non interferisce con input numerico esplicito.
- [ ] Stato Ortho è visibile.

### 11.2 Polar Tracking

- [ ] Polar Off non vincola angoli.
- [ ] Polar 90° vincola correttamente.
- [ ] Polar 45° vincola correttamente.
- [ ] Polar 30° vincola correttamente.
- [ ] Polar 15° vincola correttamente.
- [ ] Polar Tracking non interferisce con snap prioritari.
- [ ] Stato Polar è visibile.

### 11.3 Griglia

- [ ] Griglia cartesiana visibile.
- [ ] Modifica passo griglia funziona.
- [ ] Modifica colore/opacità griglia funziona se previsto.
- [ ] Griglia non copre le entità.
- [ ] Griglia non copre tool panel/status bar.
- [ ] Griglia si aggiorna con zoom/pan.
- [ ] Griglia isometrica visibile se attiva.
- [ ] Le verticali della griglia isometrica passano dagli incroci delle diagonali.
- [ ] Impostazioni griglia persistono se previsto.

---

## 12. Layer, visibilità, blocco e proprietà grafiche

### 12.1 Layer Manager

- [ ] Layer Manager si apre senza errori.
- [ ] Creo un nuovo layer.
- [ ] Rinomino un layer.
- [ ] Elimino un layer non usato.
- [ ] Non posso eliminare layer obbligatori se previsto.
- [ ] Cambio layer corrente.
- [ ] Cambio colore layer.
- [ ] Cambio lineweight layer.
- [ ] Cambio line format layer.
- [ ] Salvo modifiche con OK.
- [ ] Annulla non applica modifiche.
- [ ] Undo/Redo delle modifiche layer funziona se previsto.

### 12.2 Visibilità layer

- [ ] Nascondo un layer con entità.
- [ ] Le entità del layer nascosto non sono renderizzate.
- [ ] Le entità del layer nascosto non sono selezionabili.
- [ ] Le entità del layer nascosto non sono considerate dagli snap.
- [ ] Le entità del layer nascosto non sono esportate se questa è la regola prevista.
- [ ] Riattivando il layer, le entità tornano visibili.

### 12.3 Blocco layer

- [ ] Blocco un layer con entità.
- [ ] Le entità restano visibili.
- [ ] Le entità non sono selezionabili.
- [ ] Le entità non sono modificabili.
- [ ] Gli snap su layer bloccato rispettano la regola prevista.
- [ ] Sbloccando il layer, le entità tornano modificabili.

### 12.4 Lineweight e rendering

- [ ] Lineweight a schermo usa solo il lineweight del layer.
- [ ] La selezione cambia colore ma non altera lo spessore reale.
- [ ] SVG usa stroke-width dal lineweight del layer.
- [ ] DXF esporta lineweight del layer.
- [ ] PDF rispetta spessori in modo coerente.

---

## 13. Line Format Manager

- [ ] Line Format Manager si apre senza errori.
- [ ] Formato `Continuous` presente.
- [ ] Formato `Dashed` presente.
- [ ] Formato `Dash-Dot` presente.
- [ ] Creo un nuovo line format.
- [ ] Modifico nome line format.
- [ ] Modifico pattern tratteggio.
- [ ] Pattern non valido viene respinto.
- [ ] Assegno line format a un layer.
- [ ] Il canvas aggiorna il tratteggio.
- [ ] Export SVG mantiene tratteggio.
- [ ] Export DXF mappa correttamente linetype.
- [ ] Salva/riapri mantiene line formats.

---

## 14. Text Format Manager

- [ ] Text Format Manager si apre senza errori.
- [ ] Formato testo di default presente.
- [ ] Creo nuovo text format.
- [ ] Modifico nome formato.
- [ ] Modifico font family se previsto.
- [ ] Modifico altezza default.
- [ ] Modifico stile/parametri disponibili.
- [ ] Assegno text format a Text.
- [ ] Assegno text format a MText.
- [ ] Salva/riapri mantiene text formats.
- [ ] Export mantiene o approssima correttamente il formato.

---

## 15. Dimension Style Manager

- [ ] Dimension Style Manager si apre senza errori.
- [ ] Stile quota di default presente.
- [ ] Creo nuovo stile quota.
- [ ] Rinomino stile quota.
- [ ] Modifico text format associato.
- [ ] Modifico altezza testo quota.
- [ ] Modifico frecce/terminatori se previsto.
- [ ] Modifico fit testo/terminatori se previsto.
- [ ] Preview stile quota si aggiorna.
- [ ] Applico stile quota corrente.
- [ ] Nuove quote usano lo stile selezionato.
- [ ] Quote esistenti mantengono o aggiornano stile secondo comportamento previsto.
- [ ] Salva/riapri mantiene dimension styles.

---

## 16. Pannello proprietà

- [ ] Selezione singola mostra proprietà specifiche.
- [ ] Selezione multipla mostra proprietà comuni.
- [ ] Modifica layer funziona.
- [ ] Modifica colore/stile se previsto funziona.
- [ ] Modifica proprietà geometriche Line funziona.
- [ ] Modifica proprietà Circle funziona.
- [ ] Modifica proprietà Arc funziona.
- [ ] Modifica proprietà Ellipse funziona.
- [ ] Modifica proprietà Polyline funziona o è disabilitata coerentemente.
- [ ] Modifica proprietà Text funziona.
- [ ] Modifica proprietà MText funziona.
- [ ] Modifica proprietà ImageReference funziona.
- [ ] Modifica proprietà Dimension funziona.
- [ ] Valori non validi vengono respinti.
- [ ] Modifiche generano Undo/Redo.
- [ ] Il pannello non genera eccezioni con selezioni miste.

---

## 17. Misure

### 17.1 Measure Distance

- [ ] Due punti misurano distanza corretta.
- [ ] ΔX e ΔY sono corretti.
- [ ] Snap funziona durante misura.
- [ ] Il tool non modifica il documento.
- [ ] ESC annulla.

### 17.2 Measure Entity

- [ ] Misura Line: lunghezza corretta.
- [ ] Misura Circle: raggio/diametro/circonferenza/area se previsti.
- [ ] Misura Arc: lunghezza arco/raggio/angoli se previsti.
- [ ] Misura Polyline: lunghezza totale corretta.
- [ ] Misura Polyline chiusa: area se prevista.
- [ ] Misura entità non supportata mostra messaggio coerente.
- [ ] Il tool non modifica il documento.

### 17.3 Measure Angle

- [ ] Misura angolo tra due linee.
- [ ] Misura angolo acuto corretto.
- [ ] Misura angolo ottuso corretto.
- [ ] Linee parallele sono gestite.
- [ ] Il tool non modifica il documento.

### 17.4 Measure Area

- [ ] Misura area di polyline chiusa.
- [ ] Misura area rettangolo corretta.
- [ ] Misura area cerchio se supportata.
- [ ] Entità aperta viene respinta.
- [ ] Il tool non modifica il documento.

---

## 18. Undo/Redo e Command History

- [ ] Add entity è undoable.
- [ ] Delete è undoable.
- [ ] Move è undoable.
- [ ] Copy è undoable.
- [ ] Rotate è undoable.
- [ ] Scale è undoable.
- [ ] Mirror è undoable.
- [ ] Trim è undoable.
- [ ] Extend è undoable.
- [ ] Offset è undoable.
- [ ] Fillet è undoable.
- [ ] Explode è undoable.
- [ ] Join è undoable.
- [ ] Grip editing è undoable.
- [ ] Modifiche proprietà sono undoable.
- [ ] Modifiche layer sono undoable se previsto.
- [ ] Modifiche line format sono undoable se previsto.
- [ ] Modifiche text format sono undoable se previsto.
- [ ] Modifiche dimension style sono undoable se previsto.
- [ ] Dopo nuovo comando, redo stack viene pulito.
- [ ] Undo multipli non corrompono il documento.
- [ ] Redo multipli non corrompono il documento.
- [ ] Undo/Redo dopo salvataggio funzionano secondo comportamento previsto.

---

## 19. Persistenza `.opencad2d.json`

### 19.1 Save/Open base

- [ ] Save crea file non vuoto.
- [ ] Save As crea file nel percorso scelto.
- [ ] Open carica file valido.
- [ ] Recent/current path aggiornato correttamente.
- [ ] Dirty flag si azzera dopo Save.
- [ ] Dirty flag si attiva dopo modifica.
- [ ] Chiudendo con modifiche compare richiesta salvataggio.
- [ ] Save Changes: Save funziona.
- [ ] Save Changes: Don’t Save funziona.
- [ ] Save Changes: Cancel annulla chiusura.

### 19.2 Roundtrip entità

- [ ] Point roundtrip.
- [ ] Line roundtrip.
- [ ] Circle roundtrip.
- [ ] Arc roundtrip.
- [ ] Ellipse roundtrip.
- [ ] EllipticalArc roundtrip.
- [ ] Polyline aperta roundtrip.
- [ ] Polyline chiusa roundtrip.
- [ ] BezierSpline roundtrip.
- [ ] Text roundtrip.
- [ ] MText roundtrip.
- [ ] HorizontalDimension roundtrip.
- [ ] VerticalDimension roundtrip.
- [ ] AlignedDimension roundtrip.
- [ ] RadiusDimension roundtrip.
- [ ] DiameterDimension roundtrip.
- [ ] AngularDimension roundtrip.
- [ ] ImageReference roundtrip.
- [ ] BlockDefinition roundtrip.
- [ ] BlockReference roundtrip.

### 19.3 Roundtrip impostazioni/stili

- [ ] Layers roundtrip.
- [ ] Layer corrente roundtrip.
- [ ] Layer visibilità roundtrip.
- [ ] Layer lock roundtrip.
- [ ] Line formats roundtrip.
- [ ] Text formats roundtrip.
- [ ] Dimension styles roundtrip.
- [ ] Viewport state roundtrip se previsto.
- [ ] Draw order roundtrip.
- [ ] Fill state roundtrip.
- [ ] Stale dimension state roundtrip.

### 19.4 Robustezza file

- [ ] File inesistente mostra errore chiaro.
- [ ] File JSON non valido mostra errore chiaro.
- [ ] Versione documento non supportata mostra errore chiaro.
- [ ] Entità sconosciuta viene gestita con recovery o warning.
- [ ] Path immagine assoluti/relativi sono risolti correttamente.
- [ ] Recovery non perde entità valide quando trova una non valida.

---

## 20. Import

### 20.1 Import `.opencad2d.json`

- [ ] Import Drawing apre finestra opzioni.
- [ ] Import in documento vuoto funziona.
- [ ] Import in documento esistente aggiunge entità.
- [ ] Opzioni posizionamento funzionano.
- [ ] Layer importati non rompono layer esistenti.
- [ ] Line formats importati sono fusi correttamente.
- [ ] Text formats importati sono fusi correttamente.
- [ ] Dimension styles importati sono fusi correttamente.
- [ ] Block definitions importate sono fuse correttamente.
- [ ] Conflitti nome/id sono gestiti correttamente.
- [ ] Undo import rimuove tutto il contenuto importato.

### 20.2 Import DXF

- [ ] Import DXF apre file valido.
- [ ] Report import DXF si apre quando ci sono warning.
- [ ] Line DXF importata correttamente.
- [ ] Circle DXF importato correttamente.
- [ ] Arc DXF importato correttamente.
- [ ] Polyline/LWPolyline DXF importata correttamente dove supportata.
- [ ] Layer DXF importati correttamente.
- [ ] Colori DXF mappati correttamente dove supportati.
- [ ] Linetype DXF mappati correttamente dove supportati.
- [ ] Entità DXF non supportate generano warning, non crash.
- [ ] Statistiche import sono plausibili.

---

## 21. Export

Creare un file misto con almeno: Line, Circle, Arc, Ellipse, Polyline aperta/chiusa, Text, MText, quote, layer multipli, line format multipli, fill, image reference e blocco se supportato.

### 21.1 SVG

- [ ] Finestra impostazioni SVG si apre.
- [ ] Export SVG crea file non vuoto.
- [ ] SVG si apre in browser.
- [ ] Coordinate/orientamento sono corretti.
- [ ] Lineweight deriva dal layer.
- [ ] Colori layer corretti.
- [ ] Tratteggi corretti.
- [ ] Testo leggibile.
- [ ] Quote leggibili.
- [ ] Fill visibile.
- [ ] Draw order rispettato.
- [ ] Image reference visibile o collegata correttamente quando supportato.
- [ ] Layer nascosti gestiti secondo regola prevista.

### 21.2 PDF

- [ ] Finestra impostazioni PDF si apre.
- [ ] Page size selezionabile.
- [ ] Orientamento pagina selezionabile.
- [ ] Export PDF crea file non vuoto.
- [ ] PDF si apre in viewer.
- [ ] Geometria vettoriale visibile.
- [ ] Lineweight coerenti.
- [ ] Testo leggibile.
- [ ] Quote leggibili.
- [ ] Fill supportato visibile.
- [ ] Raster images assenti o gestite secondo limitazioni documentate.
- [ ] Disegno centrato/scalato correttamente.

### 21.3 DXF

- [ ] Export DXF crea file non vuoto.
- [ ] DXF si apre in LibreCAD.
- [ ] DXF si apre in QCAD.
- [ ] DXF si apre in Autodesk viewer se disponibile.
- [ ] Layer esportati correttamente.
- [ ] Colori layer esportati correttamente.
- [ ] Lineweight layer esportati correttamente.
- [ ] Linetype esportati correttamente.
- [ ] Line visibili.
- [ ] Circle visibili.
- [ ] Arc visibili.
- [ ] Polyline visibili.
- [ ] Text visibili.
- [ ] Quote gestite correttamente o limitazione documentata.
- [ ] HATCH solid fill visibile dove supportato.
- [ ] Y inversion/orientamento è corretto.
- [ ] Raster images assenti o gestite secondo limitazioni documentate.

### 21.4 PNG

- [ ] Export PNG disponibile se implementato nella UI.
- [ ] PNG creato correttamente.
- [ ] Dimensioni immagine coerenti.
- [ ] Sfondo coerente con impostazioni.
- [ ] Geometria leggibile.
- [ ] Lineweight e colori coerenti.

---

## 22. Finestre e dialoghi applicativi

- [ ] AboutWindow si apre.
- [ ] AboutWindow mostra `OpenCad2D`.
- [ ] AboutWindow mostra creator `Emilie Rollandin`.
- [ ] AboutWindow mostra contatto corretto.
- [ ] CreateBlockOptionsWindow si apre.
- [ ] InsertBlockOptionsWindow si apre senza warning AVLN3001.
- [ ] GridSettingsWindow si apre e salva impostazioni.
- [ ] LayerManagerWindow si apre e salva modifiche.
- [ ] LineFormatManagerWindow si apre e salva modifiche.
- [ ] TextFormatManagerWindow si apre e salva modifiche.
- [ ] DimensionStyleManagerWindow si apre e salva modifiche.
- [ ] ImageReferenceManagerWindow si apre e mostra riferimenti.
- [ ] ImportDrawingOptionsWindow si apre e conferma opzioni.
- [ ] DxfImportReportWindow si apre quando necessario.
- [ ] SvgExportSettingsWindow si apre e conferma opzioni.
- [ ] PdfExportSettingsWindow si apre e conferma opzioni.
- [ ] TextInputWindow si apre per Text.
- [ ] TextInputWindow si apre per MText.
- [ ] SaveChangesWindow gestisce Save/Don’t Save/Cancel.
- [ ] Tutte le finestre hanno titolo corretto.
- [ ] Tutte le finestre hanno pulsanti OK/Cancel coerenti.
- [ ] Nessuna finestra resta dietro la principale in modo problematico.

---

## 23. Blocchi

- [ ] Creo blocco da una selezione semplice.
- [ ] Creo blocco da selezione mista.
- [ ] Punto base viene scelto correttamente.
- [ ] Annullare scelta punto base non modifica documento.
- [ ] Inserisco blocco con scala 1.
- [ ] Inserisco blocco con scala diversa.
- [ ] Inserisco blocco con rotazione.
- [ ] Inserimento con coordinate funziona.
- [ ] Preview inserimento blocco funziona.
- [ ] Salva/riapri mantiene definizioni blocco.
- [ ] Salva/riapri mantiene riferimenti blocco.
- [ ] Explode blocco funziona se supportato.
- [ ] DXF/SVG/PDF gestiscono blocchi correttamente o con limitazioni documentate.

---

## 24. Immagini esterne

- [ ] Attach image da UI funziona.
- [ ] Percorso assoluto viene memorizzato/convertito secondo regola prevista.
- [ ] Percorso relativo viene risolto correttamente.
- [ ] Missing image rilevata alla riapertura.
- [ ] Relink Missing aggiorna il path.
- [ ] Replace Image aggiorna riferimento.
- [ ] Open Folder apre cartella corretta dove supportato.
- [ ] Collect Refs copia immagini accanto al disegno.
- [ ] Collect Refs non duplica inutilmente file già raccolti.
- [ ] Manage Refs seleziona istanza nel disegno.
- [ ] Manage Refs mostra numero istanze corretto.
- [ ] Manage Refs gestisce immagini mancanti senza crash.

---

## 25. Performance e robustezza

- [ ] Documento con 100 entità resta fluido.
- [ ] Documento con 1.000 entità resta utilizzabile.
- [ ] Zoom/pan su documento grande resta accettabile.
- [ ] Selection window su molte entità resta accettabile.
- [ ] Spatial index/culling non nasconde entità visibili.
- [ ] Undo/Redo su molte entità resta corretto.
- [ ] Salvataggio documento grande produce file valido.
- [ ] Riapertura documento grande funziona.
- [ ] Operazioni ripetute non generano memory leak evidente.
- [ ] Nessuna eccezione in console/log durante uso normale.

---

## 26. Casi limite geometrici

- [ ] Linea di lunghezza zero viene impedita o gestita.
- [ ] Cerchio raggio zero viene impedito o gestito.
- [ ] Ellisse con asse zero viene impedita o gestita.
- [ ] Arco con punti collineari viene impedito o gestito.
- [ ] Polyline con vertici duplicati viene gestita.
- [ ] Spline con punti insufficienti viene gestita.
- [ ] Trim con entità parallele non va in crash.
- [ ] Extend senza intersezione non modifica il documento.
- [ ] Offset con distanza enorme viene gestito.
- [ ] Offset con distanza molto piccola viene gestito.
- [ ] Fillet con raggio troppo grande viene gestito.
- [ ] Mirror con asse degenerato viene respinto.
- [ ] Scale con fattore zero viene respinto.
- [ ] Coordinate molto grandi non rompono rendering/export.
- [ ] Coordinate molto piccole non rompono snapping/tolleranze.

---

## 27. Compatibilità file e viewer esterni

### 27.1 DXF viewer matrix

| Viewer | Versione | OS | Esito | Note |
|---|---:|---|---|---|
| LibreCAD |  |  |  |  |
| QCAD |  |  |  |  |
| Autodesk DWG TrueView / altro Autodesk |  |  |  |  |
| Altro viewer |  |  |  |  |

- [ ] I risultati sono annotati in `docs/dxf-compatibility.md`.
- [ ] Eventuali limiti sono riportati in `docs/known-limitations.md`.

### 27.2 SVG viewer matrix

| Viewer | Versione | OS | Esito | Note |
|---|---:|---|---|---|
| Firefox |  |  |  |  |
| Chrome / Edge |  |  |  |  |
| Inkscape |  |  |  |  |

- [ ] SVG apre correttamente in browser.
- [ ] SVG apre correttamente in editor vettoriale.

### 27.3 PDF viewer matrix

| Viewer | Versione | OS | Esito | Note |
|---|---:|---|---|---|
| Acrobat Reader |  |  |  |  |
| Firefox PDF viewer |  |  |  |  |
| Edge PDF viewer |  |  |  |  |

- [ ] PDF leggibile in viewer multipli.
- [ ] Dimensioni pagina corrette.
- [ ] Scala e centratura corrette.

---

## 28. Documentazione da sincronizzare dopo il collaudo

- [ ] `README.md` riflette le funzioni effettive.
- [ ] `docs/ai-handoff.md` aggiornato con stato corrente.
- [ ] `docs/architecture.md` aggiornato se sono emersi cambi architetturali.
- [ ] `docs/tools.md` aggiornato se un tool cambia comportamento.
- [ ] `docs/commands.md` aggiornato se alias/comandi cambiano.
- [ ] `docs/snapping.md` aggiornato se snap cambia comportamento.
- [ ] `docs/modify-tools.md` aggiornato se Trim/Extend/Offset/Fillet/Break/Join/Explode cambiano.
- [ ] `docs/text-and-dimensions.md` aggiornato per Text/MText/quote.
- [ ] `docs/line-formats.md` aggiornato per formati linea.
- [ ] `docs/layer-appearance.md` aggiornato per layer/lineweight/visibility/lock.
- [ ] `docs/export.md` aggiornato.
- [ ] `docs/svg-export.md` aggiornato.
- [ ] `docs/pdf-export.md` aggiornato.
- [ ] `docs/dxf-export.md` aggiornato.
- [ ] `docs/dxf-import.md` aggiornato.
- [ ] `docs/known-limitations.md` aggiornato.
- [ ] Roadmap aggiornata senza contraddizioni tra cose completate e cose ancora segnate come pending.

---

## 29. Checklist finale pre-release

- [ ] Build pulita.
- [ ] Test automatici passati.
- [ ] Smoke test manuale completato.
- [ ] Tutte le entità principali create e salvate/riaperte.
- [ ] Tutti i tool registrati provati almeno una volta.
- [ ] Tutti gli alias principali provati.
- [ ] Undo/Redo provati su operazioni critiche.
- [ ] Layer nascosti/bloccati verificati.
- [ ] Snap verificati.
- [ ] Grip editing verificato.
- [ ] Export SVG verificato.
- [ ] Export PDF verificato.
- [ ] Export DXF verificato.
- [ ] Import DXF verificato se incluso nella release.
- [ ] Import `.opencad2d.json` verificato se incluso nella release.
- [ ] Blocchi verificati se inclusi nella release.
- [ ] Immagini esterne verificate se incluse nella release.
- [ ] Documentazione aggiornata.
- [ ] Known limitations aggiornate.
- [ ] Release notes aggiornate.
- [ ] Nessun bug bloccante aperto.
- [ ] Eventuali bug minori sono elencati con workaround o priorità.

---

## 30. Tabella problemi trovati

| ID | Area | Gravità | Descrizione | Passi per riprodurre | Esito atteso | Esito ottenuto | Screenshot/File | Stato |
|---|---|---|---|---|---|---|---|---|
| 001 |  |  |  |  |  |  |  |  |
| 002 |  |  |  |  |  |  |  |  |
| 003 |  |  |  |  |  |  |  |  |

---

## 31. File minimo consigliato per test completo

Creare un file di prova chiamato, ad esempio:

```text
manual-validation-full.opencad2d.json
```

Contenuto consigliato:

- [ ] Layer 0: linee base.
- [ ] Walls: rettangolo/polyline chiusa con lineweight alto.
- [ ] Axis: linee dash-dot.
- [ ] Construction lines: linee dashed.
- [ ] Annotations: testi, MText e quote.
- [ ] Almeno un punto.
- [ ] Almeno una linea orizzontale, verticale e inclinata.
- [ ] Almeno un cerchio.
- [ ] Almeno un arco.
- [ ] Almeno un’ellisse.
- [ ] Almeno una polyline aperta.
- [ ] Almeno una polyline chiusa con fill.
- [ ] Almeno una spline.
- [ ] Almeno un testo.
- [ ] Almeno un MText.
- [ ] Almeno una quota orizzontale.
- [ ] Almeno una quota verticale.
- [ ] Almeno una quota allineata.
- [ ] Almeno una quota raggio.
- [ ] Almeno una quota diametro.
- [ ] Almeno una quota angolare.
- [ ] Almeno un’immagine PNG.
- [ ] Almeno un’immagine JPG.
- [ ] Almeno un blocco con riferimento inserito.
- [ ] Entità sovrapposte per test CTRL+click cycle.
- [ ] Entità su layer nascosto.
- [ ] Entità su layer bloccato.
- [ ] Entità con draw order diverso.

---

## 32. Decisione finale

- [ ] Release approvata.
- [ ] Release non approvata.
- [ ] Serve nuova build di correzione.

Note finali:

```text


```
