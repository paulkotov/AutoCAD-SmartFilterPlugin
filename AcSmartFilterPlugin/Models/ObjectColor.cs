using System;

namespace AcSmartFilterPlugin.Models
{
    /// <summary>
    /// Назначение цвета объекта: «По слою», «По блоку», индекс ACI или истинный цвет.
    /// Сравнивается само назначение, без подстановки цвета слоя или блока.
    /// </summary>
    public sealed class ObjectColor : IEquatable<ObjectColor>
    {
        private ObjectColor(ObjectColorSource source, short colorIndex, byte red, byte green, byte blue)
        {
            Source = source;
            ColorIndex = colorIndex;
            Red = red;
            Green = green;
            Blue = blue;
        }

        public static ObjectColor ByLayer { get; } = new ObjectColor(ObjectColorSource.ByLayer, 256, 0, 0, 0);

        public static ObjectColor ByBlock { get; } = new ObjectColor(ObjectColorSource.ByBlock, 0, 0, 0, 0);

        public ObjectColorSource Source { get; }

        public short ColorIndex { get; }

        public byte Red { get; }

        public byte Green { get; }

        public byte Blue { get; }

        public string DisplayName
        {
            get
            {
                switch (Source)
                {
                    case ObjectColorSource.ByLayer:
                        return "По слою";
                    case ObjectColorSource.ByBlock:
                        return "По блоку";
                    case ObjectColorSource.ColorIndex:
                        return FormatColorIndex(ColorIndex);
                    case ObjectColorSource.TrueColor:
                        return $"RGB {Red}, {Green}, {Blue}";
                    default:
                        return "Специальный";
                }
            }
        }

        public static ObjectColor FromIndex(short colorIndex)
        {
            return new ObjectColor(ObjectColorSource.ColorIndex, colorIndex, 0, 0, 0);
        }

        public static ObjectColor FromRgb(byte red, byte green, byte blue)
        {
            return new ObjectColor(ObjectColorSource.TrueColor, 0, red, green, blue);
        }

        public static ObjectColor Other(short colorIndex, byte red, byte green, byte blue)
        {
            return new ObjectColor(ObjectColorSource.Other, colorIndex, red, green, blue);
        }

        public bool Equals(ObjectColor other)
        {
            if (other == null || other.Source != Source)
            {
                return false;
            }

            switch (Source)
            {
                case ObjectColorSource.ColorIndex:
                    return ColorIndex == other.ColorIndex;
                case ObjectColorSource.TrueColor:
                    return Red == other.Red && Green == other.Green && Blue == other.Blue;
                case ObjectColorSource.Other:
                    return ColorIndex == other.ColorIndex
                        && Red == other.Red
                        && Green == other.Green
                        && Blue == other.Blue;
                default:
                    return true;
            }
        }

        public override bool Equals(object obj) => Equals(obj as ObjectColor);

        public override int GetHashCode()
        {
            unchecked
            {
                switch (Source)
                {
                    case ObjectColorSource.ColorIndex:
                        return ((int)Source * 397) ^ ColorIndex;
                    case ObjectColorSource.TrueColor:
                        return ((int)Source * 397) ^ Red ^ (Green << 8) ^ (Blue << 16);
                    case ObjectColorSource.Other:
                        return ((int)Source * 397) ^ ColorIndex ^ Red ^ (Green << 8) ^ (Blue << 16);
                    default:
                        return (int)Source;
                }
            }
        }

        private static string FormatColorIndex(short colorIndex)
        {
            switch (colorIndex)
            {
                case 1: return "Красный (1)";
                case 2: return "Жёлтый (2)";
                case 3: return "Зелёный (3)";
                case 4: return "Голубой (4)";
                case 5: return "Синий (5)";
                case 6: return "Пурпурный (6)";
                case 7: return "Белый (7)";
                default: return "Индекс " + colorIndex;
            }
        }
    }
}
