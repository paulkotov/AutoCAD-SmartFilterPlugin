using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;

namespace AcSmartFilterPlugin.Models
{
    /// <summary>
    /// Объекты, скрытые фильтром по типу и цвету. Нужен, чтобы вернуть им видимость.
    /// </summary>
    public sealed class EntityVisibilitySnapshot
    {
        public static EntityVisibilitySnapshot Empty { get; } =
            new EntityVisibilitySnapshot(Array.Empty<ObjectId>());

        public EntityVisibilitySnapshot(IEnumerable<ObjectId> hiddenEntityIds)
        {
            HiddenEntityIds = (hiddenEntityIds ?? Enumerable.Empty<ObjectId>()).ToArray();
        }

        public IReadOnlyList<ObjectId> HiddenEntityIds { get; }
    }
}
