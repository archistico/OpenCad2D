using OpenCad2D.Core.Documents;
using OpenCad2D.Persistence.Dto;

namespace OpenCad2D.Persistence;

/// <summary>
/// Serializes and deserializes OpenCad2D documents.
/// </summary>
public interface IDocumentSerializer
{
    DocumentDto Serialize(
        CadDocument document,
        string currentLayerId,
        ViewportStateDto viewport);

    CadDocument Deserialize(
        DocumentDto dto,
        out string currentLayerId,
        out ViewportStateDto viewport);

    DocumentRecoveryResult DeserializeWithRecovery(DocumentDto dto);

    void SaveToFile(
        DocumentDto dto,
        string filePath);

    DocumentDto LoadFromFile(string filePath);
}
