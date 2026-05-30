# Dynamic Command HUD Stabilization Checklist

This checklist protects the stable dynamic command HUD baseline before new tools are connected to editable HUD routing.

## Stable scope

The current stable editable HUD scope is limited to:

- Line
- Polyline straight-segment mode
- Rectangle opposite-corner mode through the dedicated rectangle resolver
- Circle radius mode through the dedicated circle resolver
- first-point absolute coordinates through `X` / `Y`
- polar point input through `Distance` / `Angle` for Line/Polyline
- rectangle size input through `Width` / `Height` for Rectangle
- circle radius input through `Radius` for Circle

Arc and Modify tools must not be considered fully editable until they receive dedicated incremental steps and tests.

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
- `CommandHudInput_CircleRadius_ShouldCreateCircle`
- `CommandHudInput_CircleCenter_ShouldAcceptAbsoluteXAndY`
- `CommandHudInput_CircleCenterPrompt_ShouldExposeOnlyCoordinateFields`
- `CommandHudInput_CircleRadiusZero_ShouldNotCreateCircle`
- `CommandHudInput_CircleRadiusNegative_ShouldNotCreateCircle`
- `CommandHudInput_CircleCenterIncompleteCoordinates_ShouldNotCreateCircle`

Manual checks should still be used for mouse transparency, visual focus/highlight behavior, and final HUD row alignment, because those depend on Avalonia event routing and rendering rather than ViewModel-only logic.

## Circle HUD checks

- `CIRCLE`, click center, type `50`, `Enter`: a circle with radius 50 is created.
- `CIRCLE`, `Tab`, type X, `Tab`, type Y, `Enter`, type radius, `Enter`: center coordinates and radius are respected.
- During circle radius input, the HUD must show `Radius`, `X`, `Y`; the mouse must remain free for canvas point selection.
- Incomplete center coordinates (`X` without `Y`) must not create a circle and should show the existing both-coordinates validation message.

## Rectangle by Sides HUD checks

Manual checks:

```text
RECTSIDES
click first corner
10
Tab
0
Enter
5
Enter
```

Expected: closed rectangle by sides with first side length 10 and second side height 5.

```text
RECTSIDES
Tab
2
Tab
3
Enter
10
Enter
5
Enter
```

Expected: first corner inserted at X=2, Y=3, then rectangle by sides is created from typed width and height.

Automated regression tests:

- `CommandHudInput_RectangleBySidesFirstSide_ShouldExposeEditableWidthAndAngle`
- `CommandHudInput_RectangleBySidesSecondSide_ShouldExposeEditableHeight`
- `CommandHudInput_RectangleBySidesWidthAngleHeight_ShouldCreateRectangle`
- `CommandHudInput_RectangleBySidesFirstCorner_ShouldAcceptAbsoluteXAndY`
- `CommandHudInput_RectangleBySidesHeightNegative_ShouldNotCreateRectangle`
- `CommandHudInput_ArcStart_ShouldExposeEditableRadiusAndAngle`
- `CommandHudInput_ArcEnd_ShouldExposeEditableAngle`
- `CommandHudInput_ArcRadiusAngleEndAngle_ShouldCreateArc`
- `CommandHudInput_ArcRadiusNegative_ShouldNotCreateArc`
- `CommandHudInput_EllipseMajorAxis_ShouldExposeEditableRadiusAndAngle`
- `CommandHudInput_EllipseMinorRadius_ShouldExposeEditableRadius`
- `CommandHudInput_EllipseMajorRadiusAngleMinorRadius_ShouldCreateEllipse`
- `CommandHudInput_EllipseMinorRadiusNegative_ShouldNotCreateEllipse`


## Step 28B - Rectangle by Sides height routing fix

Fixed the logical HUD initial numeric routing so a single available `Height` field is treated as a preferred numeric target. This keeps the second side phase keyboard-driven: after setting the first side, typing a number routes to `Height` instead of the hidden generic command buffer.

## Arc HUD checks

Manual checks:

```text
ARC
click center
10
Tab
0
Enter
90
Enter
```

Expected: an arc centered on the clicked point, radius 10, start angle 0 degrees and end angle 90 degrees.

```text
ARC
Tab
0
Tab
0
Enter
10
Enter
90
Enter
```

Expected: center inserted at X=0, Y=0, then the arc is created from typed radius and end angle.

Step 29A note: `Angle` is now a preferred initial numeric HUD target when it is the available editable field, so the Arc end phase stays keyboard-driven.

## Ellipse HUD checks

Manual checks:

```text
ELLIPSE
click center
10
Tab
0
Enter
4
Enter
```

Expected: an ellipse centered on the clicked point, major radius 10 on the 0 degree axis and minor radius 4.

```text
ELLIPSE
Tab
0
Tab
0
Enter
10
Enter
4
Enter
```

Expected: center inserted at X=0, Y=0, then the ellipse is created from typed major and minor radii.

Step 29B note: Ellipse uses dedicated major-axis and minor-radius resolvers. Do not route these fields through the generic Line/Polyline polar resolver.
