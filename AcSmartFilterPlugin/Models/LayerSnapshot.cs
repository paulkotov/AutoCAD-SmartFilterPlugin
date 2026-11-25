using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace AcSmartFilterPlugin.Models
{
    /// <summary>
    /// Состояние слоёв чертежа на момент открытия окна: список слоёв
    /// и текущий слой. Используется для восстановления после фильтрации.
    /// </summary>
    public sealed class LayerSnapshot
    {
        public IReadOnlyList<LayerInfo> Layers { get; }

        public ObjectId CurrentLayerId { get; }

        public LayerSnapshot(IReadOnlyList<LayerInfo> layers, ObjectId currentLayerId)
        {
            Layers = layers;
            CurrentLayerId = currentLayerId;
        }

    }
}
