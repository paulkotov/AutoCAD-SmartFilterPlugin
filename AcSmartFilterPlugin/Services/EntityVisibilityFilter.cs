using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using AcSmartFilterPlugin.Models;

using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;
using AcRxException = Autodesk.AutoCAD.Runtime.Exception;

namespace AcSmartFilterPlugin.Services
{
    /// <summary>
    /// Реализация <see cref="IEntityVisibilityFilter"/>: прячет несовпавшие примитивы
    /// через <see cref="Entity.Visible"/> и запоминает их для восстановления.
    /// </summary>
    public sealed class EntityVisibilityFilter : IEntityVisibilityFilter
    {
        public EntityVisibilitySnapshot HideNonMatching(string objectType, ObjectColor color)
        {
            if (string.IsNullOrEmpty(objectType) && color == null)
            {
                return EntityVisibilitySnapshot.Empty;
            }

            var doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                return EntityVisibilitySnapshot.Empty;
            }

            var hiddenIds = new List<ObjectId>();

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                foreach (var spaceId in GetTargetSpaceIds(tr, doc.Database))
                {
                    var space = (BlockTableRecord)tr.GetObject(spaceId, OpenMode.ForRead);
                    foreach (ObjectId id in space)
                    {
                        if (!TryGetEntity(tr, id, out var entity)
                            || !entity.Visible
                            || IsProtectedViewport(entity, objectType)
                            || Matches(entity, objectType, color))
                        {
                            continue;
                        }

                        try
                        {
                            entity.UpgradeOpen();
                            entity.Visible = false;
                            hiddenIds.Add(entity.ObjectId);
                        }
                        catch (AcRxException)
                        {
                            // Заблокированный слой и нередактируемые объекты остаются видимыми.
                        }
                    }
                }

                if (hiddenIds.Count == 0)
                {
                    return EntityVisibilitySnapshot.Empty;
                }

                tr.Commit();
            }

            doc.Editor.Regen();
            return new EntityVisibilitySnapshot(hiddenIds);
        }

        public EntityVisibilitySnapshot Restore(EntityVisibilitySnapshot snapshot)
        {
            if (snapshot == null || snapshot.HiddenEntityIds.Count == 0)
            {
                return EntityVisibilitySnapshot.Empty;
            }

            var doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                return snapshot;
            }

            var stillHidden = new List<ObjectId>();
            var changed = false;

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                foreach (var id in snapshot.HiddenEntityIds)
                {
                    if (!TryGetEntity(tr, id, out var entity))
                    {
                        continue;
                    }

                    if (entity.Visible)
                    {
                        continue;
                    }

                    try
                    {
                        entity.UpgradeOpen();
                        entity.Visible = true;
                        changed = true;
                    }
                    catch (AcRxException)
                    {
                        stillHidden.Add(id);
                    }
                }

                if (!changed)
                {
                    return new EntityVisibilitySnapshot(stillHidden);
                }

                tr.Commit();
            }

            doc.Editor.Regen();
            return new EntityVisibilitySnapshot(stillHidden);
        }

        /// <summary>
        /// Пространство модели и, если пользователь на листе, этот лист.
        /// Через видовой экран видна именно модель, поэтому её фильтруем всегда.
        /// </summary>
        private static IReadOnlyList<ObjectId> GetTargetSpaceIds(Transaction tr, Database db)
        {
            var blockTable = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            var modelSpaceId = blockTable[BlockTableRecord.ModelSpace];
            if (db.CurrentSpaceId.IsNull || db.CurrentSpaceId == modelSpaceId)
            {
                return new[] { modelSpaceId };
            }

            return new[] { modelSpaceId, db.CurrentSpaceId };
        }

        /// <summary>
        /// Видовой экран листа не прячем, иначе пропадёт вид модели.
        /// Исключение — фильтр явно ищет тип VIEWPORT.
        /// </summary>
        private static bool IsProtectedViewport(Entity entity, string objectType)
        {
            if (!(entity is Viewport))
            {
                return false;
            }

            return !string.Equals(objectType, "VIEWPORT", StringComparison.OrdinalIgnoreCase);
        }

        private static bool Matches(Entity entity, string objectType, ObjectColor color)
        {
            if (!string.IsNullOrEmpty(objectType)
                && !string.Equals(EntityProperties.GetObjectType(entity), objectType, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (color != null && !color.Equals(EntityProperties.GetColor(entity)))
            {
                return false;
            }

            return true;
        }

        private static bool TryGetEntity(Transaction tr, ObjectId id, out Entity entity)
        {
            entity = null;
            if (id.IsNull || !id.IsValid || id.IsErased)
            {
                return false;
            }

            try
            {
                entity = tr.GetObject(id, OpenMode.ForRead, false) as Entity;
                return entity != null && !entity.IsErased;
            }
            catch (AcRxException)
            {
                return false;
            }
        }
    }
}
