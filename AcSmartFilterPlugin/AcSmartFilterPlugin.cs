using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Windows;
using System;
using System.Linq;
using System.Windows.Input;

// This line is not mandatory, but improves loading performances
[assembly: ExtensionApplication(typeof(AcSmartFilterPlugin.AcSmartFilterPlugin))]

namespace AcSmartFilterPlugin
{
    // This class is instantiated by AutoCAD once and kept alive for the 
    // duration of the session. If you don't do any one time initialization 
    // then you should remove this class.
    public class AcSmartFilterPlugin : IExtensionApplication
    {
        private const string RibbonTabId = "AcSmartFilterPlugin_Tab";
        private const string RibbonPanelId = "AcSmartFilterPlugin_Panel";

        void IExtensionApplication.Initialize()
        {
            // The ribbon may not be ready yet when the plug-in is loaded
            // (e.g. when AutoCAD is still starting). Build it immediately if it
            // already exists, otherwise defer until the ribbon becomes available.
            if (ComponentManager.Ribbon != null)
            {
                CreateRibbon();
            }
            else
            {
                ComponentManager.ItemInitialized += OnRibbonItemInitialized;
            }
        }

        void IExtensionApplication.Terminate()
        {
            ComponentManager.ItemInitialized -= OnRibbonItemInitialized;
        }

        private void OnRibbonItemInitialized(object sender, RibbonItemEventArgs e)
        {
            if (ComponentManager.Ribbon == null)
                return;

            // The ribbon is now available; stop listening and build the UI once.
            ComponentManager.ItemInitialized -= OnRibbonItemInitialized;
            CreateRibbon();
        }

        private void CreateRibbon()
        {
            RibbonControl ribbon = ComponentManager.Ribbon;
            if (ribbon == null)
                return;

            // Avoid adding the tab twice (e.g. reload / workspace change).
            RibbonTab tab = ribbon.FindTab(RibbonTabId);
            if (tab == null)
            {
                tab = new RibbonTab
                {
                    Title = "Smart Filter",
                    Id = RibbonTabId
                };
                ribbon.Tabs.Add(tab);
            }

            bool panelExists = tab.Panels.Any(p => p.Source != null && p.Source.Id == RibbonPanelId);
            if (panelExists)
                return;

            RibbonPanelSource panelSource = new RibbonPanelSource
            {
                Title = "Filters",
                Id = RibbonPanelId
            };

            RibbonButton button = new RibbonButton
            {
                Text = "Smart Filter",
                ShowText = true,
                Name = "Smart Filter",
                Size = RibbonItemSize.Large,
                ShowImage = true,
                CommandHandler = new SendCommandHandler("SmartFilter "),
                CommandParameter = "SmartFilter ",
                ToolTip = "Open the Smart Filter panel."
            };

            panelSource.Items.Add(button);
            tab.Panels.Add(new RibbonPanel { Source = panelSource });
        }

        // Simple ICommand that forwards a command string to the active document.
        private sealed class SendCommandHandler : ICommand
        {
            private readonly string _command;

            public SendCommandHandler(string command)
            {
                _command = command;
            }

            public event EventHandler CanExecuteChanged
            {
                add { }
                remove { }
            }

            public bool CanExecute(object parameter)
            {
                return Application.DocumentManager.MdiActiveDocument != null;
            }

            public void Execute(object parameter)
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                if (doc == null)
                    return;

                doc.SendStringToExecute(_command, true, false, true);
            }
        }
    }
}
