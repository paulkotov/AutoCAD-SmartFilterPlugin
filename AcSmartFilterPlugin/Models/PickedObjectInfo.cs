using System;
using Autodesk.AutoCAD.DatabaseServices;

namespace AcSmartFilterPlugin.Models
{
    /// <summary>
    /// Сведения об указанном в чертеже объекте: идентификация, тип, цвет и слой.
    /// </summary>
    public sealed class PickedObjectInfo
    {
        public PickedObjectInfo(
            ObjectId id,
            Handle handle,
            string objectType,
            ObjectColor color,
            ObjectId layerId,
            string layerName)
        {
            Id = id;
            Handle = handle;
            ObjectType = objectType;
            Color = color ?? throw new ArgumentNullException(nameof(color));
            LayerId = layerId;
            LayerName = layerName;
        }

        public ObjectId Id { get; }

        /// <summary>Постоянный идентификатор объекта в чертеже.</summary>
        public Handle Handle { get; }

        /// <summary>Тип объекта: DXF-имя (например, LWPOLYLINE) или имя .NET-класса.</summary>
        public string ObjectType { get; }

        /// <summary>Назначение цвета указанного объекта.</summary>
        public ObjectColor Color { get; }

        public ObjectId LayerId { get; }

        public string LayerName { get; }
    }
}
