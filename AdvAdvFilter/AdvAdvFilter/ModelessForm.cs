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

        #region Modeless Form

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

        #endregion

        // private List<ElementId> selElements;
        // private List<ElementId> allElements;
        // Almost useless, should delete later
        private Autodesk.Revit.DB.View lastValidView;

        #region Controllers

        DataController dataController;
        RevitController revitController;
        ElementSelectionController selectionController;

        #endregion Controllers

        #region API Handler Requests

        // Make an enum for all the possible requests
        enum Request
        {
            AllElementIds = 0,
            UpdateTreeView = 1,
            SelectElementIds = 2,

            Invalid = -1
        }

        #endregion API Handler Requests

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
            // selectionTool(...);
            
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

            // Initialize this.lastValidView = null
            this.lastValidView = null;

            // Initialize controllers
            this.selectionController = new ElementSelectionController(ElementSelectionPanel, ElementSelectionLabel, ElementSelectionTreeView);
            this.revitController = new RevitController(commandData);
            this.dataController = new DataController();

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
            // Update all elements that is within this document
            revitController.UpdateView();
            List<ElementId> newElements = revitController.GetElementsFromView();
            // This is to determine if we need to update the TreeView or not
            bool updateTreeView = false;
            updateTreeView = dataController.UpdateAllElements(newElements);

            if (updateTreeView)
                selectionController.UpdateTreeView(dataController.AllElements, revitController);

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

            selectionController.UpdateAfterCheck(e.Node as AdvTreeNode);

            selectionController.UpdateTotalSelectedItemsLabel();

            List<ElementId> selectedElementIds = selectionController.GetSelectedElementIds();

            // revitController.MakeNewSelection(selectedElementIds);
        }

        private void ElementSelectionTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Action == TreeViewAction.Unknown)
                return;

            selectionController.UpdateAfterCheck(e.Node as AdvTreeNode);

            selectionController.UpdateTotalSelectedItemsLabel();

            List<ElementId> selectedElementIds = selectionController.GetSelectedElementIds();

            // revitController.MakeNewSelection(selectedElementIds);
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

                    // This block is to update all the elements that is currently available within the document
                    revitController.UpdateView();
                    List<ElementId> newElements = revitController.GetElementsFromView();
                    bool updateTreeView = false;
                    updateTreeView = dataController.UpdateAllElements(newElements);

                    List<ElementId> elementIds = dataController.AllElements;

                    // Temp Test

                    // List<CategoryType> categoryTypes = revitController.GetAllCategoryTypes();
                    StringBuilder sb2 = new StringBuilder();

                    void printDict(SortedDictionary<string, List<ElementId>> dict, StringBuilder sbf)
                    {
                        foreach (KeyValuePair<string, List<ElementId>> kvp in dict)
                        {
                            sbf.Append(String.Format("> {0}\n", kvp.Key));
                            foreach (ElementId i in kvp.Value)
                            {
                                sbf.Append(String.Format("\t\t+ {0}\n", i.ToString()));
                            }
                        }
                        sbf.Append("\n");
                    }

                    SortedDictionary<string, List<ElementId>> dGroup = null;

                    dGroup = revitController.GroupElementIdsBy(
                                        "CategoryType".GetType(),
                                        dataController.AllElements);
                    printDict(dGroup, sb2);

                    dGroup = revitController.GroupElementIdsBy(
                                        typeof(Category),
                                        dataController.AllElements);
                    printDict(dGroup, sb2);

                    dGroup = revitController.GroupElementIdsBy(
                                        typeof(Family),
                                        dataController.AllElements);
                    printDict(dGroup, sb2);
                    dGroup = revitController.GroupElementIdsBy(
                                        typeof(ElementType),
                                        dataController.AllElements);
                    printDict(dGroup, sb2);



                    foreach (ElementId elementId in selectedElementIds)
                    {
                        Element e = revitController.GetElement(elementId);
                        sb2.Append(String.Format("Element : {0}\n{1}\n", e.Name, elementId));
                    }

                    

                    // Temp Test

                    #region TEST SECTION

                    StringBuilder sb1 = new StringBuilder();
                    sb1.Append("Elements Categories\n");
                    if (dataController.AllElements != null)
                    {
                        foreach (ElementId eId in dataController.AllElements)
                        {
                            Element e = revitController.GetElement(eId);
                            Category c = revitController.GetCategory(e);
                            
                            string t = "null";

                            if (c != null)
                            {
                                sb1.Append(eId.ToString() + "  "
                                    + e.Name + "  "
                                    + c.CategoryType.ToString() + "\n");
                            }
                            else
                            {
                                if (e != null)
                                    t = e.Name;

                                sb1.Append(eId.ToString() + "  "
                                    + t + "  "
                                    + "Null\n");
                            }
                        }
                    }

                    foreach (ElementId elementId in selectedElementIds)
                    {
                        Element e = revitController.GetElement(elementId);
                        sb1.Append(String.Format("Element : {0}\n{1}\n", e.Name, elementId));
                    }


                    Document document = null;
                    document = doc;

                    StringBuilder sb = new StringBuilder();

                    FilteredElementCollector test = null;
                    test = new FilteredElementCollector(document);
                    // FilteredElementCollector test = new FilteredElementCollector(activeDoc);
                    int count = test.GetElementCount();
                    sb.Append(string.Format("Element Count: {0}\n", count));

                    sb.Append(string.Format("this.uiDoc is {0}\n", this.uiDoc));
                    sb.Append(string.Format("this.doc is {0}\n", this.doc));

                    sb.Append(string.Format("uiDoc.view is {0}\n", this.uiDoc.ActiveView));
                    sb.Append(string.Format("doc.view is {0}\n", this.doc.ActiveView));

                    sb.Append(string.Format("uiDoc.view.name is {0}\n", this.uiDoc.ActiveView.Name));
                    sb.Append(string.Format("doc.view.name is {0}\n", this.doc.ActiveView.Name));

                    sb.Append(string.Format("uiDoc.view.type is {0}\n", this.uiDoc.ActiveView.ViewType));
                    sb.Append(string.Format("doc.view.type is {0}\n", this.doc.ActiveView.ViewType));

                    // ViewType.FloorPlan
                    Autodesk.Revit.DB.View activeView = this.doc.ActiveView;
                    if (activeView.ViewType == ViewType.FloorPlan)
                    {
                        this.lastValidView = activeView;
                    }

                    if (this.lastValidView != null)
                    {
                        FilteredElementCollector test2 = new FilteredElementCollector(document, this.lastValidView.Id);
                        List<Element> elements = test2.ToElements().ToList<Element>();
                        foreach (Element e in elements)
                        {
                            sb.Append(string.Format("Id: {0}\n", e.Id));
                        }
                    }

                    
                    sb.Append("\n*For Reference*\n");

                    foreach (ElementId elementId in selectedElementIds)
                    {
                        sb.Append(String.Format("Element : {0}\n", elementId));
                    }


                    #endregion TEST SECTION

                    // Update the TreeView
                    if (updateTreeView)
                    {
                        this.BeginInvoke(new Action(() =>
                        {
                            // ElementSelectionLabel.Text += "Thing";
                            selectionController.UpdateTreeView(dataController.AllElements, revitController);
                        }));
                    }

                    // Check if the selection set is changed (treeView selection != Revit Selection)
                    bool updateSelection = (selectedElementIds.Count >= 1);
                    // updateSelection = selectionController.UpdateSelectedNodes(selectedElementIds);

                    // Update the selectedElementIds
                    // if (selectedElementIds.Count >= 1)
                    if (updateSelection)
                    {
                        this.BeginInvoke(new Action(() =>
                        {
                            // This is for testing purposes
                            TestLabel.Text = sb2.ToString();



                        }));
                    }

                    
                    if (selectedElementIds.Count == 0)
                    {
                        selectedElementIds = selectionController.GetSelectedElementIds();
                        if (selectedElementIds.Count != 0)
                        {
                            uiDoc.Selection.SetElementIds(selectedElementIds);
                        }
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
