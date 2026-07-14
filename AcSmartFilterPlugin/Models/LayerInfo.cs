using Autodesk.AutoCAD.DatabaseServices;

namespace AcSmartFilterPlugin.Models
{
    /// <summary>
    /// Снимок слоя чертежа: идентификатор, имя и видимость на момент чтения.
    /// </summary>
    public sealed class LayerInfo
    {
        public LayerInfo(ObjectId id, string name, bool isOff, bool isFrozen)
        {
            Id = id;
            Name = name;
            IsOff = isOff;
            IsFrozen = isFrozen;
        }

        public ObjectId Id { get; }

        public string Name { get; }

        public bool IsOff { get; }

        public bool IsFrozen { get; }
    }
}
