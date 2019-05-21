namespace AdvAdvFilter
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Forms;
    using System.Windows.Media.Imaging;
    //using System.Windows.Media.Imaging;

    using Autodesk.Revit.Attributes;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;

    public class Main : IExternalApplication
    {
        #region Constants

        // Ribbon names
        private static string RIBBON_TAB_NAME = "AA Filter";
        private static string RIBBON_PANEL_NAME = "Adv. Adv. Filter";

        // Push button names & tooltips
        private static string PUSHBUTTON_TEXT = "Advanced Advanced Filter";
        private static string PUSHBUTTON_TOOLTIP = "Filtering but advanced";


        #endregion

        #region Fields

        // Application objects (For Revit and the current extension)
        private static UIControlledApplication uiCtrlApp;
        private static Main activeExtrApp;

        // Objects relating to events and the activeModelessForm (The pop-up window)
        private static ExternalEvent externalEvent;
        private static EventHandler eventHandler;
        private static ModelessForm activeModelessForm;

        // Unknown
        private static Thread modelessFormThread;

        private static ElementProtectionUpdater elementProtectionUpdater;

        #endregion

        #region Properties

        /// <summary>
        /// The active UIControlledApplication for the IExternal Application
        /// </summary>
        public static UIControlledApplication UiCtrlApp
        {
            get { return Main.uiCtrlApp; }
        }

        /// <summary>
        /// The active ExternalApplication
        /// </summary>
        public static Main ActiveExtrApp
        {
            get { return Main.activeExtrApp; }
        }

        /// <summary>
        /// The active <see cref="EventHandler"/>.
        /// </summary>
        public static EventHandler EventHandler
        {
            get { return Main.eventHandler; }
            set { Main.eventHandler = value; }
        }

        /// <summary>
        /// The thread the <see cref="ActiveModelessForm"/> is running on.
        /// </summary>
        public static Thread ModelessFormThread
        {
            get { return Main.modelessFormThread; }
            set { Main.modelessFormThread = value; }
        }

        /// <summary>
        /// The active <see cref="ModelessForm"/>
        /// </summary>
        public static ModelessForm ActiveModelessForm
        {
            get { return Main.activeModelessForm; }
            set { Main.activeModelessForm = value; }
        }

        /// <summary>
        /// The active <see cref="ElementProtectionUpdater"/>.
        /// </summary>
        public static ElementProtectionUpdater ElementProtectionUpdater
        {
            get { return Main.elementProtectionUpdater; }
            set { Main.elementProtectionUpdater = value; }
        }

        #endregion

        #region Static Methods

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static bool IsUserWindowsActive()
        {
            if (Main.ActiveModelessForm != null)
                return true;

            return false;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="app"></param>
        /// <returns>The Result of the command</returns>
        public Result OnStartup(UIControlledApplication app)
        {
            Main.uiCtrlApp = app;
            Main.activeExtrApp = this;

            // Create Updater to prevent panel element modification while interface is open
            Main.elementProtectionUpdater = new ElementProtectionUpdater(app.ActiveAddInId);
            UpdaterRegistry.RegisterUpdater(elementProtectionUpdater);
            LogicalOrFilter filter
                = new LogicalOrFilter(
                    new List<ElementFilter>
                    {
                        new ElementCategoryFilter(BuiltInCategory.OST_Walls),
                        new ElementCategoryFilter(BuiltInCategory.OST_Floors),
                        new ElementCategoryFilter(BuiltInCategory.OST_Windows),
                        new ElementCategoryFilter(BuiltInCategory.OST_Doors),
                        new ElementCategoryFilter(BuiltInCategory.OST_StructConnections),
                    });
            UpdaterRegistry.AddTrigger(elementProtectionUpdater.GetUpdaterId(), filter, Element.GetChangeTypeAny());

            // Add the ribbon panel
            this.AddExtensionRibbon(app);

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication app)
        {
            return Result.Succeeded;
        }

        public ModelessForm CreateModelessForm(ExternalCommandData commandData, List<Element> protectedElements)
        {

            Main.eventHandler = new EventHandler();
            Main.externalEvent = ExternalEvent.Create(eventHandler);
            Main.eventHandler.ExternalEvent = Main.externalEvent;

            // Precent floor elements from being modified
            foreach (Element elem in protectedElements)
            {
                Main.ElementProtectionUpdater.ProtectedElementIds.Add(elem.Id);
            }

            // Disable ribbon panel to prevent conflicting commands
            foreach (RibbonPanel panel in Main.UiCtrlApp.GetRibbonPanels( RIBBON_TAB_NAME ))
            {
                panel.Enabled = false;
            }

            Main.ModelessFormThread
                = new Thread(delegate ()
                {
                    try
                    {
                        ModelessForm modelessForm = new ModelessForm(
                                commandData,
                                Main.UiCtrlApp,
                                externalEvent,
                                eventHandler,
                                Main.ElementProtectionUpdater,
                                protectedElements
                            );

                        Main.ActiveModelessForm = modelessForm;

                        Main.ActiveModelessForm.Show(new RevitApplicationWindow());

                        ManualResetEvent resetEvent = new ManualResetEvent(false);

                        Main.EventHandler.SetActionAndRaise(
                            delegate (UIApplication uiApp, object args)
                            {
                                // SIMILAR CODE (00)
                                try
                                {
                                    // Subscribe to idling event to update UI when element selection changes.
                                    Main.UiCtrlApp.Idling +=
                                        Main.ActiveModelessForm.SelectionChanged_UIAppEvent_WhileIdling;
                                }
                                catch (Exception ex)
                                {
                                    ErrorReport.Report(ex);
                                }
                            },
                            resetEvent);

                        resetEvent.WaitOne();

                        Application.Run(Main.ActiveModelessForm);
                    }
                    catch (Exception ex)
                    {
                        ErrorReport.Report(ex);
                    }
                    finally
                    {
                        try
                        {
                            // Clear protected elements
                            Main.ElementProtectionUpdater.ProtectedElementIds.Clear();

                            // Wait for any executing methods to finish
                            while (Main.EventHandler.Executing)
                            {
                                Thread.Sleep(500);      // Sleep for a brief time to lessen the load of spin locking
                            }

                            ManualResetEvent resetEvent = new ManualResetEvent(false);

                            Main.EventHandler.SetActionAndRaise(
                                delegate (UIApplication uiApp, object args)
                                {
                                    // SIMILAR CODE (00)
                                    try
                                    {
                                        // Deregister handlers and nullify active framing form.
                                        Main.UiCtrlApp.Idling -=
                                            Main.ActiveModelessForm.SelectionChanged_UIAppEvent_WhileIdling;

                                        Main.ActiveModelessForm = null;
                                    }
                                    catch (Exception ex)
                                    {
                                        ErrorReport.Report(ex);
                                    }
                                },
                                resetEvent);

                            resetEvent.WaitOne();

                            // Restore ribbon panel
                            Main.EventHandler.SetActionAndRaise(this.RestoreModelessRibbonPanel, new ManualResetEvent(false));
                        }
                        catch (Exception ex)
                        {
                            ErrorReport.Report(ex);
                        }
                    }
                });

            // ApartmentState.STA needed for FolderBrowserDialog and OpenFileDialog.
            Main.ModelessFormThread.SetApartmentState(ApartmentState.STA);
            Main.ModelessFormThread.Start();

            return Main.activeModelessForm;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Adds a Ribbon panel as an extension for Revit
        /// </summary>
        /// <param name="app">The application</param>
        private void AddExtensionRibbon(UIControlledApplication app)
        {
            // Create the RibbonTab with the name RIBBON_TAB_NAME
            app.CreateRibbonTab( RIBBON_TAB_NAME );

            // Place a panel into the tab of RIBBON_TAB_NAME
            RibbonPanel ribbonPanel = app.CreateRibbonPanel( RIBBON_TAB_NAME, RIBBON_PANEL_NAME );

            // Call AddExtensionButtons to add buttons onto ribbonPanel
            this.AddExtensionButtons(ribbonPanel);
        }

        /// <summary>
        /// Add buttons to the given ribbonPanel
        /// </summary>
        /// <param name="ribbonPanel">The ribbon panel that the buttons are put into</param>
        private void AddExtensionButtons(RibbonPanel ribbonPanel)
        {
            // Create data for the push button that (should) call on ModelessFormCommand.cs when pressed
            PushButtonData createModelessPushButtonData
                = new PushButtonData(
                    "AdvAdvFilter",
                    PUSHBUTTON_TEXT,
                    Assembly.GetExecutingAssembly().Location,
                    "AdvAdvFilter.ModelessFormCommand");

            // Use the data created in createModelessPushButtonData to create a PushButton
            PushButton createModelessFormPushButton = ribbonPanel.AddItem(createModelessPushButtonData) as PushButton;

            // Add an image to the push button
            createModelessFormPushButton.LargeImage
                = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                        AdvAdvFilter.Properties.Resources.Duck.GetHbitmap(),
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());

            // Add a tooltip to the push button
            createModelessFormPushButton.ToolTip = PUSHBUTTON_TOOLTIP;
        }

        /// <summary>
        /// Restores the RibbonPanel.
        /// </summary>
        /// <param name="uiApp">The active UIApplication</param>
        /// <param name="args">null, needed for EventHandler.SetActionAndRaise()</param>
        private void RestoreModelessRibbonPanel(UIApplication uiApp, object args = null)
        {
            try
            {
                // Enable every individual ribbonPanel inside RIBBON_TAB_NAME
                foreach (RibbonPanel ribbonPanel in uiApp.GetRibbonPanels(RIBBON_TAB_NAME))
                    ribbonPanel.Enabled = true;
            }
            catch (Exception ex)
            {
                ErrorReport.Report(ex);
            }
                
        }

        #endregion

    }
}
