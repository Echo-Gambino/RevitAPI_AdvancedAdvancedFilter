namespace AdvAdvFilter
{
    using System;
    using System.Drawing;
    using System.Text;
    using System.Threading;
    using System.Windows.Forms;

    using Autodesk.Revit.UI;

    public class EventHandler : IExternalEventHandler
    {
        #region Fields

        private ExternalEvent externalEvent;
        private Tuple<Action<UIApplication, object>, object> actionToExecute;
        private ManualResetEvent resetEvent;
        private bool executing;

        #endregion

        #region Properties

        /// <summary>
        /// The <see cref="Autodesk.Revit.UI.ExternalEvent"/> of the TestApplication.
        /// </summary>
        public ExternalEvent ExternalEvent
        {
            get { return this.externalEvent; }
            set { this.externalEvent = value; }
        }

        /// <summary>
        /// An <see cref="Action"/> to execute with an <see cref="object"/> containing any necessary parameters.
        /// </summary>
        public Tuple<Action<UIApplication, object>, object> ActionToExecute
        {
            get { return this.actionToExecute; }
            set { this.actionToExecute = value; }
        }

        /// <summary>
        /// A ManualResetEvent to signal when the <see cref="ActionToExecute"/> has finished executing.
        /// </summary>
        public ManualResetEvent ResetEvent
        {
            get { return this.resetEvent; }
            set { this.resetEvent = value; }
        }

        /// <summary>
        /// A value indicating whether or not the event is currently executing.
        /// </summary>
        public bool Executing
        {
            get { return this.executing; }
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Sets an <see cref="Action"/> to execute with API context and raises the <see cref="Autodesk.Revit.UI.ExternalEvent"/>.
        /// </summary>
        /// <param name="actionToExecute">
        ///     The delegate for the <see cref="Action"/> to execute.
        ///     Must be of the form 'void Function(<see cref="Autodesk.Revit.UI.UIApplication"/> uiApp, <see cref="object"/> args)'.</param>
        /// <param name="resetEvent">
        ///     A ManualResetEvent to signal when the <see cref="ActionToExecute"/> has finished executing.</param>
        /// <param name="args">
        ///     Any arguments the <see cref="Action"/> requires.
        ///     Cast must be resolved by the receiving <see cref="Action"/>.</param>
        public void SetActionAndRaise(Action<UIApplication, object> actionToExecute, ManualResetEvent resetEvent, object args = null)
        {
            this.ActionToExecute = new Tuple<Action<UIApplication, object>, object>(actionToExecute, args);
            this.ResetEvent = resetEvent;

            // Revit application window must be in focus for event to raise properly.
            SetForegroundWindow(Autodesk.Windows.ComponentManager.ApplicationWindow);

            this.ExternalEvent.Raise();

            // Try to force immediate execution of event by "jiggling" the mouse.
            // Adapted from Jo Ye, ACE DevBlog.
            // http://adndevblog.typepad.com/aec/2013/07/tricks-to-force-trigger-idling-event.html
            Cursor.Position = new Point(Cursor.Position.X + 1, Cursor.Position.Y + 1);
            Cursor.Position = new Point(Cursor.Position.X - 1, Cursor.Position.Y - 1);
        }

        /// <summary>
        /// Executes some time after the <see cref="Autodesk.Revit.UI.ExternalEvent"/> has been raised.
        /// We can't control its execution, or be guaranteed it will execute promptly,
        /// but we can try to make it happen quickly with a few tricks.
        /// e.g., set focused window to Revit application window and "jiggle" the mouse,
        ///     switch between Revit application window and active Framing window.
        /// </summary>
        /// <param name="uiApp">The active <see cref="Autodesk.Revit.UI.UIApplication"/>.</param>
        public void Execute(UIApplication uiApp)
        {
            try
            {
                if (this.actionToExecute != null)
                {
                    this.executing = true;

                    // Execute method.
                    this.actionToExecute.Item1.Invoke(uiApp, this.actionToExecute.Item2);
                }
            }
            catch (Exception ex)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("Error Type: \n\t" + ex.GetType().ToString());
                sb.Append("\nMessage: \n\t" + ex.Message);
                sb.Append("\nException To String: \n\t" + ex.ToString());

                TaskDialog.Show("Error!", sb.ToString());
            }
            finally
            {
                this.actionToExecute = null;
                this.executing = false;

                // Set reset event to form may resume regular operation.
                this.resetEvent.Set();
            }
        }

        /// <summary>
        /// Required by <see cref="Autodesk.Revit.UI.IExternalEventHandler"/>.
        /// </summary>
        /// <returns>The string "EventHandler".</returns>
        public string GetName()
        {
            return "EventHandler";
        }

        #endregion

        #region External Methods

        // Revit application window must be focused window for event to execute.
        // Use external Windows user32 method to set foreground window.
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr handle);

        #endregion
    }
}
