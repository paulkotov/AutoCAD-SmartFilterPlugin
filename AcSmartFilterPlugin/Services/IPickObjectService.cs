using System.Threading.Tasks;
using AcSmartFilterPlugin.Models;

namespace AcSmartFilterPlugin.Services
{
    /// <summary>
    /// Указание одного объекта в чертеже и чтение его характеристик.
    /// </summary>
    public interface IPickObjectService
    {
        /// <summary>
        /// Просит указать объект в чертеже и возвращает его сведения.
        /// <c>null</c> — выбор отменён или объект недоступен для чтения.
        /// </summary>
        Task<PickedObjectInfo> PickObjectAsync();
    }
}
