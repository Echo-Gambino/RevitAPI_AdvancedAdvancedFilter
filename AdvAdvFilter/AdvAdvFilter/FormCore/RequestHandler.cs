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
            ShiftSelected = 4,
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
        ActionController actionController;

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
            ElementSelectionController selectionController,
            ActionController actionController
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
            this.actionController = actionController;
        }

        public void ResetAll()
        {
            ResetRequest();
            ResetFailureList();
        }

        #region Set conditions

        public void HideUnselected(bool notUpdate = false)
        {
            this.actionCondition.hideUnselected = true;
            if (notUpdate) return;
            this.actionQueue.Add(Request.SelectElementIds);
        }

        public void ShowAll(bool notUpdate = false)
        {
            this.actionCondition.hideUnselected = false;
            if (notUpdate) return;
            this.actionQueue.Add(Request.SelectElementIds);
        }

        public void FilterBySelection(bool notUpdate = false)
        {
            this.actionCondition.filter = FilterMode.Selection;
            if (notUpdate) return;
            this.actionQueue.Add(Request.UpdateTreeView);
        }

        public void FilterByView(bool notUpdate = false)
        {
            this.actionCondition.filter = FilterMode.View;
            if (notUpdate) return;
            this.actionQueue.Add(Request.UpdateTreeView);
        }

        public void FilterByProject(bool notUpdate = false)
        {
            this.actionCondition.filter = FilterMode.Project;
            if (notUpdate) return;
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
                case Request.ShiftSelected:

                    currSelected = revitController.GetElementIdsFromSelection();
                    if (currSelected != null)
                    {
                        this.FailureListRemove(request);
                    }
                    else
                    {
                        // If failed, then attempt recovery
                        this.AttemptRecovery(request);
                        request = Request.Nothing;
                        break;
                    }

                    List<ElementId> movableElementIds;
                    List<int> coords;
                    bool copyAndShift;
                    bool success;

                    movableElementIds = revitController.GetMovableElementIds(currSelected);

                    if (movableElementIds.Count != 0)
                    {
                        success = actionController.TryGetAllInputs(out coords, out copyAndShift);
                        if (success)
                        {                            
                            actionController.DisablePrompt();
                            dataController.MovElements = movableElementIds;
                            dataController.Coords = coords;
                            dataController.CopyAndShift = copyAndShift;
                            request = Request.ShiftSelected;
                        }
                        else
                        {
                            actionController.PromptNotValidShifts();
                            request = Request.Nothing;
                        }
                    }
                    else
                    {
                        actionController.PromptNotValidElements();
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

    }
}
