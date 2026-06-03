# Text Formats

Text formats define the reusable appearance of text annotations. They are used by single-line text, multiline text and, indirectly, by dimension styles when dimension text is linked to a text format.

OpenCad2D separates the text content from its appearance. The text entity stores the written value and its placement in the drawing. The text format defines how that value is displayed: font family, height, color and style options such as bold or italic when supported.

This separation keeps drawings easier to maintain. If many labels use the same text format, changing the format can make the annotation style consistent without editing every text entity individually.

## Text and MText

`Text` is used for short, single-line annotations. It is suitable for labels, room names, simple notes and compact technical text.

`MText` is used for multiline notes. It is better for longer annotations, descriptions, legends or blocks of text where line breaks matter. Multiline text stores the same kind of placement information as normal text, but it can contain multiple lines and may include a reference width used by import/export formats such as DXF MTEXT.

Use Text when the note is short and should behave like a single label. Use MText when the note is paragraph-like or when you need explicit line breaks.

## Common text properties

A text format typically controls the font family, text height, color and style. The entity itself controls insertion point, rotation, layer and the actual text value. This distinction is important: if a label is in the wrong position, edit the entity; if many labels have the wrong size, edit or change the text format.

Text height is expressed in drawing units. It should be chosen according to the drawing scale and the intended output. A text height that looks good at one zoom level may be too large or too small when printed or exported, so the final check should always consider the output format.

## Relationship with dimensions

Dimension text is controlled by dimension styles. A dimension style can reference a text format so that dimension labels remain visually consistent with the rest of the drawing.

For example, a drawing may use one text format for general room labels and another text format for dimensions. Dimension styles then decide which text format to use, together with dimension-specific settings such as prefixes, suffixes, decimal precision and text offset.

## Practical convention

Keep the number of text formats limited. A typical drawing does not need many of them. A practical starting point is one format for general annotations, one for dimensions if needed, and one larger format for titles or labels that must stand out.

Clear names are more useful than decorative names. Prefer names such as `Annotation 2.5`, `Dimension 2.5` or `Title 5.0`, because they explain the intended use directly.
