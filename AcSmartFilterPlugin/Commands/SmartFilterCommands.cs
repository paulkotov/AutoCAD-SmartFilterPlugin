using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using AcSmartFilterPlugin.Views;
using System;

[assembly: CommandClass(typeof(AcSmartFilterPlugin.Commands.SmartFilterCommands))]

namespace AcSmartFilterPlugin.Commands
{
    public class SmartFilterCommands
    {
        // Храним ссылку, чтобы окно не было собрано сборщиком мусора, пока открыто.
        private static FilterView _filterView;

        [CommandMethod("MyGroup", "SmartFilter", CommandFlags.Modal)]
        public void ShowSmartFilter()
        {
            if (_filterView == null)
            {
                _filterView = new FilterView();
                _filterView.Closed += (s, e) => _filterView = null;
                Application.ShowModelessWindow(_filterView);
            }
            else
            {
                _filterView.Activate();
            }
        }

        [CommandMethod("MyGroup", "MyCommand", CommandFlags.Modal)]
        public void MyCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed;
            if (doc != null)
            {
                ed = doc.Editor;
                ed.WriteMessage("Hello, this is your first command.");

            }
        }

        [CommandMethod("MyGroup", "MyPickFirst", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void MyPickFirst()
        {
            var ed = Application.DocumentManager.MdiActiveDocument.Editor;
            PromptSelectionResult result = ed.GetSelection();
            if (result.Status == PromptStatus.OK)
            {
                // There are selected entities
                // Put your command using pickfirst set code here
            }
            else
            {
                // There are no selected entities
                // Put your command code here
            }
        }

    }
}
