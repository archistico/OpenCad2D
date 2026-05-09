using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Spatial;

/// <summary>
/// Provides spatial lookup for CAD entities.
/// </summary>
public interface ISpatialIndex
{
    void Add(CadEntity entity);

    bool Remove(EntityId id);

    void Replace(CadEntity entity);

    void Clear();

    IReadOnlyList<CadEntity> Query(BoundingBox2D area);
}