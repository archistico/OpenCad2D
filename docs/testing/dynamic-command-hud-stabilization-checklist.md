# Dynamic Command HUD Stabilization Checklist

This checklist protects the stable dynamic command HUD baseline before new tools are connected to editable HUD routing.

## Stable scope

The current stable editable HUD scope is limited to:

- Line
- Polyline straight-segment mode
- Rectangle opposite-corner mode through the dedicated rectangle resolver
- first-point absolute coordinates through `X` / `Y`
- polar point input through `Distance` / `Angle` for Line/Polyline
- rectangle size input through `Width` / `Height` for Rectangle

Circle, Arc and Modify tools must not be considered fully editable until they receive dedicated incremental steps and tests.

## Manual checks

### Line distance only

```text
LINE
click first point
move mouse to the intended direction
200
Enter
```

Expected result: a line of length 200 in the direction that was visible when the value was entered.

### Line distance and angle

```text
LINE
click first point
200
Tab
45
Enter
```

Expected result: a line of length 200 at 45 degrees.

### Polyline first point from coordinates

```text
POLYLINE
Tab
0
Tab
0
Enter
```

Expected result: first vertex at absolute UCS coordinates X=0, Y=0.

### Polyline two polar segments

```text
POLYLINE
click first point
200
Tab
45
Enter
200
Tab
0
Enter
C
```

Expected result: a closed polyline with the first two inserted segments matching the requested polar inputs. The second segment must not inherit a stale angle or distance unless explicitly typed.

### Rectangle width and height

```text
RECTANGLE
click first corner
200
Tab
100
Enter
```

Expected result: a closed rectangle 200 units wide and 100 units high in the live quadrant shown by the cursor.

### Rectangle first corner from coordinates

```text
RECTANGLE
Tab
50
Tab
25
Enter
200
Tab
100
Enter
```

Expected result: first corner at absolute UCS coordinates X=50, Y=25, then a 200 by 100 rectangle.

### Mouse transparency

Move the mouse quickly through the HUD while drawing.

Expected result: the HUD must never block canvas point picking.

## Regression rules

Before extending another tool, verify:

- `Tab` does not focus the Property Panel.
- `Distance -> Tab -> Angle` remains reliable.
- `X/Y` first-point entry still requires intentional `Tab` navigation.
- HUD overrides clear after point confirmation.
- Plain numeric input during first-point prompts does not jump into `X`.
- Rectangle `Width` / `Height` uses only the dedicated rectangle resolver and must not change Line/Polyline routing.

## Automated regression coverage

The App test suite now covers the most fragile HUD behaviors so they do not need to be checked manually after every small change:

- `CommandHudInput_LineDistance_ShouldFreezeVisibleLiveAngle`
- `CommandHudInput_LineFirstPoint_ShouldAcceptAbsoluteXAndY`
- `CommandHudInput_LineDistanceAngle_ShouldCreateExpectedSegment`
- `CommandHudInput_PolylineFirstPoint_ShouldAcceptAbsoluteXAndY`
- `CommandHudInput_PolylineDistanceAngle_ShouldCreateNextVertexAndResetOverride`
- `CommandHudInput_PolylineDistanceOnly_ShouldFreezeVisibleLiveAngle`
- `CommandHudInput_PolylineMultipleSegments_ShouldNotReusePreviousOverrides`
- `CommandHudInput_PolylineFirstPointIncompleteCoordinates_ShouldNotCreatePoint`
- `CommandHudInput_RectangleWidthHeight_ShouldCreateRectangle`
- `CommandHudInput_RectangleFirstCorner_ShouldAcceptAbsoluteXAndY`
- `CommandHudInput_RectangleWidthOnly_ShouldFreezeVisibleLiveHeight`
- `CommandHudInput_RectangleWidthHeight_ShouldRespectLiveQuadrant`
- `CommandHudInput_RectangleFirstCornerIncompleteCoordinates_ShouldNotCreateRectangle`

Manual checks should still be used for mouse transparency and visual focus/highlight behavior, because those depend on Avalonia event routing and rendering rather than ViewModel-only logic.
