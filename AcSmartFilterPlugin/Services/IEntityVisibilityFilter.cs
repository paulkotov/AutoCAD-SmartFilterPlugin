using AcSmartFilterPlugin.Models;

namespace AcSmartFilterPlugin.Services
{
    /// <summary>
    /// Скрывает объекты, которые не совпали с типом и цветом указанного объекта,
    /// и возвращает им видимость.
    /// </summary>
    public interface IEntityVisibilityFilter
    {
        /// <summary>
        /// Скрывает несовпавшие объекты пространства модели и текущего листа.
        /// Пустой тип или <c>null</c>-цвет означает, что этот критерий не используется.
        /// Видовые экраны не скрываются, если фильтр ищет не их тип.
        /// </summary>
        EntityVisibilitySnapshot HideNonMatching(string objectType, ObjectColor color);

        /// <summary>
        /// Снова показывает объекты из снимка.
        /// Возвращает те, которые показать не удалось.
        /// </summary>
        EntityVisibilitySnapshot Restore(EntityVisibilitySnapshot snapshot);
    }
}
