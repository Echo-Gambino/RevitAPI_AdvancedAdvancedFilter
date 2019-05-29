namespace AdvAdvFilter
{

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using System.Windows.Forms;

    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;
    using Autodesk.Revit.UI.Events;

    class RequestHandler
    {
        #region Data Types
        // Make an enum for all the possible requests
        public enum Request
        {
            Nothing = 0,
            UpdateTreeView = 1,
            UpdateTreeViewSelection = 2,
            SelectElementIds = 3,
            Invalid = -1
        }

        public enum FilterMode
        {
            Selection = 0,
            View = 1,
            Project = 2,
            Invalid = -1
        }

        struct Condition
        {
            public bool hideUnselected;
            public FilterMode filter;
        };

        #endregion Data Types

        #region Fields

        // Records requests that have previously failed so that the API handler will abort the action
        // when the handler tries to resolve the request a second time.
        private List<Request> failureList;
        // This actionQueue allows asynchronous execution of modeless form and the revit software
        // by making requests that persist even if other requests are posted before that reqeust gets resolved
        private List<Request> actionQueue;
        // ActionCondition is something that is must be present in almost every loop of API handler
        private Condition actionCondition;

        // Use these objects to get information
        RevitController revitController;
        DataController dataController;
        ElementSelectionController selectionController;

        #endregion Fields

        #region Parameters

        public bool UnselectedHidden
        {
            get { return actionCondition.hideUnselected; }
        }

        public FilterMode FilterBy
        {
            get { return actionCondition.filter; }
        }

        #endregion Parameters

        public RequestHandler(
            RevitController revitController,
            DataController dataController,
            ElementSelectionController selectionController
            )
        {
            // Initialize variables
            this.actionQueue = new List<Request>();
            this.failureList = new List<Request>();
            this.actionCondition = new Condition();

            // Set values to the variables
            this.revitController = revitController;
            this.dataController = dataController;
            this.selectionController = selectionController;

        }

        public void ResetAll()
        {
            ResetRequest();
            ResetFailureList();
        }

        #region Set conditions

        public void HideUnselected()
        {
            this.actionCondition.hideUnselected = true;
            this.actionQueue.Add(Request.SelectElementIds);
        }

        public void ShowAll()
        {
            this.actionCondition.hideUnselected = false;
            this.actionQueue.Add(Request.SelectElementIds);
        }

        public void FilterBySelection()
        {
            this.actionCondition.filter = FilterMode.Selection;
            this.actionQueue.Add(Request.UpdateTreeView);
        }

        public void FilterByView()
        {
            this.actionCondition.filter = FilterMode.View;
            this.actionQueue.Add(Request.UpdateTreeView);
        }

        public void FilterByProject()
        {
            this.actionCondition.filter = FilterMode.Project;
            this.actionQueue.Add(Request.UpdateTreeView);
        }

        #endregion Set conditions

        #region actionQueue Manipulators

        public void AddRequest(Request request)
        {
            this.actionQueue.Add(request);
        }

        public Request GetRequest()
        {
            Request output;

            if (this.actionQueue.Count != 0)
            {
                output = this.actionQueue[0];
                this.actionQueue.RemoveAt(0);
            }
            else
            {
                output = Request.Nothing;
            }

            return output;
        }

        public void ResetRequest()
        {
            this.actionQueue.Clear();
        }

        #endregion actionQueue Manipulators

        #region failureList Manipulators

        public bool AttemptRecovery(Request request)
        {
            bool nonFatalFailure;

            if (this.failureList.Contains(request))
            {
                this.failureList.Remove(request);
                nonFatalFailure = false;
            }
            else
            {
                // Add request into the failure list to prevent infinite loops
                this.failureList.Add(request);

                // Insert the same request on the top of the actionQueue to reattempt
                this.actionQueue.Insert(0, request);
                // If the current request is UpdateTreeViewSelection, then add UpdateTreeView to perform a complete refresh
                if (request == Request.UpdateTreeViewSelection)
                    this.actionQueue.Insert(0, Request.UpdateTreeView);
                nonFatalFailure = true;
            }

            return nonFatalFailure;
        }

        public void FailureListRemove(Request request)
        {
            if (this.failureList.Contains(request))
            {
                this.failureList.Remove(request);
            }
        }

        public void ResetFailureList()
        {
            this.failureList.Clear();
        }

        #endregion failureList Manipulators

        public Request ProcessRequest(Request request)
        {
            List<ElementId> elementIds;
            List<ElementId> currSelected;

            switch (request)
            {
                case Request.UpdateTreeView:

                    // Update / Refresh the revitController's view and get elements from that newly refreshed view
                    revitController.UpdateView();
                    // TODO: Make it so that revitController only gives filtered results (selection, view, project)
                    elementIds = revitController.GetAllElementIds(this.actionCondition.filter);
                    // If dataController failed to update all elements, then attempt a recovery and switch the request to nothing
                    if (!dataController.UpdateAllElements(elementIds))
                    {
                        this.AttemptRecovery(request);
                        request = Request.Nothing;
                    }

                    break;
                case Request.SelectElementIds:

                    // Get selected elementIds from the Modeless form
                    elementIds = selectionController.GetSelectedElementIds();
                    if (elementIds != null)
                    {
                        // If successful, then set dataController with elementIds and
                        // remove request from the failure list if it exists.
                        dataController.SelElements = elementIds;
                        this.FailureListRemove(request);
                    }
                    else
                    {
                        // If failed, then attempt recovery
                        this.AttemptRecovery(request);
                        request = Request.Nothing;
                    }

                    break;
                case Request.UpdateTreeViewSelection:

                    currSelected = revitController.GetElementIdsFromSelection();
                    if (currSelected != null)
                    {
                        // If successful, then set dataController with currentSelected to list and
                        // remove request from the failure list if it exists.
                        dataController.SelElements = currSelected;
                        this.FailureListRemove(request);
                    }
                    else
                    {
                        // If failed, then attempt recovery
                        this.AttemptRecovery(request);
                        request = Request.Nothing;
                    }

                    break;
                case Request.Nothing:

                    // When the given Request holds no significance, try to perform polling and
                    // check if the Revit application changed states and values to update Modeless Form
                    elementIds = selectionController.GetSelectedElementIds();
                    if (elementIds == null)
                        break;

                    currSelected = revitController.GetElementIdsFromSelection();
                    if (currSelected != null)
                    {
                        if (!selectionController.IsListEqual(elementIds, currSelected))
                        {
                            request = Request.UpdateTreeViewSelection;
                        }
                    }

                    break;
                case Request.Invalid:
                    request = Request.Nothing;
                    break;
                default:
                    request = Request.Nothing;
                    break;
            }

            return request;
        }

        /*
        public void HandleAll()
        {
            
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
                                TaskDialog.Show("Debug - selectionController.GetSelectedElementIds()",
                                    "Fatal Error: Cannot retrieve selected elementIds on retry due to the leaf nodes being changed");
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
                                TaskDialog.Show("Debug - SelectionChanged_UIAppEvent_WhileIdling(...)",
                                    "Fatal Error: selectionController failed to update the selected leaves from the Revit application");
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
                    TaskDialog.Show("Debug - SelectionChanged_UIAppEvent_WhileIdling(...)",
                        "Warning: Handler given invalid request");

                    break;
            }

            #endregion STOW
            
        }
        */

    }
}
