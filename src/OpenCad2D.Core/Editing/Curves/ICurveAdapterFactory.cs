using OpenCad2D.Core.Entities;

namespace OpenCad2D.Core.Editing.Curves;

public interface ICurveAdapterFactory
{
    bool TryCreate(
        CadEntity entity,
        out ICurveAdapter adapter);
}
