namespace AdvAdvFilter
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using FilterMode = AdvAdvFilter.Common.FilterMode;
    using Request = AdvAdvFilter.Common.Request;

    class RequestHandler
    {
        #region Data Types

        struct Condition
        {
            public bool hideUnselected;
            public FilterMode filter;
        };

        #endregion Data Types

        #region Fields

        // This actionQueue allows asynchronous execution of modeless form and the revit software
        // by making requests that persist even if other requests are posted before that reqeust gets resolved
        private List<Request> requestQueue;
        // ActionCondition is something that is must be present in almost every loop of API handler
        private Condition actionCondition;

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

        public RequestHandler()
        {
            // Initialize variables
            this.requestQueue = new List<Request>();
            this.actionCondition = new Condition();
        }

        // Main reset to reset all facets of requestHandler  
        public void ResetAll()
        {
            // Reset actionQueue
            ResetRequest();

            // Reset actionCondition
            this.actionCondition = new Condition();
        }

        #region Set conditions

        /// <summary>
        /// Set up the parameters needed to hide unselected nodes, and request to change element visibility
        /// </summary>
        /// <param name="urgent"></param>
        public void HideUnselected(bool urgent = false)
        {
            this.actionCondition.hideUnselected = true;

            PutInRequestQueue(Request.ChangeElementVisibility, urgent);
        }

        /// <summary>
        /// Set up the parameters needed to restore the visibility of all the nodes, and request to change element visibility
        /// </summary>
        /// <param name="urgent"></param>
        public void ShowAll(bool urgent = false)
        {
            this.actionCondition.hideUnselected = false;

            PutInRequestQueue(Request.ChangeElementVisibility, urgent);
        }

        /// <summary>
        /// Sets up the parameters needed to filter the TreeView by selection, and request to update the TreeView
        /// </summary>
        /// <param name="urgent"></param>
        public void FilterBySelection(bool urgent = false)
        {
            this.actionCondition.filter = FilterMode.Selection;

            PutInRequestQueue(Request.UpdateTreeView, urgent);
        }

        /// <summary>
        /// Sets up the parameters needed to filter the TreeView by view, and request to update the TreeView 
        /// </summary>
        /// <param name="urgent"></param>
        public void FilterByView(bool urgent = false)
        {
            this.actionCondition.filter = FilterMode.View;

            PutInRequestQueue(Request.UpdateTreeView, urgent);
        }

        /// <summary>
        /// Sets up the parameters needed to filter the TreeView by the project, and request to update the TreeView
        /// </summary>
        /// <param name="urgent"></param>
        public void FilterByProject(bool urgent = false)
        {
            this.actionCondition.filter = FilterMode.Project;

            PutInRequestQueue(Request.UpdateTreeView, urgent);
        }

        /// <summary>
        /// A simple way to put urgent / non-urgent requests into the request queue
        /// </summary>
        /// <param name="request"></param>
        /// <param name="urgent"></param>
        private void PutInRequestQueue(Request request, bool urgent)
        {
            if (urgent)
            {
                // If request is urgent, have it 'skip' the line and set it up to be served ASAP
                this.ImmediateRequest(request);
            }
            else
            {
                // If request isn't urgent, have it put on the end of the line and process it through like normal
                this.AddRequest(request);
            }
        }

        #endregion Set conditions

        #region actionQueue Manipulators

        /// <summary>
        /// Have the given request be put at the 'front' of the queue, where it will be served immediately
        /// </summary>
        /// <param name="request"></param>
        public void ImmediateRequest(Request request)
        {
            this.requestQueue.Insert(0, request);
        }

        /// <summary>
        /// Have the given request be put at the 'back' of the queue, where it will be served orderly
        /// </summary>
        /// <param name="request"></param>
        public void AddRequest(Request request)
        {
            this.requestQueue.Add(request);
        }

        /// <summary>
        /// Gets the requests of the actionQueue,
        /// if the actionQueue has requests, then it will 'pop' the request in 'front' out,
        /// if actionQueue doesn't have any elements, send out Request.Nothing
        /// </summary>
        /// <returns></returns>
        public Request GetRequest()
        {
            Request output;

            if (this.requestQueue.Count != 0)
            {
                // Pop the request at the front of the actionQueue out into output
                output = this.requestQueue[0];
                this.requestQueue.RemoveAt(0);
            }
            else
            {
                // Set output to Nothing
                output = Request.Nothing;
            }

            return output;
        }

        /// <summary>
        /// Clears out the actionQueue for initialization or possible debug purposes
        /// </summary>
        public void ResetRequest()
        {
            this.requestQueue.Clear();
        }

        #endregion actionQueue Manipulators
    }
}
