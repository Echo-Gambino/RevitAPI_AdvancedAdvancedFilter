namespace AdvAdvFilter
{
    
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    public abstract class Controller
    {
        #region Fields

        private Panel controllerPanel;

        #endregion Fields

        #region Parameters

        public Panel ControllerPanel
        {
            get { return this.controllerPanel; }
        }

        #endregion Parameters

        public Controller(Panel panel)
        {
            // Throws a null argument exception if (panel == null)
            this.controllerPanel = panel ?? throw new ArgumentNullException();
        }

        #region Public Methods

        public abstract void Enable();

        public abstract void Disable();

        #endregion Public Methods


    }
}
