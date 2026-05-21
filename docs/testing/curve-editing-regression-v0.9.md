# Curve Editing Regression Checklist v0.9

Questa checklist serve per verificare manualmente il blocco **Curve Editing v0.9** dopo l’introduzione di:

- `CadCurveSplitService`
- `ICurveAdapter`
- `CurveCut`
- `CurveInterval`
- `CadIntersectionPoint`
- `EllipticalArcEntity`
- `BezierSplineSplitService`
- preview UX per TRIM / BREAK / EXTEND

Obiettivo principale:

> Le operazioni CAD devono modificare entità native usando parametri geometrici nativi.  
> Il campionamento è ammesso solo come supporto provvisorio, mai come fonte definitiva della geometria modificata.


## Sessione consigliata per questa sera

Questa sessione non deve cercare di completare tutta la checklist. L'obiettivo è fare una prima passata utile, concentrata sui comandi che hanno più probabilità di rivelare regressioni dopo i refactor recenti.

Ordine consigliato:

1. **Preparazione e smoke test**: completare `PREP-01` → `PREP-05`.
2. **TRIM base**: completare `TRIM-GEN-*`, `TRIM-LINE-*`, `TRIM-CIRC-*`.
3. **TRIM curve avanzate**: completare `TRIM-ELL-*` e `TRIM-SPL-*` se il tempo lo permette.
4. **BREAK**: completare almeno `BRKPT-01` → `BRKPT-05` e `BRKSEG-01` → `BRKSEG-09`.
5. **EXTEND**: completare `EXT-01` → `EXT-14`, segnando subito i casi su curve chiuse come OK solo se il messaggio è chiaro.
6. **Micro-gap**: completare almeno `GAP-01` → `GAP-06`, zoomando molto sulle intersezioni.
7. **Chiusura sessione**: compilare `Bug trovati`, `Deferred confermati` e la tabella `Risultato finale regression`, anche se parziale.

Criterio pratico: se trovi un bug grave o ripetibile, fermati e annotalo bene invece di continuare a spuntare casi. Per questa fase è più utile un bug riproducibile con pochi passi che una checklist completata superficialmente.

### File di prova consigliato

Per cominciare subito la sessione è disponibile un file sample già pronto:

```text
docs/testing/samples/curve-editing-regression-v0.9.opencad2d.json
```

Per la passata serale usare anche il foglio operativo:

```text
docs/testing/curve-editing-evening-run-2026-05-21.md
```

In alternativa, creare un singolo disegno manuale chiamato, ad esempio:

```text
curve-editing-regression-v0.9.opencad2d.json
```

Contenuto minimo consigliato:

- due linee incrociate;
- una linea che attraversa un cerchio;
- una linea che attraversa un arco;
- una polilinea aperta con almeno 4 vertici;
- un rettangolo/poligono come polilinea chiusa;
- una ellisse completa;
- un arco ellittico;
- una Bezier aperta;
- almeno due layer, uno normale e uno locked/hidden per i test di selezione.

Salvare una copia prima di ogni gruppo di prove distruttive, oppure usare Undo/Redo sistematicamente dopo ogni comando.

### Priorità bug

| Gravità | Quando usarla | Esempi |
|---|---|---|
| Blocker | impedisce di continuare la sessione | crash, eccezione, file non più apribile |
| High | comando produce geometria sbagliata o perde il tipo nativo | ellisse trasformata in polilinea, trim lato sbagliato |
| Medium | comportamento scorretto ma aggirabile | preview incoerente, messaggio confuso, undo parziale |
| Low | dettaglio UX o documentale | testo non chiaro, piccolo problema visivo |

### Template bug consigliato

```text
ID: BUG-xx
Comando: TRIM / BREAK AT POINT / BREAK SEGMENT / EXTEND
Entità coinvolte:
Passi per riprodurre:
Risultato atteso:
Risultato ottenuto:
Gravità: Blocker / High / Medium / Low
Note/screenshot:
```


## Status messaging checks for complex geometry

The curve-editing tools must never fail silently. During regression testing, verify both the geometry result and the command/status feedback shown to the user.

Expected message categories:

| Area | Scenario | Expected feedback |
|---|---|---|
| TRIM | Target does not intersect the selected cutting edge | Message states that no trim intersection was found and suggests selecting crossing geometry. |
| TRIM | Intersections exist, but the picked side cannot be removed | Message states that the picked side does not produce a removable interval and suggests picking the valid side. |
| TRIM | Closed spline target | Message states that closed splines cannot be trimmed yet and suggests using an open spline or conversion/explode workflow. |
| BREAK AT POINT | Pick is not on the selected entity | Message states that the break point is not on the entity and suggests picking directly on the entity or enabling snaps. |
| BREAK AT POINT | Pick is too close to endpoint/vertex | Message states that the point is too close to an endpoint, vertex, or tolerance-sensitive area. |
| BREAK AT POINT | Closed spline target | Message states that Break Point does not support closed splines yet. |
| BREAK SEGMENT | Two break points are coincident or too close | Message states that two distinct points are required. |
| BREAK SEGMENT | Second point is not on the selected entity | Message states that the second break point is not on the entity. |
| BREAK SEGMENT | Closed spline segment removal | Message states that closed spline segment removal is not supported yet. |
| EXTEND | Projected target does not intersect the boundary | Message states that no extension is possible because the projected entity does not intersect the boundary. |
| EXTEND | Boundary is reachable only from the opposite side | Message states that the boundary intersects the projected entity, but not beyond the picked endpoint side. |

When a result is `Deferred`, it is acceptable only if the message explains the limitation and no geometry is modified.

The same rule applies while hovering: if TRIM or EXTEND cannot produce a preview over a hovered target, the status text should explain the likely cause instead of falling back to a generic “select a valid entity” message.

## Come usare la checklist

Per ogni prova, segnare:

| Stato | Significato |
|---|---|
| OK | comportamento corretto |
| Bug | comportamento errato da correggere |
| Deferred | comportamento noto e rinviato |
| N/A | non applicabile alla build corrente |

Quando possibile, verificare anche:

- tipo entità risultante;
- assenza di micro-gap;
- preview coerente con il risultato reale;
- undo/redo;
- salvataggio e riapertura;
- export SVG/PDF/DXF.

---

## 1. Preparazione ambiente

| ID | Prova | Risultato atteso | Stato | Note |
|---|---|---|---|---|
| PREP-01 | Aprire OpenCad2D da build pulita | L'app si avvia senza errori |  |  |
| PREP-02 | Creare nuovo documento | Documento vuoto, layer iniziali corretti |  |  |
| PREP-03 | Verificare zoom/pan/selezione base | Canvas reattivo |  |  |
| PREP-04 | Salvare un file `.opencad2d.json` di prova | `CurrentFilePath` aggiornato, dirty state azzerato |  |  |
| PREP-05 | Modificare il documento dopo il salvataggio | Dirty state attivo |  |  |

---

## 2. TRIM — comportamento generale

| ID | Prova | Risultato atteso | Stato | Note |
|---|---|---|---|---|
| TRIM-GEN-01 | Avviare TRIM | Messaggio iniziale chiaro |  |  |
| TRIM-GEN-02 | Verificare snap durante TRIM | Solo snap/selezione entità attivo, non endpoint/midpoint/etc. |  |  |
| TRIM-GEN-03 | Selezionare boundary e muovere il mouse su target | Preview visibile |  |  |
| TRIM-GEN-04 | Preview TRIM | La parte che verrà rimossa è tratteggiata |  |  |
| TRIM-GEN-05 | Eseguire TRIM e Undo | Undo ripristina esattamente lo stato precedente |  |  |
| TRIM-GEN-06 | Eseguire Redo dopo Undo | Redo ripristina il trim |  |  |
| TRIM-GEN-07 | Premere ESC durante TRIM | Tool cancellato o stato intermedio pulito |  |  |
| TRIM-GEN-08 | Selezionare target non supportato | Messaggio chiaro, nessuna modifica sporca |  |  |

---

## 3. TRIM — LineEntity

| ID | Prova | Risultato atteso | Stato | Note |
|---|---|---|---|---|
| TRIM-LINE-01 | Tagliare una linea con una linea boundary | Frammento corretto, endpoint sul punto di intersezione |  |  |
| TRIM-LINE-02 | Tagliare una linea con due boundary | Rimosso solo intervallo scelto dal pick |  |  |
| TRIM-LINE-03 | Pick su lato esterno sinistro | Frammento destro fuso correttamente |  |  |
| TRIM-LINE-04 | Pick su lato esterno destro | Frammento sinistro fuso correttamente |  |  |
| TRIM-LINE-05 | Trim reciproco di due linee | Endpoint coincidenti, nessun micro-gap |  |  |
| TRIM-LINE-06 | Trim linea con circle boundary | Endpoint sul cerchio e sulla linea |  |  |
| TRIM-LINE-07 | Trim linea con ellipse boundary | Endpoint condiviso e nessun gap visibile |  |  |

---

## 4. TRIM — CircleEntity e ArcEntity

| ID | Prova | Risultato atteso | Stato | Note |
|---|---|---|---|---|
| TRIM-CIRC-01 | Tagliare cerchio con una linea | Risultato `ArcEntity`, non polyline |  |  |
| TRIM-CIRC-02 | Tagliare cerchio con due linee boundary | Archi corretti, preview coerente |  |  |
| TRIM-CIRC-03 | Tagliare cerchio con ellipse boundary | Endpoint su cerchio ed ellisse; distanza tra punti condivisi non visibile |  |  |
| TRIM-CIRC-04 | Tagliare arco con linea | Risultato `ArcEntity`, centro/raggio preservati |  |  |
| TRIM-CIRC-05 | Tagliare arco con più boundary | Intervallo scelto dal pick rimosso correttamente |  |  |
| TRIM-CIRC-06 | Tagliare arco con ellipse boundary | Endpoint su arco ed ellisse entro tolleranza CAD |  |  |
| TRIM-CIRC-07 | Caso tangente linea/cerchio | Nessun frammento degenerato o comportamento instabile |  |  |

---

## 5. TRIM — Polyline, Rectangle, Polygon

| ID | Prova | Risultato atteso | Stato | Note |
|---|---|---|---|---|
| TRIM-PL-01 | Tagliare polyline aperta con linea | Polyline frammentata preservando vertici intermedi |  |  |
| TRIM-PL-02 | Tagliare polyline con due boundary | Intervallo corretto rimosso |  |  |
| TRIM-PL-03 | Tagliare closed polyline / polygon | Risultato polyline aperta coerente |  |  |
| TRIM-PL-04 | Tagliare rectangle rappresentato come polyline chiusa | Risultato polyline aperta, non rectangle semantico |  |  |
| TRIM-PL-05 | Trim polyline con ellipse boundary | Vertice di taglio coincidente con intersezione nativa |  |  |
| TRIM-PL-06 | Trim con pick vicino a un vertice | Nessun segmento degenerato indesiderato |  |  |

---

## 6. TRIM — EllipseEntity ed EllipticalArcEntity

| ID | Prova | Risultato atteso | Stato | Note |
|---|---|---|---|---|
| TRIM-ELL-01 | Tagliare ellisse completa con linea | Risultato `EllipticalArcEntity`, non `PolylineEntity` |  |  |
| TRIM-ELL-02 | Tagliare ellisse completa con due linee | Frammenti ellittici nativi corretti |  |  |
| TRIM-ELL-03 | Tagliare ellisse completa con polyline | Endpoint sulla polyline e sull'ellisse |  |  |
| TRIM-ELL-04 | Tagliare ellisse completa con cerchio | Endpoint condivisi; nessuna distanza visibile tra cerchio ed ellisse |  |  |
| TRIM-ELL-05 | Tagliare arco ellittico con linea | Risultato `EllipticalArcEntity` |  |  |
| TRIM-ELL-06 | Tagliare arco ellittico con polyline | Endpoint nativo preciso, non campionato |  |  |
| TRIM-ELL-07 | Tagliare arco ellittico con cerchio/arco | Endpoint su entrambe le curve native |  |  |
| TRIM-ELL-08 | Export SVG/PDF/DXF dopo trim ellisse | Entità visibile e coerente |  |  |
| TRIM-ELL-09 | Salva/riapri dopo trim ellisse | Rimane `EllipticalArcEntity` |  |  |

---

## 7. TRIM — BezierSplineEntity aperta

| ID | Prova | Risultato atteso | Stato | Note |
|---|---|---|---|---|
| TRIM-SPL-01 | Tagliare spline aperta con linea | Risultato `BezierSplineEntity`, non `PolylineEntity` |  |  |
| TRIM-SPL-02 | Tagliare spline aperta con due boundary | Frammenti nativi corretti |  |  |
| TRIM-SPL-03 | Verificare continuità visiva sul punto di taglio | Nessun salto o micro-gap visibile |  |  |
| TRIM-SPL-04 | Salva/riapri dopo trim spline | Rimane spline nativa |  |  |
| TRIM-SPL-05 | Spline chiusa | Deferred/no-op documentato |  |  |

---

## 8. BREAK AT POINT

| ID | Prova | Risultato atteso | Stato | Note |
|---|---|---|---|---|
| BRKPT-01 | Break at point su linea | Due linee con punto condiviso |  |  |
| BRKPT-02 | Break at point su arco | Due archi nativi |  |  |
| BRKPT-03 | Break at point su polyline aperta | Due polilinee, vertice condiviso |  |  |
| BRKPT-04 | Break at point su arco ellittico | Due `EllipticalArcEntity` |  |  |
| BRKPT-05 | Break at point su spline aperta | Due `BezierSplineEntity` |  |  |
| BRKPT-06 | Break at point su cerchio completo | Deferred/no-op documentato |  |  |
| BRKPT-07 | Break at point su ellisse completa | Deferred/no-op documentato |  |  |
| BRKPT-08 | Undo/Redo break at point | Stato ripristinato correttamente |  |  |

---

## 9. BREAK SEGMENT / BREAK BETWEEN POINTS

| ID | Prova | Risultato atteso | Stato | Note |
|---|---|---|---|---|
| BRKSEG-01 | Break segment su linea | Segmento centrale rimosso |  |  |
| BRKSEG-02 | Preview break segment | Tratto da rimuovere tratteggiato |  |  |
| BRKSEG-03 | Break segment su cerchio | Risultato `ArcEntity` nativo |  |  |
| BRKSEG-04 | Break segment su arco | Frammenti `ArcEntity` nativi |  |  |
| BRKSEG-05 | Break segment su polyline aperta | Intervallo rimosso, vertici intermedi preservati |  |  |
| BRKSEG-06 | Break segment su closed polyline/polygon | Polyline aperta risultante |  |  |
| BRKSEG-07 | Break segment su ellisse completa | Risultato `EllipticalArcEntity` |  |  |
| BRKSEG-08 | Break segment su arco ellittico | Frammenti `EllipticalArcEntity` |  |  |
| BRKSEG-09 | Break segment su spline aperta | Frammenti `BezierSplineEntity` |  |  |
| BRKSEG-10 | Curve chiuse: invertire ordine dei due punti | Rimosso l'intervallo opposto |  |  |
| BRKSEG-11 | Due punti quasi coincidenti | Nessun frammento degenerato |  |  |

---

## 10. EXTEND

| ID | Prova | Risultato atteso | Stato | Note |
|---|---|---|---|---|
| EXT-01 | Extend linea verso linea boundary | Endpoint esteso sul punto condiviso |  |  |
| EXT-02 | Extend linea verso circle boundary | Endpoint su cerchio |  |  |
| EXT-03 | Extend linea verso ellipse boundary | Endpoint su ellisse, senza gap |  |  |
| EXT-04 | Extend arco verso linea boundary | Centro/raggio preservati |  |  |
| EXT-05 | Extend arco verso ellipse boundary | Intersezione nativa circle/ellipse corretta |  |  |
| EXT-06 | Extend polyline aperta verso linea | Solo endpoint più vicino modificato |  |  |
| EXT-07 | Extend polyline aperta verso arco ellittico | Endpoint su arco ellittico |  |  |
| EXT-08 | Extend arco ellittico verso linea | Risultato `EllipticalArcEntity` |  |  |
| EXT-09 | Preview extend | Tratto aggiunto evidenziato come Addition |  |  |
| EXT-10 | Target cerchio completo | Messaggio chiaro: curva chiusa non estendibile |  |  |
| EXT-11 | Target ellisse completa | Messaggio chiaro: curva chiusa non estendibile |  |  |
| EXT-12 | Target polyline chiusa/polygon | Messaggio chiaro: curva chiusa non estendibile |  |  |
| EXT-13 | Boundary ellisse selezionabile | Tool accetta `EllipseEntity` come boundary |  |  |
| EXT-14 | Boundary arco ellittico selezionabile | Tool accetta `EllipticalArcEntity` come boundary |  |  |

---

## 11. Shared intersection / micro-gap verification

Questi controlli vanno fatti zoomando molto e usando snap/quote quando possibile.

| ID | Prova | Risultato atteso | Stato | Note |
|---|---|---|---|---|
| GAP-01 | Trim reciproco tra due linee | Endpoint identici, non “quasi” coincidenti |  |  |
| GAP-02 | Trim linea + polyline | Endpoint/vertice condiviso |  |  |
| GAP-03 | Trim linea + cerchio | Line endpoint sul punto di intersezione; arco coerente |  |  |
| GAP-04 | Trim cerchio + ellisse | Endpoint delle due curve sulla stessa intersezione visiva |  |  |
| GAP-05 | Extend linea verso ellisse | Endpoint esattamente sul boundary visivo |  |  |
| GAP-06 | Break segment su curva chiusa | Nessun gap extra oltre al tratto intenzionalmente rimosso |  |  |
| GAP-07 | Salva/riapri e zoomare sulle intersezioni | Nessun peggioramento dopo persistence |  |  |
| GAP-08 | Export DXF e aprire in CAD esterno | Intersezioni ancora coerenti |  |  |

---

## 12. Persistence JSON

| ID | Prova | Risultato atteso | Stato | Note |
|---|---|---|---|---|
| PERS-01 | Salvare disegno con `EllipticalArcEntity` | JSON valido |  |  |
| PERS-02 | Riaprire disegno con `EllipticalArcEntity` | Entità ripristinata correttamente |  |  |
| PERS-03 | Salvare disegno con spline spezzata | Spline rimane nativa |  |  |
| PERS-04 | Riaprire e rieseguire TRIM/BREAK | Comandi ancora funzionanti |  |  |
| PERS-05 | Undo/Redo dopo riapertura | Nessun errore evidente |  |  |

---

## 13. Export SVG / PDF / DXF

| ID | Prova | Risultato atteso | Stato | Note |
|---|---|---|---|---|
| EXP-01 | Export SVG con linee/archi/polilinee | Geometria e layer corretti |  |  |
| EXP-02 | Export SVG con `EllipticalArcEntity` | Path coerente |  |  |
| EXP-03 | Export PDF con `EllipticalArcEntity` | Geometria leggibile |  |  |
| EXP-04 | Export DXF con `EllipticalArcEntity` | ELLIPSE parziale coerente |  |  |
| EXP-05 | Aprire DXF in LibreCAD/QCAD | Archi ellittici visibili e orientati correttamente |  |  |
| EXP-06 | Export dopo trim spline | Spline/frammenti esportati correttamente |  |  |
| EXP-07 | Export non aggiorna `CurrentFilePath` | File corrente nativo invariato |  |  |
| EXP-08 | Export non azzera dirty state | Messaggio UX chiaro |  |  |
| EXP-09 | Export da documento mai salvato | Messaggio suggerisce Save As per progetto editabile |  |  |

---

## 14. Import DXF — controllo rapido

| ID | Prova | Risultato atteso | Stato | Note |
|---|---|---|---|---|
| IMP-01 | Import LINE/CIRCLE/ARC | Entità native corrette |  |  |
| IMP-02 | Import ELLIPSE completa | `EllipseEntity` corretta |  |  |
| IMP-03 | Import ELLIPSE parziale | Se non ancora supportato, segnare Deferred |  |  |
| IMP-04 | Import LWPOLYLINE | Polyline coerente |  |  |
| IMP-05 | Import SPLINE base | Spline coerente o limite documentato |  |  |
| IMP-06 | Import + TRIM/BREAK successivo | Entità importate modificabili |  |  |

---

## 15. Property Panel / Selection

| ID | Prova | Risultato atteso | Stato | Note |
|---|---|---|---|---|
| PROP-01 | Selezionare `EllipticalArcEntity` | Property panel senza errori |  |  |
| PROP-02 | Selezionare spline spezzata | Property panel senza errori |  |  |
| PROP-03 | Cambiare layer a entità generate da trim | Layer aggiornato correttamente |  |  |
| PROP-04 | Lock layer e provare TRIM/BREAK/EXTEND | Entità locked non modificabile |  |  |
| PROP-05 | Hide layer e provare snap/selection | Entità hidden non selezionabile né usata per snap |  |  |

---

## 16. UX / messaggi / preview

| ID | Prova | Risultato atteso | Stato | Note |
|---|---|---|---|---|
| UX-01 | TRIM preview | Parte rimossa tratteggiata |  |  |
| UX-02 | BREAK preview | Segmento rimosso tratteggiato |  |  |
| UX-03 | EXTEND preview | Parte aggiunta evidenziata |  |  |
| UX-04 | Messaggio TRIM | Cita chiaramente la parte tratteggiata da rimuovere |  |  |
| UX-05 | Messaggio BREAK Segment | Cita il segmento tratteggiato da rimuovere |  |  |
| UX-06 | Messaggio EXTEND | Cita la parte evidenziata da aggiungere |  |  |
| UX-07 | Curve chiuse in BREAK Segment | Messaggio o comportamento chiarisce l’ordine dei punti |  |  |
| UX-08 | Target non supportato in EXTEND | Messaggio chiaro sulle curve chiuse |  |  |
| UX-09 | ESC durante preview | Preview cancellata |  |  |

---

## 17. Performance / robustness smoke test

| ID | Prova | Risultato atteso | Stato | Note |
|---|---|---|---|---|
| PERF-01 | Disegno con molte linee e polilinee | Selezione e TRIM reattivi |  |  |
| PERF-02 | Disegno con molte ellissi/archi ellittici | Preview non eccessivamente lenta |  |  |
| PERF-03 | Multi-boundary TRIM con molte boundary | Nessun blocco evidente |  |  |
| PERF-04 | Zoom/pan durante sessione lunga | Nessuna degradazione evidente |  |  |
| PERF-05 | Ripetere Undo/Redo molte volte | Nessun errore o stato sporco incoerente |  |  |

---

## 18. Risultato finale regression

| Area | Esito | Note |
|---|---|---|
| TRIM |  |  |
| BREAK AT POINT |  |  |
| BREAK SEGMENT |  |  |
| EXTEND |  |  |
| EllipticalArcEntity |  |  |
| BezierSplineEntity aperta |  |  |
| Shared intersections / micro-gap |  |  |
| Persistence |  |  |
| Export |  |  |
| Import DXF |  |  |
| UX preview / messaggi |  |  |
| Performance smoke test |  |  |

## Bug trovati

| ID | Descrizione | Gravità | File/caso di prova | Stato |
|---|---|---|---|---|
| BUG-01 |  |  |  |  |
| BUG-02 |  |  |  |  |
| BUG-03 |  |  |  |  |

## Deferred confermati

| ID | Tema | Motivazione | Fase futura |
|---|---|---|---|
| DEF-01 | BreakAtPoint su cerchio completo | Arco quasi 360° richiede policy dedicata | v0.9/v1.0 |
| DEF-02 | BreakAtPoint su ellisse completa | Arco ellittico quasi 360° richiede policy dedicata | v0.9/v1.0 |
| DEF-03 | Closed Bezier spline editing | Richiede policy e split su curva chiusa | v1.0 |
| DEF-04 | Offset ellipse/spline | Offset non è nativamente ellisse/Bezier esatta | v0.9/v1.0 |
| DEF-05 | DXF import partial ELLIPSE | Mapping verso `EllipticalArcEntity` da completare/verificare | v0.9 |

## Decisione finale

| Campo | Valore |
|---|---|
| Regression eseguita da |  |
| Data |  |
| Build / commit |  |
| Esito complessivo |  |
| Pronto per fase successiva | Sì / No |


## Preview/status invariants

- Selected TRIM cutting edges, EXTEND boundaries and BREAK targets stay visible as emphasis overlays.
- TRIM previews show the selected cutting edge, hovered target and hot picked-side marker when the removal preview is valid.
- EXTEND previews show the selected boundary, hovered target and hot picked-endpoint marker when the addition preview is valid.
- BREAK point previews show projected point markers.
- Removal previews use removal styling; addition previews use addition styling.
- No-op results must show a clear status message and leave the document unchanged.
