# Dimension Styles

Dimension styles control how dimensions are drawn. They define the appearance and formatting of dimension entities, including text, symbols, offsets, prefixes, suffixes and fit behavior.

Dimensions in OpenCad2D are graphical annotations. They store their own measured points and dimension-line geometry. At the current stage they are not fully associative: if the original geometry changes later, existing dimensions may need to be checked, updated or recreated.

The purpose of dimension styles is consistency. Instead of configuring each dimension separately, you define a style and use it for all dimensions that follow the same drawing convention.

## What a dimension style controls

A dimension style can control the linked text format, arrow or terminator symbol, arrow size, text offset from the dimension line, preferred distance from measured points, extension line offset and overshoot, decimal precision, decimal separator, prefixes, suffixes and text orientation.

For radius and diameter dimensions, the style can also define dedicated prefixes. This makes it possible to show radius and diameter annotations with conventional labels without manually editing every dimension text.

The Dimension Style Manager includes a preview. The preview is important because dimension settings interact with each other: changing arrow size, text offset or extension spacing can make a style clearer or more crowded. The preview gives immediate visual feedback before the style is applied in a drawing.

## Built-in styles

OpenCad2D provides practical starting styles. `Standard` is the generic default style. `Architectural` is intended for architectural drawings, typically using architectural tick symbols and meter-based suffixes. `Mechanical` is intended for more mechanical-style annotations, typically using filled triangle terminators, millimeter suffixes and horizontal text orientation.

These styles should be treated as starting points. They can be duplicated or adjusted according to the drawing convention you want to use.

## Text orientation and readability

Dimension text can follow different orientation rules. A readable mode keeps text from appearing upside down. For vertical dimensions, OpenCad2D follows the current project convention where text is readable from the left side of the sheet. A horizontal mode keeps dimension text horizontal regardless of the dimension direction. An aligned mode follows the dimension line more strictly.

The best choice depends on the drawing standard you want to follow. Architectural drawings often benefit from readable aligned text, while some mechanical conventions prefer horizontal dimension text.

## Fit behavior

Short dimensions can become crowded when the measured span is too small for both text and terminators. Dimension styles include fit rules to decide whether text and terminators should remain inside the measured span or move outside when needed.

This is not just a visual detail. Good fit rules keep dense drawings readable and reduce manual cleanup after placing dimensions in tight areas.

## Applying styles

New dimensions use the current dimension style. Existing dimensions can be updated from the property panel when a dimension entity is selected. Changing the style of a selected dimension should be treated like any other document edit: it must be undoable and should preserve the measured geometry while changing only the appearance controlled by the style.

A good workflow is to define or choose the dimension style before adding many dimensions. This reduces later cleanup and keeps the drawing consistent from the beginning.
