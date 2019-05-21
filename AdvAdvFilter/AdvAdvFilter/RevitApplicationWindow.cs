namespace AdvAdvFilter
{
    using System;
    using System.Windows.Forms;

    /// <summary>
    /// A minimal implementation of <see cref="IWin32Window"/> containing the handle for Revit's application window.
    /// Used to establish Revit's window as the parent of any <see cref="Form"/>s, <see cref="MessageBox"/>es, etc.
    /// </summary>
    public class RevitApplicationWindow : IWin32Window
    {
        /// <summary>
        /// The handle for Revit's application window.
        /// </summary>
        public IntPtr Handle
        {
            get
            {
                return Autodesk.Windows.ComponentManager.ApplicationWindow;
            }
        }
    }
}