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
    using FilterMode = AdvAdvFilter.DataController.FilterMode;
    using Request = AdvAdvFilter.RequestHandler.Request;

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

        // Controls the inner workings of the form and revit 
        DataController dataController;
        RevitController revitController;
        RequestHandler requestHandler;
        // Controls elements visible in modeless form
        ElementSelectionController selectionController;
        OptionController optionController;

        #endregion Fields: Controllers

        #region Fields: API Handler Requests

        // Make an enum for all the possible requests
        /*
        enum Request
        {
            Nothing = 0,
            UpdateTreeView = 1,
            UpdateTreeViewSelection = 2,
            SelectElementIds = 3,
            Invalid = -1
        }
        */
        struct Condition
        {
            public bool hideUnselected;
            public FilterMode filterSelection;
        };
        
        // Records requests that have previously failed so that the API handler will abort the action
        // when the handler tries to resolve the request a second time.
        List<Request> failureList;
        // This actionQueue allows asynchronous execution of modeless form and the revit software
        // by making requests that persist even if other requests are posted before that reqeust gets resolved
        List<Request> actionQueue;
        // ActionCondition is something that is must be present in almost every loop of API handler
        Condition actionCondition;
        

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

            // Initialize Back-end Controllers
            this.revitController = new RevitController(commandData);
            this.dataController = new DataController();
            // Initialize Front-end Controllers
            this.selectionController = new ElementSelectionController(
                                            ElementSelectionPanel,
                                            ElementSelectionLabel,
                                            ElementSelectionTreeView);
            OptionVisibilityController optionVisibility = new OptionVisibilityController(
                                            OptionVisibilityPanel,
                                            OptionVisibilityCheckBox);
            OptionFilterController optionFilter = new OptionFilterController(
                                            OptionFilterPanel,
                                            OptionFilterRadioButton0,
                                            OptionFilterRadioButton1,
                                            OptionFilterRadioButton2);
            this.optionController = new OptionController(OptionPanel, optionVisibility, optionFilter);

            this.requestHandler = new RequestHandler(
                                            this.revitController,
                                            this.dataController,
                                            this.selectionController);

            // Initialize the failureList
            failureList = new List<Request>();
            // Initialize the action queue
            actionQueue = new List<Request>();
            // Initialize the action conditions
            actionCondition = new Condition();

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

            // Set up Element Selector / TreeView
            // Request the API handler to update the tree view
            this.actionQueue.Add(Request.UpdateTreeView);

            requestHandler.AddRequest(Request.UpdateTreeView);

            // Set up Options
            List<System.EventHandler> visibilityHandler = new List<System.EventHandler>() { OptionVisibilityCheckBox_CheckedChanged };
            List<System.EventHandler> filterHandler = new List<System.EventHandler>()
            {
                OptionFilterRadioButton0_CheckedChanged,
                OptionFilterRadioButton1_CheckedChanged,
                OptionFilterRadioButton2_CheckedChanged
            };
            this.optionController.Reset(visibilityHandler, filterHandler);

            // Initialize action conditions
            bool hideUnselected = this.optionController.GetVisibilityState();
            actionCondition.hideUnselected = hideUnselected;
            FilterMode filterMode = this.optionController.GetFilterState();
            actionCondition.filterSelection = filterMode;

        }

        #endregion Essential Form Methods

        #region EventHandlers

        #region EventHandlers: ElementSelection

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

            requestHandler.AddRequest(Request.SelectElementIds);

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

            requestHandler.AddRequest(Request.SelectElementIds);

            this.haltIdlingHandler = false;
        }

        #endregion EventHandlers: ElementSelection

        #region EventHandlers: Option

        private void OptionVisibilityCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            actionCondition.hideUnselected = !actionCondition.hideUnselected;
            actionQueue.Add(Request.SelectElementIds);

            requestHandler.AddRequest(Request.SelectElementIds);
        }

        private void OptionFilterRadioButton0_CheckedChanged(object sender, EventArgs e)
        {
            FilterMode filterMode = optionController.GetFilterState();
            actionCondition.filterSelection = filterMode;
            // actionQueue.Add(Request.UpdateTreeView);
        }

        private void OptionFilterRadioButton1_CheckedChanged(object sender, EventArgs e)
        {
            FilterMode filterMode = optionController.GetFilterState();
            actionCondition.filterSelection = filterMode;
            // actionQueue.Add(Request.UpdateTreeView);
        }

        private void OptionFilterRadioButton2_CheckedChanged(object sender, EventArgs e)
        {
            FilterMode filterMode = optionController.GetFilterState();
            actionCondition.filterSelection = filterMode;
            // actionQueue.Add(Request.UpdateTreeView);
        }

        #endregion EventHandlers: Option

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

                    #region STOW

                    // This is to check any time something in the model may have changed or something was selected
                    // ICollection<ElementId> selectedElementIds = uiDoc.Selection.GetElementIds();

                    Request request;

                    revitController.UpdateView();

                    request = requestHandler.GetRequest();

                    request = requestHandler.ProcessRequest(request);

                    switch (request)
                    {
                        case Request.UpdateTreeView:

                            // Update the treeView element within the form
                            this.BeginInvoke(new Action(() =>
                            {
                                selectionController.UpdateTreeView(dataController.AllElements, revitController);
                            }));

                            break;
                        case Request.UpdateTreeViewSelection:

                            ICollection<ElementId> currentSelected = this.uiDoc.Selection.GetElementIds();
                            this.BeginInvoke(new Action(() =>
                            {
                                bool updateSucceeded = selectionController.UpdateSelectedLeaves(currentSelected);
                                if (updateSucceeded)
                                {
                                    requestHandler.FailureListRemove(request);
                                }
                                else
                                {
                                    requestHandler.AttemptRecovery(request);
                                }
                            }));

                            break;
                        case Request.SelectElementIds:

                            revitController.MakeNewSelection(dataController.SelElements);

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

                    #endregion STOW

                    /*
                    if (revitController.View != null)
                    {
                        MessageBox.Show(uiDoc.ActiveView.Id.ToString()
                            + "vs."
                            + revitController.View.Id.ToString());
                    }
                    else
                    {
                        MessageBox.Show("RevitController.View is null!");
                    }
                    */

                    /*
                    List<ElementId> elementIds;
                    Request request;
                    bool attemptRecovery;
                    bool requestSuccessful;

                    if (this.actionQueue.Count != 0)
                    {
                        // Pop the first request out from the queue
                        request = this.actionQueue[0];
                        this.actionQueue.RemoveAt(0);
                        // Attempt the recovery if elementIds == null
                        attemptRecovery = true;
                    }
                    else
                    {
                        // request = Request.Nothing;
                        request = Request.UpdateTreeViewSelection;

                        // Don't attempt the recovery if elementIds == null
                        attemptRecovery = false;
                    }

                    switch (request)
                    {
                        case Request.UpdateTreeView:

                            elementIds = GetDocumentElementIds(actionCondition.filterSelection);
                            requestSuccessful = (elementIds != null);
                            RequestResultHandler(requestSuccessful, request, attemptRecovery);

                            HandleUpdateTreeView(elementIds);

                            break;
                        case Request.UpdateTreeViewSelection:

                            elementIds = selectionController.GetSelectedElementIds();
                            requestSuccessful = (elementIds != null);
                            RequestResultHandler(requestSuccessful, request, attemptRecovery);

                            if (requestSuccessful)
                            {
                                // Get the selected elementIds from the view to compare
                                List<ElementId> currentSelected = this.uiDoc.Selection.GetElementIds().ToList<ElementId>();
                                // Check if the lists are equal or not, if not, then update the treeview's selection
                                if (!selectionController.IsListEqual(elementIds, currentSelected))
                                {
                                    request = Request.UpdateTreeViewSelection;
                                    elementIds = currentSelected;
                                }

                                HandleUpdateTreeViewSelection(elementIds);
                            }

                            break;
                        case Request.SelectElementIds:

                            elementIds = selectionController.GetSelectedElementIds();
                            requestSuccessful = (elementIds != null);
                            RequestResultHandler(requestSuccessful, request, attemptRecovery);

                            // MessageBox.Show("Selecting");
                            HandleSelectElementIds(elementIds);

                            if (actionCondition.hideUnselected)
                            {
                                //MessageBox.Show("Hello");
                                //HandleHideUnselectedElementIds(elementIds);
                            }

                            break;
                        case Request.Nothing:


                            break;
                        case Request.Invalid:
                            break;
                        default:
                            break;
                    }
                    */
                    /*
                    #region STOW

                    // This is to check any time something in the model may have changed or something was selected
                    // ICollection<ElementId> selectedElementIds = uiDoc.Selection.GetElementIds();

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
                                if (elementIds == null)
                                {
                                    if (failureList.Contains(request))
                                    {
                                        // Show the MessageBox to display the error and remove the request
                                        // from the failure list in hopes that it works another time
                                        MessageBox.Show("Fatal Error: Cannot retrieve selected elementIds on retry due to the leaf nodes being changed",
                                            "Debug - selectionController.GetSelectedElementIds()");
                                        failureList.Remove(request);
                                    }
                                    else
                                    {
                                        // Add request into the failure list to prevent infinite loops
                                        failureList.Add(request);
                                        // Insert the same request on the top of the actionQueue to reattempt
                                        actionQueue.Insert(0, request);
                                        request = Request.Nothing;
                                    }
                                }
                                else
                                {
                                    // Remove the request from the failure list if it exists there
                                    if (failureList.Contains(request)) failureList.Remove(request);
                                }

                                break;
                            // This case usually handles Request.Invalid or Request.Nothing
                            default:

                                // When the given Request holds no significance, try to perform polling and
                                // check if the Revit application changed states and values to update Modeless Form
                                elementIds = selectionController.GetSelectedElementIds();
                                if (elementIds == null)
                                {
                                    // Insert the same request on the top of the actionQueue to reattempt
                                    actionQueue.Insert(0, request);
                                    request = Request.Nothing;
                                }
                                else
                                {
                                    // Get the selected elementIds from the view to compare 
                                    List<ElementId> currentSelected = this.uiDoc.Selection.GetElementIds().ToList<ElementId>();
                                    // Check if the lists are equal or not, if not, then update the treeview's selection
                                    if (!selectionController.IsListEqual(elementIds, currentSelected))
                                    {
                                        request = Request.UpdateTreeViewSelection;
                                    }
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
                        if (elementIds == null)
                        {
                            // Insert the same request on the top of the actionQueue to reattempt
                            actionQueue.Insert(0, request);
                            request = Request.Nothing;
                        }
                        else
                        {
                            // Get the selected elementIds from the view to compare 
                            List<ElementId> currentSelected = this.uiDoc.Selection.GetElementIds().ToList<ElementId>();
                            // Check if the lists are equal or not, if not, then update the treeview's selection
                            if (!selectionController.IsListEqual(elementIds, currentSelected))
                            {
                                request = Request.UpdateTreeViewSelection;
                            }
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
                    
                    #endregion STOW
                    */
                }
            }
            catch (Exception ex)
            {
                ErrorReport.Report(ex);
            }
        }

        #region Idling API Handler Methods

        private void HandleHideUnselectedElementIds(List<ElementId> selElementIds)
        {
            // revitController.HideUnselectedElementIds(selElementIds);

            ICollection<ElementId> ids = new FilteredElementCollector(this.doc).OfClass(typeof(FamilyInstance)).ToElementIds();

            List<ElementId> hideIds = new List<ElementId>();
            foreach (var id in ids)
            {
                if (!selElementIds.Contains(id))
                {
                    hideIds.Add(id);
                }
            }

            using (var tran = new Transaction(doc, "Test"))
            {
                tran.Start();

                View3D view = revitController.UiDoc.ActiveView as View3D;
                if (view != null)
                {
                    view.HideElements(hideIds);
                }

                tran.Commit();
            }

            /*
            List<ElementId> docElementIds = GetDocumentElementIds(FilterMode.Project);
            Element element;

            foreach (ElementId eId in docElementIds)
            {
                if (selElementIds.Contains(eId))
                {
                    selElementIds.Remove(eId);
                    continue;
                }

                element = this.doc.GetElement(eId);
                if (element.CanBeHidden())
            }
            */

        }

        private void HandleUpdateTreeView(List<ElementId> elementIds)
        {
            if (elementIds == null) return;

            // Update the treeView element within the form
            this.BeginInvoke(new Action(() =>
            {
                selectionController.UpdateTreeView(elementIds, revitController);
            }));
        }

        private void HandleUpdateTreeViewSelection(List<ElementId> elementIds)
        {
            if (elementIds == null) return;

            ICollection<ElementId> currentSelected = elementIds;
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
        }

        private void HandleSelectElementIds(List<ElementId> elementIds)
        {
            if (elementIds == null) return;

            // Select the list of elementIds with uiDoc (Must be run the in the API idling handler
            this.uiDoc.Selection.SetElementIds(elementIds);
        }

        private List<ElementId> GetDocumentElementIds(FilterMode filter)
        {
            List<ElementId> output;

            // Update / Refresh the revitController's view
            revitController.UpdateView();
            // Get Elements from the newly refreshed view
            output = revitController.GetElementsFromView();
            // If dataController's UpdateAllElements(...) method returns FALSE,
            // Then switch the request from Request.UpdateTreeView to Request.Nothing
            if (dataController.UpdateAllElements(output))
                output = dataController.AllElements;

            return output;
        }

        private void RequestResultHandler(bool success, Request request, bool attemptRecovery = true)
        {

            if (this.failureList.Contains(request))
            {
                // If its not a success...
                if (!success)
                {
                    // Show and report the error and remove the request from the failureList as it has been 'resolved'
                    MessageBox.Show("Fatal Error: Cannot retrieve selected elementIds on retry due to the leaf nodes being changed",
                        "Debug - selectionController.GetSelectedElementIds()");
                }
                this.failureList.Remove(request);
            }
            else
            {
                // If its not a success and the method is allowed to attempt recovery of the request
                if ((!success) && attemptRecovery)
                {
                    // Record that this requested has failed before and put the request back on top to reattempt it
                    this.failureList.Add(request);
                    actionQueue.Insert(0, request);
                }
            }

            return;
        }

        #endregion Idling API Handler Methods

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
