namespace AdvAdvFilter
{
    using Autodesk.Revit.UI;
    using System;
    using System.Text;

    class ErrorReport
    {
        public static void Report(Exception ex)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Error Type: \n\t" + ex.GetType().ToString());
            sb.Append("\nMessage: \n\t" + ex.Message);
            sb.Append("\nException To String: \n\t" + ex.ToString());

            TaskDialog.Show("Error!", sb.ToString());
        }
    }
}
