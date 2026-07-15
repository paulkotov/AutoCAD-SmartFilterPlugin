using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using AcSmartFilterPlugin.Models;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace AcSmartFilterPlugin.Services
{
    /// <summary>
    /// Реализация <see cref="ILayerFilterService"/> поверх .NET API AutoCAD.
    /// Вся работа с транзакциями и блокировкой документа сосредоточена здесь.
    /// </summary>
    public sealed class LayerFilterService : ILayerFilterService
    {
        public bool HasActiveDocument => AcApp.DocumentManager.MdiActiveDocument != null;

        public ObjectId GetCurrentLayerId()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            return doc?.Database.Clayer ?? ObjectId.Null;
        }

        public LayerSnapshot LoadLayers()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                return null;
            }

            var layers = new List<LayerInfo>();
            ObjectId currentLayerId;

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                currentLayerId = doc.Database.Clayer;

                var layerTable = (LayerTable)tr.GetObject(doc.Database.LayerTableId, OpenMode.ForRead);
                foreach (ObjectId layerId in layerTable)
                {
                    var layer = (LayerTableRecord)tr.GetObject(layerId, OpenMode.ForRead);
                    layers.Add(new LayerInfo(layerId, layer.Name, layer.IsOff, layer.IsFrozen));
                }

                tr.Commit();
            }

            return new LayerSnapshot(layers, currentLayerId);
        }

        public void ApplyFilter(IReadOnlyCollection<ObjectId> selectedLayerIds)
        {
            if (selectedLayerIds == null || selectedLayerIds.Count == 0)
            {
                return;
            }

            var doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                return;
            }

            var db = doc.Database;

            using (doc.LockDocument())
            using (var tr = db.TransactionManager.StartTransaction())
            {
                // Если текущий слой будет выключен, AutoCAD выдаёт предупреждение.
                // Переназначаем текущий слой на один из выбранных, чтобы этого избежать.
                if (!selectedLayerIds.Contains(db.Clayer))
                {
                    db.Clayer = selectedLayerIds.First();
                }

                var layerTable = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                foreach (ObjectId layerId in layerTable)
                {
                    var layer = (LayerTableRecord)tr.GetObject(layerId, OpenMode.ForWrite);

                    if (selectedLayerIds.Contains(layerId))
                    {
                        if (layer.IsFrozen)
                        {
                            layer.IsFrozen = false;
                        }

                        layer.IsOff = false;
                    }
                    else
                    {
                        layer.IsOff = true;
                    }
                }

                tr.Commit();
            }

            doc.Editor.Regen();
        }

        public void RestoreState(LayerSnapshot snapshot)
        {
            if (snapshot == null || snapshot.Layers.Count == 0)
            {
                return;
            }

            var doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                return;
            }

            var originalById = snapshot.Layers.ToDictionary(l => l.Id);

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                // Сначала возвращаем исходный текущий слой (он не был заморожен),
                // чтобы можно было корректно восстановить заморозку прочих слоёв.
                if (!snapshot.CurrentLayerId.IsNull && snapshot.CurrentLayerId.IsValid)
                {
                    doc.Database.Clayer = snapshot.CurrentLayerId;
                }

                var layerTable = (LayerTable)tr.GetObject(doc.Database.LayerTableId, OpenMode.ForRead);
                foreach (ObjectId layerId in layerTable)
                {
                    if (!originalById.TryGetValue(layerId, out var original))
                    {
                        continue;
                    }

                    var layer = (LayerTableRecord)tr.GetObject(layerId, OpenMode.ForWrite);

                    // Замораживать текущий слой нельзя — пропускаем такой случай.
                    bool isCurrentLayer = layerId == doc.Database.Clayer;
                    bool canRestoreFreeze = layer.IsFrozen != original.IsFrozen
                        && !(original.IsFrozen && isCurrentLayer);
                    if (canRestoreFreeze)
                    {
                        layer.IsFrozen = original.IsFrozen;
                    }

                    if (layer.IsOff != original.IsOff)
                    {
                        layer.IsOff = original.IsOff;
                    }
                }

                tr.Commit();
            }

            doc.Editor.Regen();
        }
    }
}
