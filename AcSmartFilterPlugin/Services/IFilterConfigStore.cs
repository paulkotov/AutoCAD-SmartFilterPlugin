using AcSmartFilterPlugin.Models;

namespace AcSmartFilterPlugin.Services
{
    /// <summary>
    /// Сохраняет и читает конфигурацию фильтра внутри активного чертежа (DWG).
    /// Данные хранятся в объекте <c>Xrecord</c> в словаре именованных объектов.
    /// </summary>
    public interface IFilterConfigStore
    {
        /// <summary>Сохраняет конфигурацию фильтра в активный чертёж.</summary>
        void Save(FilterConfig config);

        /// <summary>
        /// Читает ранее сохранённую конфигурацию фильтра из активного чертежа.
        /// Возвращает <c>null</c>, если данных нет или нет активного документа.
        /// </summary>
        FilterConfig Load();
    }
}
