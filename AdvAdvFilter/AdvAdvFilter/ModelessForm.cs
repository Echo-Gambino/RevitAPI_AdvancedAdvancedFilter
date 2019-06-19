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
    using Autodesk.Revit.DB.Events;
    using Autodesk.Revit.UI.Events;

    // using Color = System.Drawing.Color;
    using Point = System.Drawing.Point;
    using Panel = System.Windows.Forms.Panel;
    using FilterMode = AdvAdvFilter.Common.FilterMode;
    using Request = AdvAdvFilter.Common.Request;

    public partial class ModelessForm : System.Windows.Forms.Form
    {
        DebugController debug;

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
        private bool firstStartup = true;

        #endregion Fields: Modeless Form

        #region Fields: Controllers

        // Controls the inner workings of the form and revit 
        DataController dataController;
        RevitController revitController;
        RequestHandler requestHandler;
        // Controls elements visible in modeless form
        ElementSelectionController selectionController;
        OptionController optionController;
        ActionController actionController;
        FilterController filterController;

        #endregion Fields: Controllers

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

            debug = new DebugController(TestPanel, listBox1, listBox2);

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
            OptionHideNodeController optionHideNode = new OptionHideNodeController(
                                            OptionHideNodePanel,
                                            OptionHideNodeCheckedListBox);

            this.optionController = new OptionController(OptionPanel, optionVisibility, optionFilter, optionHideNode);

            ActionAxisController xAxis = new ActionAxisController(
                ActionShiftPanel0, ActionShiftLabel0, ActionShiftTextBox0);

            ActionAxisController yAxis = new ActionAxisController(
                ActionShiftPanel1, ActionShiftLabel1, ActionShiftTextBox1);

            ActionAxisController zAxis = new ActionAxisController(
                ActionShiftPanel2, ActionShiftLabel2, ActionShiftTextBox2);

            ActionModeController mode = new ActionModeController(
                ActionModePanel, ActionModeRadioButton0, ActionModeRadioButton1);

            List<ActionAxisController> xyz = new List<ActionAxisController>() { xAxis, yAxis, zAxis };

            this.actionController = new ActionController(
                ActionPanel, ActionResetButton, ActionShiftButton, ActionPromptLabel, xyz, mode);

            // Initialize Back-end Controllers
            this.revitController = new RevitController(commandData);
            this.dataController = new DataController(this.doc);
            this.requestHandler = new RequestHandler();
            this.filterController = new FilterController(this.revitController);

            // Get elementList, not sure what to use it for
            elementList = elementList.Where(e => null != e.Category && e.Category.HasMaterialQuantities).ToList();

            // Execute method ModelessForm_FormClosed when the form is closing
            this.FormClosing += this.ModelessForm_FormClosed;

        }

        private void ModelessForm_FormClosed(object sender, FormClosingEventArgs e)
        {
            this.haltIdlingHandler = true;
            e.Cancel = true;
            this.ExecuteWithAPIContext(this.APIClose, null);
        }

        private void ModelessForm_Load(object sender, EventArgs e)
        {
            // Stop IdlingHandler from executing during initialization
            this.haltIdlingHandler = true;

            this.filterController.ClearAllFilters();

            this.requestHandler.ResetAll();

            this.requestHandler.AddRequest(Request.UpdateTreeView);

            this.requestHandler.AddRequest(Request.UpdateTreeViewSelection);

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
            if (hideUnselected)
            {
                requestHandler.HideUnselected(true);
            }
            else
            {
                requestHandler.ShowAll(true);
            }

            FilterMode filterMode = this.optionController.GetFilterState();
            switch (filterMode)
            {
                case FilterMode.Project:
                    requestHandler.FilterByProject();
                    break;
                case FilterMode.View:
                    requestHandler.FilterByView();
                    break;
                case FilterMode.Selection:
                    requestHandler.FilterBySelection();
                    break;
                default:
                    throw new InvalidEnumArgumentException("Error: filterMode is not Project, View, or Selection in this.optionController.GetFilterState()");
            }

            // Begin first start up loop (where TreeStructure gets all its elements from the document)
            this.firstStartup = true;

            // Set up actionController
            this.actionController.Reset();

            // Resume IdlingHandler
            this.haltIdlingHandler = false;
        }

        #endregion Essential Form Methods

        #region EventHandlers

        #region EventHandlers: ElementSelection

        private void ElementSelectionTreeView_BeforeCollapse(object sender, TreeViewCancelEventArgs e)
        {
        }

        private void ESExpandAllButton_Click(object sender, EventArgs e)
        {
            selectionController.ExpandAll();
        }

        private void ESCollapseAllButton_Click(object sender, EventArgs e)
        {
            selectionController.CollapseAll();
        }

        private void ESExpandSelectedButton_Click(object sender, EventArgs e)
        {
            selectionController.ExpandSelected();
        }

        private void ESCollapseSelectedButton_Click(object sender, EventArgs e)
        {
            selectionController.CollapseSelected();
        }

        private void ElementSelectionTreeView_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (e.Action == TreeViewAction.Unknown)
                return;

            this.haltIdlingHandler = true;

            // Step 1: Get node's full path
            // string fullPath = e.Node.FullPath;
            // Step 2: Tokenize the path by seperating out the '\'s
            // List<string> pathTokens = fullPath.Split('\\').ToList();
            List<string> pathTokens = selectionController.GetNamePath(e.Node);
            // Step 3: Get all elements that has that same path
            HashSet<ElementId> elementIds = dataController.GetElementIdsByPath(pathTokens);
            elementIds.IntersectWith(dataController.AllElements);
            // Step 4: Apply the change to the nodes with the corresponding elementIds
            selectionController.UpdateSelectionByElementId(elementIds, e.Node.Checked);
            //// Step 5: Update the counter to the tree itself
            //selectionController.UpdateSelectionCounter(dataController.ElementTree);

            selectionController.UpdateAffectedCounter(elementIds, dataController);
            // selectionController.UpdateNodeCounter(e.Node, dataController);

            if (e.Node.Checked)
            {
                // Step 5a: Add elementIds onto curSelection
                dataController.SelElementIds.UnionWith(elementIds);
            }
            else
            {
                // Step 5b: Remove elementIds from curSelection
                dataController.SelElementIds.ExceptWith(elementIds);
            }

            requestHandler.AddRequest(Request.SelectElementIds);

            this.haltIdlingHandler = false;
        }

        #endregion EventHandlers: ElementSelection

        #region EventHandlers: Option

        private void OptionVisibilityCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            this.haltIdlingHandler = true;

            if (OptionVisibilityCheckBox.Checked)
            {
                HashSet<ElementId> idsToHide = new HashSet<ElementId>(dataController.AllElements.Except(dataController.SelElementIds));
                dataController.IdsToHide = idsToHide;

                // requestHandler.ImmediateRequest(Request.ChangeElementVisibility);
                requestHandler.HideUnselected( true );
            }
            else
            {
                dataController.IdsToHide.Clear();

                // requestHandler.ImmediateRequest(Request.ChangeElementVisibility);
                requestHandler.ShowAll( true );
            }

            this.haltIdlingHandler = false;
        }

        private void OptionFilterRadioButton0_CheckedChanged(object sender, EventArgs e)
        {
            this.haltIdlingHandler = true;

            FilterMode filterMode = optionController.GetFilterState();
            OptionFilterRadioButton_ChangeFilter(filterMode);

            this.haltIdlingHandler = false;
        }

        private void OptionFilterRadioButton1_CheckedChanged(object sender, EventArgs e)
        {
            this.haltIdlingHandler = true;

            FilterMode filterMode = optionController.GetFilterState();
            OptionFilterRadioButton_ChangeFilter(filterMode);

            this.haltIdlingHandler = false;
        }

        private void OptionFilterRadioButton2_CheckedChanged(object sender, EventArgs e)
        {
            this.haltIdlingHandler = true;

            FilterMode filterMode = optionController.GetFilterState();
            OptionFilterRadioButton_ChangeFilter(filterMode);

            this.haltIdlingHandler = false;
        }

        private void OptionFilterRadioButton_ChangeFilter(FilterMode filterMode)
        {
            if (requestHandler.FilterBy == filterMode)
                return;

            switch (filterMode)
            {
                case FilterMode.Project:
                    requestHandler.FilterByProject();
                    break;
                case FilterMode.View:
                    requestHandler.FilterByView();
                    break;
                case FilterMode.Selection:
                    requestHandler.FilterBySelection();
                    break;
                default:
                    throw new InvalidEnumArgumentException("Error: filterMode is not Project, View, or Selection in this.optionController.GetFilterState()");
            }
        }

        private void OptionHideNodeCheckedListBox_SelectedIndexChanged(object sender, ItemCheckEventArgs e)
        {
            this.haltIdlingHandler = true;

            optionController.ToggleCheck(e.Index);

            HashSet<string> hiddenNodes = new HashSet<string>(optionController.HiddenNodes);

            filterController.SetFieldBlackList(hiddenNodes, Common.Depth.CategoryType);

            requestHandler.AddRequest(Request.UpdateTreeView);

            this.haltIdlingHandler = false;
        }

        #endregion EventHandlers: Option

        #region EventHandlers: Action

        private void ActionShiftButton_Click(object sender, EventArgs e)
        {
            this.haltIdlingHandler = true;

            // Attempt to parse from actionController
            bool success = this.actionController.TryGetAllInputs(
                out List<int> coords, out bool copyAndShift);

            // If its a success...
            if (success)
            {
                // Disable the error prompt for actionController
                this.actionController.DisablePrompt();
                // Get additonal data needed for dataController
                HashSet<ElementId> selected = new HashSet<ElementId>(dataController.SelElementIds);
                // Set data into the dataController
                dataController.IdsToMove = selected;
                dataController.Coords = coords;
                dataController.CopyAndShift = copyAndShift;
                // Request to shift the elements
                requestHandler.AddRequest(Request.ShiftElements);
            }
            else
            {
                // Enable 'not valid' error prompts
                this.actionController.PromptNotValidShifts();
            }

            this.haltIdlingHandler = false;
        }

        private void ActionResetButton_Click(object sender, EventArgs e)
        {
            this.actionController.Reset();
        }

        private void ActionShiftTextBox0_TextChanged(object sender, EventArgs e)
        {
            ActonShiftTextBox_HandleTextChanged(ActionShiftTextBox0);
        }

        private void ActionShiftTextBox1_TextChanged(object sender, EventArgs e)
        {
            ActonShiftTextBox_HandleTextChanged(ActionShiftTextBox1);
        }

        private void ActionShiftTextBox2_TextChanged(object sender, EventArgs e)
        {
            ActonShiftTextBox_HandleTextChanged(ActionShiftTextBox2);
        }

        private void ActonShiftTextBox_HandleTextChanged(System.Windows.Forms.TextBox textBox)
        {
            bool parseSuccessful;
            string text = textBox.Text;

            parseSuccessful = Int32.TryParse(text, out int value);

            if (parseSuccessful)
                textBox.ForeColor = System.Drawing.Color.Black;
            else
                textBox.ForeColor = System.Drawing.Color.Red;

            if (this.actionController.IsAllAxisEmpty()
                && this.actionController.IsModeDefault())
            {
                this.actionController.DisableShift();
                this.actionController.DisableDefaults();
            }
            else
            {
                this.actionController.EnableDefaults();

                if (actionController.TryGetAllInputs(out List<int> coords, out bool shift))
                {
                    this.actionController.EnableShift();
                }
                else
                {
                    this.actionController.DisableShift();
                }
            }
        }

        #endregion EventHandlers: Action

        #endregion EventHandlers

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
        public void UIAppEvent_IdlingEventHandler(object sender, IdlingEventArgs args)
        {
            try
            {
                args.SetRaiseWithoutDelay();

                // If a thread has halted the idling handler, this if statement will
                // make sure that the process does not pass through any further.
                if (this.haltIdlingHandler) return;

                // Initializer for everything the form needs within the Revit API context
                FirstStartup_IdlingHandler();

                Request request = GetNextRequest_IdlingHandler();

                HandleRequest_IdlingHandler(request);

                #region Stow
                /*
                if (!this.haltIdlingHandler)
                {                    
                    // This is to get the essential objects to use the Revit API
                    UIApplication uiApp = sender as UIApplication;
                    UIDocument uiDoc = uiApp.ActiveUIDocument;
                    Document activeDoc = uiDoc.Document;

                    if (this.firstStartup)
                    {
                        this.firstStartup = false;

                        List<ElementId> AllElementIds = revitController.GetAllElementIds(FilterMode.Project);                        
                        dataController.SetAllElements(AllElementIds);
                    }

                    Request request;
                    FilterMode filter;

                    filter = requestHandler.FilterBy;

                    int viewChanged = revitController.UpdateView();
                    if (viewChanged == 1)
                    {
                        request = Request.UpdateTreeView;
                    }
                    else
                    {
                        request = requestHandler.GetRequest();
                    }

                    request = requestHandler.ProcessRequest(request);

                    switch (request)
                    {
                        case Request.UpdateTreeView:

                            dataController.SetMode(filter);

                            // Update the treeView element within the form
                            this.BeginInvoke(new Action(() =>
                            {
                                selectionController.UpdateTreeViewStructure(dataController.AllElements, dataController.ElementTree);
                                // selectionController.UpdateTreeViewStructure_New(dataController.ElementTree.SubSet, dataController.ElementTree);
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

                            //List<ElementId> elementIds
                            //    = revitController.GetAllElementIds(filter);
                            //dataController.UpdateAllElements(elementIds);

                            dataController.SetMode(filter);

                            // Hide all elements 
                            if (requestHandler.UnselectedHidden)
                            {
                                revitController.HideUnselectedElementIds(
                                    dataController.SelElements,
                                    dataController.AllElements);
                            }
                            else
                            {
                                revitController.ShowSelectedElementIds(dataController.SelElements);
                            }

                            break;
                        case Request.ShiftSelected:

                            List<ElementId> movElementIds = dataController.MovElements;
                            List<int> coords = dataController.Coords;
                            bool copyAndShift = dataController.CopyAndShift;

                            // Copy and Move the Elements
                            revitController.CopyAndMoveElements(movElementIds, coords, copyAndShift);

                            break;
                        case Request.Nothing:
                            // Do absolutely nothing
                            break;
                        default:

                            // If the request isn't any other request (even Nothing), then prompt user with warning message
                            TaskDialog.Show("Debug - SelectionChanged_UIAppEvent_WhileIdling",
                                "Warning: Handler given invalid request");

                            break;
                    }
                }
                */
                #endregion STOW
            }
            catch (Exception ex)
            {
                ErrorReport.Report(ex);
            }
        }

        #region Idling API Handler Methods

        /// <summary>
        /// Initializes variables to upoun startup the idling handler, executes only one time per run
        /// </summary>
        private void FirstStartup_IdlingHandler()
        {
            // Uses revitController! Only use it within API context!

            // If this isn't the first startup, then exit out immediately
            if (!this.firstStartup) return;
            // Disable first startup
            this.firstStartup = false;

            // Get all elementIds and cache it into dataController
            List<ElementId> allElementIds = revitController.GetAllElementIds(FilterMode.Project);

            this.dataController.SetAllElements(allElementIds);

            this.dataController.SetMode(requestHandler.FilterBy);

            this.selectionController.NodesToAdd = dataController.AllElements.ToList();
            this.selectionController.NodesToDel = dataController.AllElements.ToList();
        }

        /// <summary>
        /// Gets the next request from the requestHandler, unless its overrided by an update in treeView
        /// </summary>
        /// <returns></returns>
        private Request GetNextRequest_IdlingHandler()
        {
            Request request;

            int viewChanged = revitController.UpdateView();

            if (viewChanged == 1)
            {
                request = Request.UpdateTreeView;
            }
            else
            {
                request = requestHandler.GetRequest();
            }

            return request;
        }

        /// <summary>
        /// Returns true if the HashSet of revit's current selection is different from dataController's current selection
        /// </summary>
        /// <returns></returns>
        private bool RevitSelectionChanged()
        {
            HashSet<ElementId> newSelection = new HashSet<ElementId>(revitController.GetElementIdsFromSelection());
            // newSelection.IntersectWith(dataController.AllElements);
            newSelection.IntersectWith(selectionController.AllElementIds);
            return dataController.DidSelectionChange(newSelection);
        }

        /// <summary>
        /// Handles the form's request to execute instructions within a revit API context
        /// </summary>
        private void HandleRequest_IdlingHandler(Request request)
        {
            FilterMode filter = requestHandler.FilterBy;

            switch (request)
            {
                case Request.UpdateTreeView:

                    // debug.printText(dataController.SelElementIds, "SelElementIds Before", 1);

                    // Step 1: Update all viewable elements for the current view
                    bool changed = dataController.SetMode(filter);

                    changed = true;
                    // Step 1.1: Exit if dataController doesn't detect a change in viewable elements
                    if (!changed) return;

                    // Get the new elements by filtering all the elements reported by dataController
                    HashSet<ElementId> newElementIds = filterController.Filter(dataController.AllElements);
                    // Get the old elements obtaining all the elements reported by selectionController
                    HashSet<ElementId> oldElementIds = selectionController.AllElementIds;

                    // Make a LINQ statement for addList
                    var addList
                        = from ElementId id in newElementIds
                          where (!oldElementIds.Contains(id))
                          select id;
                    // Make a LINQ statement for delList
                    var delList
                        = from ElementId id in oldElementIds
                          where (!newElementIds.Contains(id))
                          select id;

                    // Set nodes to add and nodes to delete
                    selectionController.NodesToAdd = addList.ToList();
                    selectionController.NodesToDel = delList.ToList();

                    // Step 2: Update the treeView element outside of the API context
                    this.BeginInvoke(new Action(() =>
                    {
                        // Commit the changes to the TreeView
                        selectionController.CommitTree(dataController.ElementTree);
                        // selectionController.CommitTree(dataController.ElementTree, newElementIds);
                        // Refresh all node counters since the TreeView's nodes are mostly going to be changed
                        selectionController.RefreshAllNodeCounters(dataController);
                        // Update the selection counter label
                        selectionController.UpdateSelectionCounter();

                        // Update the HideNodeList
                        optionController.UpdateHideNodeList(dataController.ElementTree.SetTree);
                    }));

                    // Step 3: Update visibility state of the view
                    Autodesk.Revit.DB.View view = dataController.View;
                    HashSet<ElementId> hidden = new HashSet<ElementId>();
                    if (optionController.GetVisibilityState())
                    {
                        // Step 3.1a: Get hidden elements by the formula: hidden = AllElements - SelElements
                        hidden = new HashSet<ElementId>(dataController.AllElements.Except(dataController.SelElementIds));
                    }
                    else
                    {
                        // Step 3.1b: To show all elements, we need to remove the previously hidden ids from the records (if there is any)
                        if (dataController.ElementTree.HiddenNodes.ContainsKey(view.Id))
                        {
                            dataController.ElementTree.HiddenNodes.Remove(view.Id);
                        }
                    }

                    // Step 4: Set dataController's ids to hide with hidden
                    dataController.IdsToHide = hidden;

                    // Step 5: Request ChangeElementVisibility
                    requestHandler.AddRequest(Request.ChangeElementVisibility);

                    requestHandler.AddRequest(Request.UpdateTreeViewSelection);

                    break;

                case Request.UpdateTreeViewSelection:
                    // Step 1: Update dataController's select element list
                    HashSet<ElementId> newSelection = new HashSet<ElementId>(revitController.GetElementIdsFromSelection());
                    // newSelection.IntersectWith(dataController.AllElements);
                    newSelection.IntersectWith(selectionController.AllElementIds);
                    HashSet<ElementId> oldSelection = dataController.SelElementIds;
                    oldSelection.IntersectWith(selectionController.AllElementIds);
                    // Step 2: Construct add and remove hashsets
                    HashSet<ElementId> addSelection = new HashSet<ElementId>(newSelection.Except(oldSelection));
                    HashSet<ElementId> remSelection = new HashSet<ElementId>(oldSelection.Except(newSelection));

                    // DEBUG: Print out addSelection
                    //StringBuilder sb = new StringBuilder();
                    //sb.AppendLine("RemSelection");
                    //foreach (ElementId id in remSelection)
                    //{
                    //    sb.AppendLine(id.ToString());
                    //}
                    //MessageBox.Show(sb.ToString());

                    // Branch nodes to update
                    

                    // Step 3: Update selection by 'adding' and 'removing' selected treeNodes by checking and unchecking them
                    this.BeginInvoke(new Action(() =>
                    {
                        selectionController.UpdateSelectionByElementId(addSelection, true);
                        selectionController.UpdateSelectionByElementId(remSelection, false);
                        // selectionController.RefreshAllNodeCounters(dataController);
                        selectionController.UpdateAffectedCounter(addSelection.Union(remSelection), dataController);
                        selectionController.UpdateSelectionCounter();
                    }));
                    // Step 4: Update dataController efficiently
                    // Mathematical visualization: SelElements = (currentSelection ^ revitSelection) U additionalSelection
                    dataController.SelElementIds.IntersectWith(newSelection);
                    dataController.SelElementIds.UnionWith(addSelection);

                    // Step 3: (OPTIONAL) Hide the remaining elementIds
                    if (optionController.GetVisibilityState())
                    {
                        // Step 3.1: Get selected and all elemnts from dataController
                        HashSet<ElementId> sel = dataController.SelElementIds;
                        HashSet<ElementId> all = dataController.AllElements;
                        // Step 3.2: Get the elementIds that are supposed to be hidden by (hid = all - sel)
                        HashSet<ElementId> hid = new HashSet<ElementId>(all.Except(sel));
                        // Step 3.3: Set dataController's requested ids to hide (NOT ACTUAL HIDDEN ELEMENTIDS) to hid
                        dataController.IdsToHide = hid;
                        // Step 3.4: Immediately override the next request by Requesting ChangeElementVisibility
                        requestHandler.ImmediateRequest(Request.ChangeElementVisibility);
                    }
                    break;

                case Request.SelectElementIds:
                    // Step 1: Make a new selection in Revit
                    revitController.MakeNewSelection(dataController.SelElementIds.ToList());
                    // Step 2: Update the selection's counter
                    this.BeginInvoke(new Action(() =>
                    {
                        // selectionController.RefreshAllNodeCounters(dataController);
                        selectionController.UpdateSelectionCounter();
                    }));

                    // Step 2: (OPTIONAL) Hide the remaining elementIds
                    if (optionController.GetVisibilityState())
                    {
                        // Step 2.1: Get selected and all elemnts from dataController
                        HashSet<ElementId> sel = dataController.SelElementIds;
                        HashSet<ElementId> all = dataController.AllElements;
                        // Step 2.2: Get the elementIds that are supposed to be hidden by (hid = all - sel)
                        HashSet<ElementId> hid = new HashSet<ElementId>(all.Except(sel));
                        // Step 2.3: Set dataController's requested ids to hide (NOT ACTUAL HIDDEN ELEMENTIDS) to hid
                        dataController.IdsToHide = hid;
                        // Step 2.4: Immediately override the next request by Requesting ChangeElementVisibility
                        requestHandler.ImmediateRequest(Request.ChangeElementVisibility);
                    }
                    break;

                case Request.ChangeElementVisibility:

                    // Step 1: Get the previously hidden ids
                    HashSet<ElementId> prevHidden = new HashSet<ElementId>();
                    Autodesk.Revit.DB.View v = dataController.View;
                    if (v != null)
                    {
                        if (dataController.ElementTree.HiddenNodes.ContainsKey(v.Id))
                        {
                            prevHidden.UnionWith(dataController.ElementTree.HiddenNodes[v.Id]);
                        }
                    }

                    // Step 2: Get ids to Hide and Show
                    // idsToHide = IdsToHide
                    HashSet<ElementId> idsToHide = dataController.IdsToHide;
                    // idsToShow = AllElements - prevHidden
                    HashSet<ElementId> idsToShow = new HashSet<ElementId>(dataController.AllElements.Union(prevHidden));

                    // Step 3: idsToShow = idsToShow - idsToHide
                    idsToShow.ExceptWith(idsToHide);

                    // Step 4: Hide elements if there is any
                    if (idsToHide.Count != 0)
                    {
                        revitController.HideElementIds(idsToHide, dataController.ElementTree);
                    }

                    // Step 5: Show elements if there is any
                    if (idsToShow.Count != 0)
                    {
                        revitController.ShowElementIds(idsToShow, dataController.ElementTree);
                    }

                    break;

                case Request.ShiftElements:
                    // Step 1: Get all the needed information within dataController
                    List<ElementId> idsToMove = revitController.GetMovableElementIds(dataController.IdsToMove.ToList());
                    List<int> coords = dataController.Coords;
                    bool copyAndShift = dataController.CopyAndShift;

                    if (idsToMove.Count == 0)
                        this.actionController.PromptNotValidElements();

                    // Step 2: Copy and move the elements via the Revit Controller using the given information and get back the affected elementIds
                    // revitController.CopyAndMoveElements(idsToMove, coords, copyAndShift);
                    List<ElementId> elementsAffected = revitController.CopyAndShiftElements(idsToMove, coords, copyAndShift);

                    if (copyAndShift)
                    {
                        this.dataController.AddToAllElements(elementsAffected);

                        this.selectionController.NodesToAdd = elementsAffected;
                        requestHandler.ImmediateRequest(Request.UpdateTreeView);
                    }

                    break;

                case Request.Nothing:
                    if (RevitSelectionChanged())
                    {
                        requestHandler.AddRequest(Request.UpdateTreeViewSelection);
                    }

                    break;
                default:
                    break;
            }
        }

        #endregion Idling API Handler Methods

        public void AppEvent_DocChangedEventHandler(object sender, DocumentChangedEventArgs args)
        {
            this.uiApp.Idling -= Main.ActiveModelessForm.UIAppEvent_IdlingEventHandler;
            this.haltIdlingHandler = true;
            
            // Retrieve the addedElement Ids and deletedElement Ids
            ICollection<ElementId> addedElements =  args.GetAddedElementIds();
            ICollection<ElementId> deletedElements = args.GetDeletedElementIds();

            // Update the dataController's cached elements
            this.dataController.AddToAllElements(addedElements.ToList());
            this.dataController.RemoveFromAllElements(deletedElements.ToList());

            this.dataController.SetMode(requestHandler.FilterBy, true);

            // Put up a request to update the TreeView
            requestHandler.AddRequest(Request.UpdateTreeView);

            this.haltIdlingHandler = false;
            this.uiApp.Idling += Main.ActiveModelessForm.UIAppEvent_IdlingEventHandler;

            return;
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
                Main.UiCtrlApp.Idling -= Main.ActiveModelessForm.UIAppEvent_IdlingEventHandler;

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

        /*
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
        */
        /*
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
        */
        #endregion

        #endregion

    }
}
