namespace OpenCad2D.Export.Dxf.Import;

/// <summary>
/// Splits DXF group-code pairs into logical sections.
/// </summary>
public sealed class DxfSectionReader
{
    /// <summary>
    /// Returns the sections found in the supplied group-code pairs.
    /// Unknown top-level records are ignored so malformed-but-readable files can still be inspected later.
    /// </summary>
    public IReadOnlyList<DxfSection> ReadSections(IReadOnlyList<DxfCodePair> pairs)
    {
        ArgumentNullException.ThrowIfNull(pairs);

        var sections = new List<DxfSection>();
        int index = 0;

        while (index < pairs.Count)
        {
            DxfCodePair current = pairs[index];

            if (!current.IsMarkerValue("SECTION"))
            {
                index++;
                continue;
            }

            if (index + 1 >= pairs.Count || pairs[index + 1].Code != 2)
            {
                throw new DxfReadException(
                    $"DXF SECTION at line {current.CodeLineNumber} is missing its name group code 2.");
            }

            string sectionName = pairs[index + 1].Value;
            int sectionStartLine = current.CodeLineNumber;
            index += 2;

            var sectionPairs = new List<DxfCodePair>();
            bool foundEndSection = false;

            while (index < pairs.Count)
            {
                DxfCodePair pair = pairs[index];

                if (pair.IsMarkerValue("ENDSEC"))
                {
                    foundEndSection = true;
                    index++;
                    break;
                }

                sectionPairs.Add(pair);
                index++;
            }

            if (!foundEndSection)
            {
                throw new DxfReadException(
                    $"DXF section '{sectionName}' starting at line {sectionStartLine} is missing ENDSEC.");
            }

            sections.Add(new DxfSection(
                sectionName,
                sectionPairs,
                sectionStartLine));
        }

        return sections;
    }
}
