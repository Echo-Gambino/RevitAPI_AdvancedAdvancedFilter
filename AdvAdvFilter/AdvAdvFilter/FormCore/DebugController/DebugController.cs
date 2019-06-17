namespace AdvAdvFilter
{

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using System.Windows.Forms;

    public class DebugController
    {
        Panel panel;
        ListBox label1;
        ListBox label2;

        public DebugController(Panel p, ListBox l1, ListBox l2)
        {
            this.panel = p;
            this.label1 = l1;
            this.label2 = l2;
        }

        public void printText<T>(IEnumerable<T> listlike, string title, int index)
        {
            ListBox label;

            switch (index)
            {
                case 1:
                    label = this.label1;
                    break;
                case 2:
                    label = this.label2;
                    break;
                default:
                    label = null;
                    break;
            }

            ListBox.ObjectCollection oc = label.Items;
            oc.Clear();

            if (label != null)
            {
                oc.Add(String.Format("{0}: \n", title));
                foreach (T item in listlike)
                {
                    oc.Add(String.Format("> {0} \n", item.ToString()));
                }
            }

            return;
        }



    }
}
