using System.Collections.Generic;
using System.Linq;

namespace AcSmartFilterPlugin.Models
{
    /// <summary>
    /// Конфигурация фильтра, сохраняемая внутри DWG: имена выбранных слоёв
    /// и признак режима «фильтровать по текущему слою». Слои хранятся по имени,
    /// а не по <c>ObjectId</c>, так как идентификаторы не переживают перезагрузку чертежа.
    /// </summary>
    public sealed class FilterConfig
    {
        public FilterConfig(IEnumerable<string> layerNames, bool useCurrentLayer)
        {
            LayerNames = (layerNames ?? Enumerable.Empty<string>()).ToList();
            UseCurrentLayer = useCurrentLayer;
        }

        /// <summary>Имена слоёв, выбранных для фильтрации.</summary>
        public IReadOnlyList<string> LayerNames { get; }

        /// <summary>Признак режима «фильтровать по текущему слою».</summary>
        public bool UseCurrentLayer { get; }
    }
}
