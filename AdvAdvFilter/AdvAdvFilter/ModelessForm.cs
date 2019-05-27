namespace AdvAdvFilter
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Data;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;
    using Autodesk.Revit.UI.Events;

    using Point = System.Drawing.Point;
    using Panel = System.Windows.Forms.Panel;

    public partial class ModelessForm : System.Windows.Forms.Form
    {

        #region Fields

        #region Fields: Modeless Form

        // Essential API data that is needed
        private ExternalCommandData commandData;
        private UIApplication uiApp;
        private UIDocument uiDoc;
        private Document doc;

        // Element protection
        private ElementProtectionUpdater elementProtectionUpdater;

        // Application of Revit?
        private UIControlledApplication uiCtrlApp;

        // Events and Handlers
        private EventHandler eventHandler;
        private ExternalEvent externalEvent;
        private ManualResetEvent resetEvent;

        private int counterOfAPI = 0;
        private string counterOfAPIstring = string.Empty;

        private bool haltIdlingHandler = false;
        private Element selectedElement; // Might change this

        #endregion Fields: Modeless Form

        #region Fields: Controllers

        DataController dataController;
        RevitController revitController;
        ElementSelectionController selectionController;

        #endregion Fields: Controllers

        #region Fields: API Handler Requests

        // Make an enum for all the possible requests
        enum Request
        {
            Nothing = 0,
            UpdateTreeView = 1,
            UpdateTreeViewSelection = 2,
            SelectElementIds = 3,


            Invalid = -1
        }

        List<Request> failureList;
        List<Request> actionQueue;

        int TestInt = 0;

        #endregion Fields: API Handler Requests

        #endregion Fields

        #region Essential Form Methods

        public ModelessForm(
            ExternalCommandData commandData,
            UIControlledApplication uiCtrlApp,
            ExternalEvent externalEvent,
            EventHandler eventHandler,
            ElementProtectionUpdater elementProtectionUpdater,
            List<Element> elementList,
            Main.SetSelection selectionTool
            )
        {
            InitializeComponent();

            //selectionTool(new List<ElementId>());
            
            // Stop IdlingHandler from executing during initialization
            this.haltIdlingHandler = true;

            // Get all data needed for use on the modeless form
            this.commandData = commandData;
            this.uiApp = this.commandData.Application;
            this.uiDoc = this.uiApp.ActiveUIDocument;
            this.doc = this.uiDoc.Document;
            this.uiCtrlApp = uiCtrlApp;
            this.externalEvent = externalEvent;
            this.eventHandler = eventHandler;
            this.elementProtectionUpdater = elementProtectionUpdater;

            // Initialize controllers
            this.selectionController = new ElementSelectionController(ElementSelectionPanel, ElementSelectionLabel, ElementSelectionTreeView);
            this.revitController = new RevitController(commandData);
            this.dataController = new DataController();

            // Initialize the failureList
            failureList = new List<Request>();
            // Initialize the action queue
            actionQueue = new List<Request>();

            // Get elementList, not sure what to use it for
            elementList = elementList.Where(e => null != e.Category && e.Category.HasMaterialQuantities).ToList();

            // Execute method ModelessForm_FormClosed when the form is closing
            this.FormClosing += this.ModelessForm_FormClosed;

            // Resume IdlingHandler
            this.haltIdlingHandler = false;

        }

        private void ModelessForm_FormClosed(object sender, FormClosingEventArgs e)
        {
            this.haltIdlingHandler = true;
            e.Cancel = true;
            this.ExecuteWithAPIContext(this.APIClose, null);
        }

        private void ModelessForm_Load(object sender, EventArgs e)
        {
            this.actionQueue.Clear();
            this.actionQueue.Add(Request.UpdateTreeView);
        }

        #endregion Essential Form Methods

        #region EventHandlers

        private void ElementSelectionTreeView_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            // .. Don't know what to use it for yet, keeping it here just in case
        }

        private void ElementSelectionTreeView_BeforeCollapse(object sender, TreeViewCancelEventArgs e)
        {

            BranchTreeNode branch = e.Node as BranchTreeNode;

            if (branch != null)
            {
                if (branch.Name == "System.String") // This is for "CategoryType" Only
                {
                    selectionController.ToggleCollapse(branch);
                    e.Cancel = true;
                }
            }
        }

        private void ElementSelectionTreeView_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (e.Action == TreeViewAction.Unknown)
                return;

            this.haltIdlingHandler = true;

            selectionController.UpdateAfterCheck(e.Node as AdvTreeNode);

            selectionController.UpdateTotalSelectedItemsLabel();

            this.actionQueue.Add(Request.SelectElementIds);

            this.haltIdlingHandler = false;
        }

        private void ElementSelectionTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Action == TreeViewAction.Unknown)
                return;

            this.haltIdlingHandler = true;

            selectionController.UpdateAfterCheck(e.Node as AdvTreeNode);

            selectionController.UpdateTotalSelectedItemsLabel();

            this.actionQueue.Add(Request.SelectElementIds);

            this.haltIdlingHandler = false;
        }

        #endregion EventHandlers

        #region Supplementary Form Methods

        #endregion Supplementary Form Methods

        #region API CALLS

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr handle);

        /// <summary>
        /// The event while the app is idling (checks what is selected)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public void SelectionChanged_UIAppEvent_WhileIdling(object sender, IdlingEventArgs args)
        {

            try
            {
                args.SetRaiseWithoutDelay();

                if (!this.haltIdlingHandler)
                {
                    // This is to get the essential objects to use the Revit API
                    UIApplication uiApp = sender as UIApplication;
                    UIDocument uiDoc = uiApp.ActiveUIDocument;
                    Document activeDoc = uiDoc.Document;

                    // This is to check any time something in the model may have changed or something was selected
                    ICollection<ElementId> selectedElementIds = uiDoc.Selection.GetElementIds();

                    Request request;
                    List<ElementId> elementIds = null;

                    // Handle the first request of the Queue as long as the actionQueue isn't 0
                    if (this.actionQueue.Count != 0)
                    {
                        request = this.actionQueue[0];
                        this.actionQueue.RemoveAt(0);

                        // This switch statement sets up the data needed to invoke a visible
                        // change within the Modeless Form or the Revit software itself,
                        // also makes last minute request overrides in case the change isn't needed to
                        // not waste tons of CPU cycles and keeps the application responsive.
                        switch (request)
                        {
                            // Updates the TreeView from the list of elementIds from Revit's view
                            case Request.UpdateTreeView:

                                // Update / Refresh the revitController's view
                                revitController.UpdateView();
                                // Get Elements from the newly refreshed view
                                elementIds = revitController.GetElementsFromView();
                                // If dataController's UpdateAllElements(...) method returns FALSE,
                                // Then switch the request from Request.UpdateTreeView to Request.Nothing
                                if (!dataController.UpdateAllElements(elementIds))
                                    request = Request.Nothing;
                                else
                                    elementIds = dataController.AllElements;

                                break;
                            // Make Revit select elements from a given elementId list
                            case Request.SelectElementIds:

                                // Get ElementId list that the ModelessForm requests to select
                                elementIds = selectionController.GetSelectedElementIds();
                                // TODO: Make an exit function if elementIds == null
                                /*
                                if (elementIds == null)
                                {
                                    actionQueue.Insert(0, request);
                                    request = Request.Nothing;
                                }
                                */


                                break;
                            // This case usually handles Request.Invalid or Request.Nothing
                            default:

                                // When the given Request holds no significance, try to perform polling and
                                // check if the Revit application changed states and values to update Modeless Form
                                elementIds = selectionController.GetSelectedElementIds();
                                // TODO: Make an exit function if elementIds == null
                                /*
                                if (elementIds == null)
                                {
                                    actionQueue.Insert(0, request);
                                    request = Request.Nothing;
                                }
                                */
                                List<ElementId> currentSelected = this.uiDoc.Selection.GetElementIds().ToList<ElementId>();

                                if (!selectionController.IsListEqual(elementIds, currentSelected))
                                {
                                    request = Request.UpdateTreeViewSelection;
                                }

                                break;
                        }
                    }
                    else
                    {
                        request = Request.Nothing;

                        // When the given Request holds no significance, try to perform polling and
                        // check if the Revit application changed states and values to update Modeless Form
                        elementIds = selectionController.GetSelectedElementIds();
                        // TODO: Make an exit function if elementIds == null
                        /*
                        if (elementIds == null)
                        {
                            actionQueue.Insert(0, request);
                            request = Request.Nothing;
                        }
                        */

                        List<ElementId> currentSelected = this.uiDoc.Selection.GetElementIds().ToList<ElementId>();

                        if (!selectionController.IsListEqual(elementIds, currentSelected))
                        {
                            request = Request.UpdateTreeViewSelection;
                        }

                    }


                    switch (request)
                    {
                        case Request.UpdateTreeView:

                            // Update the treeView element within the form
                            this.BeginInvoke(new Action(() =>
                            {
                                selectionController.UpdateTreeView(elementIds, revitController);
                            }));

                            break;
                        case Request.UpdateTreeViewSelection:

                            ICollection<ElementId> currentSelected = this.uiDoc.Selection.GetElementIds();
                            this.BeginInvoke(new Action(() =>
                            {
                                bool updateSucceeded = selectionController.UpdateSelectedLeaves(currentSelected);
                                if (!updateSucceeded)
                                {
                                    if (failureList.Contains(Request.UpdateTreeViewSelection))
                                    {
                                        // Prompt user with error message if the refreshed reattempt still failed.
                                        MessageBox.Show("Fatal Error: selectionController failed to update the selected leaves from the Revit application",
                                            "Debug - SelectionChanged_UIAppEvent_WhileIdling(...)");
                                        failureList.Remove(Request.UpdateTreeViewSelection);
                                    }
                                    else
                                    {
                                        // Add a failure list to prevent infinite loops / requests
                                        failureList.Add(Request.UpdateTreeViewSelection);
                                        // Demand an update request to refresh the treeview structure and its selection
                                        actionQueue.Insert(0, Request.UpdateTreeViewSelection);
                                        actionQueue.Insert(0, Request.UpdateTreeView);
                                    }

                                }
                                else
                                {
                                    // if failureList has UpdateTreeViewSelection, remove it
                                    if (failureList.Contains(Request.UpdateTreeViewSelection))
                                        failureList.Remove(Request.UpdateTreeViewSelection);
                                }

                            }));

                            break;
                        case Request.SelectElementIds:

                            // revitController.MakeNewSelection(elementIds);
                            this.uiDoc.Selection.SetElementIds(elementIds);

                            break;
                        case Request.Nothing:
                            // Do absolutely nothing
                            break;
                        default:
                            
                            // If the request isn't any other request (even Nothing), then prompt user with warning message
                            MessageBox.Show("Warning: Handler given invalid request",
                                "Debug - SelectionChanged_UIAppEvent_WhileIdling");

                            break;

                    }
                }

            }
            catch (Exception ex)
            {
                ErrorReport.Report(ex);
            }
        }

        #region Not Need Attention

        /// <summary>
        /// executes the function passed and the object parameter
        /// </summary>
        /// <param name="actionToExecute"> the action executed </param>
        /// <param name="args"> the event arguments </param>
        /// <param name="extendWait"> if we are extending the wait </param>
        private void ExecuteWithAPIContext(Action<UIApplication, object> actionToExecute, object args = null, bool extendWait = false)
        {
            this.counterOfAPI += 1;
            this.counterOfAPIstring += actionToExecute.Method.Name + "\n";

            this.haltIdlingHandler = true;
            bool executed = false;

            // Prevent user from stealing focus from Revit
            this.SetStyle(ControlStyles.Selectable, false);

            this.resetEvent = new ManualResetEvent(false);

            this.eventHandler.SetActionAndRaise(actionToExecute, this.resetEvent, args);

            // Try to force execution of event.
            // Adapted from Jo Ye, ACE DevBlog.
            // http://adndevblog.typepad.com/aec/2013/07/tricks-to-force-trigger-idling-event.html
            SetForegroundWindow(this.Handle);

            SetForegroundWindow(Autodesk.Windows.ComponentManager.ApplicationWindow);

            Cursor.Position = new System.Drawing.Point(Cursor.Position.X + 1, Cursor.Position.Y + 1);
            Cursor.Position = new System.Drawing.Point(Cursor.Position.X - 1, Cursor.Position.Y - 1);

            if (extendWait)
            {
                executed = this.resetEvent.WaitOne();
            }
            else
            {
                executed = this.resetEvent.WaitOne(1000);

                for (int i = 0; (i < 4 && !executed); i++)
                {
                    SetForegroundWindow(this.Handle);

                    SetForegroundWindow(Autodesk.Windows.ComponentManager.ApplicationWindow);

                    Cursor.Position = new Point(Cursor.Position.X + 1, Cursor.Position.Y + 1);
                    Cursor.Position = new Point(Cursor.Position.X - 1, Cursor.Position.Y - 1);

                    executed = this.resetEvent.WaitOne(1000);

                    if (this.eventHandler.Executing)
                    {
                        executed = this.resetEvent.WaitOne();
                        break;
                    }
                }
            }

            if (!executed)
                throw new TimeoutException(string.Format("Execution of {0} failed after multiple attempts.\n", actionToExecute.Method.Name));

            this.SetStyle(ControlStyles.Selectable, false);

            this.resetEvent.Reset();

            this.Focus();

            this.haltIdlingHandler = false;
        }

        /// <summary>
        /// Closes the Form
        /// </summary>
        /// <param name="uiApp"> the app </param>
        /// <param name="args"> the event args </param>
        private void APIClose(UIApplication uiApp, object args)
        {
            try
            {
                //// if closing the form then use this:
                this.resetEvent.Set();
                this.elementProtectionUpdater.ProtectedElementIds.Clear();

                // terminate event handler for SelectionChanged_UIAppEvent_WhileIdling
                Main.UiCtrlApp.Idling -= Main.ActiveModelessForm.SelectionChanged_UIAppEvent_WhileIdling;

                this.Invoke(new Action(() =>
                {
                    Application.ExitThread();
                }));
            }
            catch (Exception ex)
            {
                ErrorReport.Report(ex);
            }
        }

        /// <summary>
        /// modifications to the form
        /// </summary>
        /// <param name="uiApp"> the app </param>
        /// <param name="args"> the event args </param>
        private void APIEdit(UIApplication uiApp, object args)
        {
            try
            {
                //// for any modifications to the form
                UIDocument uiDoc = uiApp.ActiveUIDocument;
                Document activeDoc = uiDoc.Document;
                this.doc = activeDoc;
                Element elem = args as Element;

                OverrideGraphicSettings ogs = new OverrideGraphicSettings();
                ogs.SetProjectionLineColor(new Color(0, 255, 0));
                ogs.SetCutFillColor(new Color(0, 255, 0));
                ogs.SetProjectionFillColor(new Color(0, 255, 0));
                ogs.SetProjectionLineColor(new Color(0, 255, 0));

                using (Transaction framePanelTransaction = new Transaction(activeDoc, "t1"))
                {
                    framePanelTransaction.Start("t1");

                    activeDoc.ActiveView.SetElementOverrides(elem.Id, ogs);

                    //// refresh everything:
                    activeDoc.Regenerate();
                    framePanelTransaction.Commit();
                }
            }
            catch (Exception ex)
            {
                ErrorReport.Report(ex);
            }
        }

        /// <summary>
        /// highlights the elements in the list passed in the args parameter
        /// </summary>
        /// <param name="uiApp"> the app </param>
        /// <param name="args"> the event args </param>
        private void APIHighlight(UIApplication uiApp, object args)
        {
            try
            {
                UIDocument uiDoc = uiApp.ActiveUIDocument;
                List<ElementId> elementsToHighlight = args as List<ElementId>;

                if (elementsToHighlight.Count > 0)
                {
                    uiDoc.Selection.SetElementIds(elementsToHighlight);
                }

                this.resetEvent.Set();
            }
            catch (Exception ex)
            {
                ErrorReport.Report(ex);
            }
        }

        #endregion

        #endregion

    }
}
