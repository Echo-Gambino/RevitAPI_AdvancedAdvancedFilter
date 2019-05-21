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

        private List<ElementId> selElements;
        private List<ElementId> allElements;

        private bool uidocNull;
        private bool docNull;

        #region Section Controllers

        ElementPickerInterface esController;

        #endregion

        #endregion

        public ModelessForm(
            ExternalCommandData commandData,
            UIControlledApplication uiCtrlApp,
            ExternalEvent externalEvent,
            EventHandler eventHandler,
            ElementProtectionUpdater elementProtectionUpdater,
            List<Element> elementList)
        {
            InitializeComponent();

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

            elementList = elementList.Where(e => null != e.Category && e.Category.HasMaterialQuantities).ToList();

            string txt = "";

            Autodesk.Revit.DB.View v0 = uiDoc.ActiveView;
            if (v0 == null)
                txt += "uiDoc.ActiveView is null";
            else
                txt += "uiDoc.ActiveView isn't null";

            txt += "\n";

            Autodesk.Revit.DB.View v1 = doc.ActiveView;
            if (v1 == null)
                txt += "doc.ActiveView is null";
            else
                txt += "doc.ActiveView isn't null";

            // Something about loading elements into TreeView
            StringBuilder sb = new StringBuilder();

            sb.Append(txt);
            sb.Append("\n" + elementList.Count.ToString());
            foreach (Element e in elementList)
            {
                sb.Append("\n+ " + e.Name);
            }

            TestLabel.Text = sb.ToString();

            this.FormClosing += this.ModelessForm_FormClosed;

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
            this.esController = new ElementPickerInterface(ElementSelectionPanel, ElementSelectionTreeView);
        }

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
                    // MessageBox.Show("SelectionCheckedUIAppEvent_WhileIdling", "debug");

                    UIApplication uiApp = sender as UIApplication;
                    UIDocument uiDoc = uiApp.ActiveUIDocument;
                    Document activeDoc = uiDoc.Document;

                    // This is to check any time something in the model may have changed or something was selected
                    ICollection<ElementId> selectedElementIds = uiDoc.Selection.GetElementIds();



                    if (selectedElementIds.Count >= 1)
                    {
                        this.BeginInvoke(new Action(() =>
                        {
                            string txt = "t";
                            TestLabel.Text = txt;

                            /*
                            List<Element> selectedElements = new List<Element>();
                            foreach (ElementId eId in selectedElementIds)
                                selectedElements.Add(activeDoc.GetElement(eId));

                            esController.SetElementsToTreeView(selectedElements, doc);
                            */
                            /*
                            string txt = "";
                            try
                            {
                                Autodesk.Revit.DB.View view = activeDoc.ActiveView;
                                if (view == null)
                                    txt = "view is null";
                                else
                                {
                                    // ElementId viewId = view.Id;
                                    // FilteredElementCollector test = new FilteredElementCollector(activeDoc, viewId);
                                    // ICollection<ElementId> allElements = test.ToElementIds();
                                }
                            }
                            catch (ArgumentNullException ex)
                            {
                                txt = "ArgumentNullException: " + ex.Message;
                            }
                            catch (ArgumentException ex)
                            {
                                txt = "ArgumentException: " + ex.Message;
                            }
                            catch (NullReferenceException ex)
                            {
                                txt = "NullReferenceException: " + ex.Message;
                            }
                            catch (Exception ex)
                            {
                                txt = "Other Exception: " + ex.Message;
                            }

                            // Update the form with the new selection
                            StringBuilder sb = new StringBuilder();

                            sb.Append(txt);

                            foreach (ElementId id in selectedElementIds)
                            {
                                Element e = activeDoc.GetElement(id);
                                sb.Append("\n+ " + e.Name);
                                
                                try
                                {
                                    ElementId eType = e.GetTypeId();
                                    ElementType type = doc.GetElement(eType) as ElementType;
                                    sb.Append(" + " + type.Name + " + " + type.FamilyName);
                                }
                                catch (Exception ex)
                                {
                                    sb.Append(" + " + ex.Message);
                                }
                                
                            }

                            TestLabel.Text = sb.ToString();
                            */
                        }));
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
