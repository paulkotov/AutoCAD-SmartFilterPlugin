using System;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using AcSmartFilterPlugin.Models;

namespace AcSmartFilterPlugin.Services
{
    /// <summary>Чтение типа и цвета примитива для фильтра.</summary>
    public static class EntityProperties
    {
        /// <summary>DXF-имя объекта; для типов без него — имя .NET-класса.</summary>
        public static string GetObjectType(Entity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            var dxfName = entity.GetRXClass()?.DxfName;
            return string.IsNullOrEmpty(dxfName) ? entity.GetType().Name : dxfName;
        }

        /// <summary>Назначение цвета объекта, без вычисления эффективного цвета слоя.</summary>
        public static ObjectColor GetColor(Entity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            var color = entity.Color;
            if (color == null)
            {
                return ObjectColor.ByLayer;
            }

            switch (color.ColorMethod)
            {
                case ColorMethod.ByLayer:
                    return ObjectColor.ByLayer;
                case ColorMethod.ByBlock:
                    return ObjectColor.ByBlock;
                case ColorMethod.ByAci:
                    return ObjectColor.FromIndex(color.ColorIndex);
                case ColorMethod.ByColor:
                    return ObjectColor.FromRgb(color.Red, color.Green, color.Blue);
                default:
                    return ObjectColor.Other(color.ColorIndex, color.Red, color.Green, color.Blue);
            }
        }
    }
}
