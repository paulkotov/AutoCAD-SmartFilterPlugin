using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using AcSmartFilterPlugin.Models;

using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace AcSmartFilterPlugin.Services
{
    /// <summary>
    /// Реализация <see cref="IFilterConfigStore"/> поверх .NET API AutoCAD.
    /// Конфигурация хранится в объекте <see cref="Xrecord"/>, помещённом в
    /// собственный словарь (<see cref="DictionaryName"/>) внутри словаря
    /// именованных объектов чертежа (Named Objects Dictionary). Благодаря этому
    /// настройки фильтра сохраняются вместе с DWG-файлом.
    /// </summary>
    public sealed class FilterConfigStore : IFilterConfigStore
    {
        /// <summary>Имя пользовательского словаря в NOD.</summary>
        private const string DictionaryName = "AcSmartFilterPlugin";

        /// <summary>Ключ записи конфигурации внутри словаря плагина.</summary>
        private const string ConfigKey = "FilterConfig";

        public void Save(FilterConfig config)
        {
            if (config == null)
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
                var nod = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);

                DBDictionary pluginDict;
                if (nod.Contains(DictionaryName))
                {
                    pluginDict = (DBDictionary)tr.GetObject(nod.GetAt(DictionaryName), OpenMode.ForWrite);
                }
                else
                {
                    nod.UpgradeOpen();
                    pluginDict = new DBDictionary();
                    nod.SetAt(DictionaryName, pluginDict);
                    tr.AddNewlyCreatedDBObject(pluginDict, true);
                }

                using (var data = BuildResultBuffer(config))
                {
                    if (pluginDict.Contains(ConfigKey))
                    {
                        var existing = (Xrecord)tr.GetObject(pluginDict.GetAt(ConfigKey), OpenMode.ForWrite);
                        existing.Data = data;
                    }
                    else
                    {
                        var xrecord = new Xrecord { Data = data };
                        pluginDict.SetAt(ConfigKey, xrecord);
                        tr.AddNewlyCreatedDBObject(xrecord, true);
                    }
                }

                tr.Commit();
            }
        }

        public FilterConfig Load()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                return null;
            }

            var db = doc.Database;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var nod = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);
                if (!nod.Contains(DictionaryName))
                {
                    return null;
                }

                var pluginDict = (DBDictionary)tr.GetObject(nod.GetAt(DictionaryName), OpenMode.ForRead);
                if (!pluginDict.Contains(ConfigKey))
                {
                    return null;
                }

                var xrecord = (Xrecord)tr.GetObject(pluginDict.GetAt(ConfigKey), OpenMode.ForRead);
                var config = ParseResultBuffer(xrecord.Data);

                tr.Commit();
                return config;
            }
        }

        /// <summary>
        /// Формирует буфер результатов: сначала флаг «текущий слой» (Int16),
        /// затем имена выбранных слоёв (Text).
        /// </summary>
        private static ResultBuffer BuildResultBuffer(FilterConfig config)
        {
            var values = new List<TypedValue>
            {
                new TypedValue((int)DxfCode.Int16, (short)(config.UseCurrentLayer ? 1 : 0))
            };

            foreach (var name in config.LayerNames)
            {
                if (!string.IsNullOrEmpty(name))
                {
                    values.Add(new TypedValue((int)DxfCode.Text, name));
                }
            }

            return new ResultBuffer(values.ToArray());
        }

        private static FilterConfig ParseResultBuffer(ResultBuffer data)
        {
            var useCurrentLayer = false;
            var layerNames = new List<string>();

            if (data != null)
            {
                var flagRead = false;
                foreach (TypedValue value in data)
                {
                    if (value.TypeCode == (int)DxfCode.Int16 && !flagRead)
                    {
                        useCurrentLayer = Convert.ToInt16(value.Value) != 0;
                        flagRead = true;
                    }
                    else if (value.TypeCode == (int)DxfCode.Text)
                    {
                        layerNames.Add((string)value.Value);
                    }
                }
            }

            return new FilterConfig(layerNames, useCurrentLayer);
        }
    }
}
