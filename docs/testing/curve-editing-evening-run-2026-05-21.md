# Curve Editing Regression — Evening Run 2026-05-21

Questo file è il foglio di lavoro operativo per la prima passata manuale sulla regression **Curve Editing v0.9**.

Checklist completa di riferimento: `docs/testing/curve-editing-regression-v0.9.md`.

Disegno di partenza consigliato: `docs/testing/samples/curve-editing-regression-v0.9.opencad2d.json`.

## Regola della sessione

Non correggere codice durante questa passata. L'obiettivo è trovare bug riproducibili, annotare i passi minimi e decidere quali casi bloccare prima della v0.9.

Se emerge un bug **Blocker** o **High**, fermare la prova del gruppo corrente, compilare la scheda bug e ripartire da una copia pulita del file di prova.

## Preparazione

| ID | Azione | Stato | Note |
|---|---|---|---|
| RUN-PREP-01 | Aprire il file sample indicato sopra |  |  |
| RUN-PREP-02 | Salvarne una copia locale con nome `curve-editing-regression-v0.9-local.opencad2d.json` |  |  |
| RUN-PREP-03 | Verificare che i layer `Regression geometry`, `Regression boundary`, `Regression locked`, `Regression hidden` siano presenti |  |  |
| RUN-PREP-04 | Verificare che lo zoom mostri tutti i gruppi di geometrie |  |  |
| RUN-PREP-05 | Provare un Undo/Redo semplice prima dei comandi distruttivi |  |  |


## Passata 0 — Messaggistica di stato

Prima dei test distruttivi, verificare che i comandi segnalino chiaramente i casi non eseguibili. Questa passata serve a distinguere un vero bug geometrico da un caso correttamente rifiutato.

| ID | Comando | Caso | Risultato atteso | Stato | Note |
|---|---|---|---|---|---|
| MSG-TRIM-01 | TRIM | Target senza intersezione con cutting edge | Messaggio "No trim intersection..." o equivalente; nessuna modifica |  |  |
| MSG-TRIM-02 | TRIM | Pick sul lato non rimovibile | Messaggio che spiega che il lato scelto non produce un intervallo rimovibile |  |  |
| MSG-BRKPT-01 | BREAK AT POINT | Pick fuori entità | Messaggio che dice che il punto non è sull'entità |  |  |
| MSG-BRKPT-02 | BREAK AT POINT | Pick troppo vicino a endpoint/vertice | Messaggio che invita a scegliere un punto interno lontano da endpoint/vertici |  |  |
| MSG-BRKSEG-01 | BREAK SEGMENT | Due punti troppo vicini | Messaggio che richiede due punti distinti |  |  |
| MSG-BRKSEG-02 | BREAK SEGMENT | Secondo punto fuori entità | Messaggio che dice che il secondo punto non è sull'entità |  |  |
| MSG-EXT-01 | EXTEND | Boundary non raggiungibile dalla proiezione | Messaggio "No extension possible..." o equivalente; nessuna modifica |  |  |
| MSG-EXT-02 | EXTEND | Boundary raggiungibile solo dall'altro endpoint | Messaggio che suggerisce di scegliere il lato/opposto endpoint |  |  |

Se un comando non modifica nulla ma mostra un messaggio chiaro e coerente, segnare `OK`. Se non modifica nulla e non spiega perché, segnare `Bug` anche se la geometria resta integra.

## Passata 0.5 — Preview visiva comune

Questa passata verifica che i comandi mostrino il contesto visuale corretto prima di modificare il disegno.

| ID | Comando | Caso | Risultato atteso | Stato | Note |
|---|---|---|---|---|---|
| PREVIEW-BRKPT-01 | BREAK AT POINT | Target selezionato, prima del punto | Target evidenziato come contesto; nessuna geometria permanente |  |  |
| PREVIEW-BRKPT-02 | BREAK AT POINT | Hover su punto valido | Due segmenti di anteprima + marker sul punto proiettato |  |  |
| PREVIEW-BRKSEG-01 | BREAK SEGMENT | Primo punto scelto | Target evidenziato + marker sul primo punto |  |  |
| PREVIEW-BRKSEG-02 | BREAK SEGMENT | Hover secondo punto valido | Intervallo rimosso tratteggiato + marker sui due punti |  |  |
| PREVIEW-TRIM-01 | TRIM | Boundary selezionata | Boundary ancora evidenziata durante la scelta del target |  |  |
| PREVIEW-EXT-01 | EXTEND | Boundary selezionata | Boundary ancora evidenziata durante la scelta del target |  |  |

## Passata 1 — TRIM base

| ID checklist | Caso | Stato | Bug ID / Note |
|---|---|---|---|
| TRIM-GEN-01 | Avvio TRIM e messaggio iniziale |  |  |
| TRIM-GEN-03 | Boundary + target con preview |  |  |
| TRIM-GEN-04 | Preview parte rimossa tratteggiata |  |  |
| TRIM-GEN-05 | TRIM + Undo |  |  |
| TRIM-GEN-06 | Redo dopo Undo |  |  |
| TRIM-LINE-01 | Linea tagliata da linea boundary |  |  |
| TRIM-LINE-05 | Trim reciproco due linee, niente micro-gap |  |  |
| TRIM-LINE-06 | Linea tagliata da cerchio |  |  |
| TRIM-CIRC-01 | Cerchio tagliato da linea, risultato ArcEntity |  |  |
| TRIM-CIRC-04 | Arco tagliato da linea, risultato ArcEntity |  |  |

## Passata 2 — Polilinee e curve avanzate

| ID checklist | Caso | Stato | Bug ID / Note |
|---|---|---|---|
| TRIM-PL-01 | Polyline aperta tagliata da linea |  |  |
| TRIM-PL-03 | Polyline chiusa/polygon tagliata |  |  |
| TRIM-ELL-01 | Ellisse completa tagliata da linea, risultato EllipticalArcEntity |  |  |
| TRIM-ELL-05 | Arco ellittico tagliato da linea |  |  |
| TRIM-SPL-01 | Spline aperta tagliata da linea, risultato BezierSplineEntity |  |  |
| TRIM-SPL-03 | Continuità visiva sul punto di taglio |  |  |

## Passata 3 — BREAK

| ID checklist | Caso | Stato | Bug ID / Note |
|---|---|---|---|
| BRKPT-01 | Break at point su linea |  |  |
| BRKPT-02 | Break at point su arco |  |  |
| BRKPT-03 | Break at point su polyline aperta |  |  |
| BRKPT-04 | Break at point su arco ellittico |  |  |
| BRKPT-05 | Break at point su spline aperta |  |  |
| BRKSEG-01 | Break segment su linea |  |  |
| BRKSEG-03 | Break segment su cerchio |  |  |
| BRKSEG-07 | Break segment su ellisse completa |  |  |
| BRKSEG-09 | Break segment su spline aperta |  |  |

## Passata 4 — EXTEND e micro-gap

| ID checklist | Caso | Stato | Bug ID / Note |
|---|---|---|---|
| EXT-01 | Extend linea verso linea boundary |  |  |
| EXT-02 | Extend linea verso circle boundary |  |  |
| EXT-03 | Extend linea verso ellipse boundary |  |  |
| EXT-08 | Extend arco ellittico verso linea |  |  |
| EXT-10 | Target cerchio completo: messaggio chiaro |  |  |
| GAP-01 | Trim reciproco tra due linee |  |  |
| GAP-03 | Trim linea + cerchio |  |  |
| GAP-04 | Trim cerchio + ellisse |  |  |
| GAP-07 | Salva/riapri e zoom sulle intersezioni |  |  |

## Bug trovati questa sera

| ID | Comando | Entità | Gravità | Passi minimi | Risultato atteso | Risultato ottenuto | Stato |
|---|---|---|---|---|---|---|---|
| BUG-CURVE-01 |  |  |  |  |  |  |  |
| BUG-CURVE-02 |  |  |  |  |  |  |  |
| BUG-CURVE-03 |  |  |  |  |  |  |  |

## Esito serata

| Campo | Valore |
|---|---|
| Build / commit |  |
| File di prova usato |  |
| Area più stabile |  |
| Area più fragile |  |
| Bug Blocker/High presenti | Sì / No |
| Possiamo passare alla prossima fase? | Sì / No |

## Note libere

- 
