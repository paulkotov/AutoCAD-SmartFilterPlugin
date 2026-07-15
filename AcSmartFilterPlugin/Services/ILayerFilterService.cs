using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using AcSmartFilterPlugin.Models;

namespace AcSmartFilterPlugin.Services
{
    /// <summary>
    /// Инкапсулирует работу с базой данных AutoCAD: чтение слоёв,
    /// применение фильтра по видимости и восстановление исходного состояния.
    /// </summary>
    public interface ILayerFilterService
    {
        /// <summary>Есть ли активный документ, с которым можно работать.</summary>
        bool HasActiveDocument { get; }

        /// <summary>Текущий слой активного чертежа (или <see cref="ObjectId.Null"/>).</summary>
        ObjectId GetCurrentLayerId();

        /// <summary>Читает слои активного чертежа и фиксирует их состояние.</summary>
        LayerSnapshot LoadLayers();

        /// <summary>Оставляет включёнными только выбранные слои, остальные выключает.</summary>
        void ApplyFilter(IReadOnlyCollection<ObjectId> selectedLayerIds);

        /// <summary>Восстанавливает состояние слоёв из снимка.</summary>
        void RestoreState(LayerSnapshot snapshot);
    }
}
