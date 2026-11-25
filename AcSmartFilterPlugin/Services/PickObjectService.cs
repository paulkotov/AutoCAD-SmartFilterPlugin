using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using AcSmartFilterPlugin.Models;

using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;
using AcRxException = Autodesk.AutoCAD.Runtime.Exception;

namespace AcSmartFilterPlugin.Services
{
    /// <summary>
    /// Реализация <see cref="IPickObjectService"/> поверх запросов редактора AutoCAD.
    /// </summary>
    public sealed class PickObjectService : IPickObjectService
    {
        public async Task<PickedObjectInfo> PickObjectAsync()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                return null;
            }

            PickedObjectInfo pickedObject = null;

            try
            {
                // Окно плагина modeless, поэтому фокус нужно вернуть в AutoCAD,
                // иначе пользователь не сможет указать объект мышью.
                AcApp.MainWindow.Focus();

                // Запросы редактора допустимы только в контексте команды.
                await AcApp.DocumentManager.ExecuteInCommandContextAsync(
                    _ =>
                    {
                        pickedObject = PickObject(doc);
                        return Task.CompletedTask;
                    },
                    null);
            }
            catch (AcRxException)
            {
                return null;
            }

            return pickedObject;
        }

        private static PickedObjectInfo PickObject(Document doc)
        {
            var options = new PromptEntityOptions("\nУкажите объект");

            var result = doc.Editor.GetEntity(options);
            if (result.Status != PromptStatus.OK || result.ObjectId.IsNull)
            {
                return null;
            }

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                if (!(tr.GetObject(result.ObjectId, OpenMode.ForRead) is Entity entity))
                {
                    return null;
                }

                var layer = (LayerTableRecord)tr.GetObject(entity.LayerId, OpenMode.ForRead);
                var pickedObject = new PickedObjectInfo(
                    entity.ObjectId,
                    entity.Handle,
                    GetObjectType(entity),
                    entity.LayerId,
                    layer.Name);

                tr.Commit();
                return pickedObject;
            }
        }

        /// <summary>DXF-имя объекта; для типов без него — имя .NET-класса.</summary>
        private static string GetObjectType(Entity entity)
        {
            var dxfName = entity.GetRXClass().DxfName;
            return string.IsNullOrEmpty(dxfName) ? entity.GetType().Name : dxfName;
        }
    }
}
