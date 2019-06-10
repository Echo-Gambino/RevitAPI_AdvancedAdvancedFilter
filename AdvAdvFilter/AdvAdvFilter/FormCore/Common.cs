namespace AdvAdvFilter
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class Common
    {
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
            Custom = 3,
            Invalid = -1
        }

        public enum Depth
        {
            CategoryType = 0,
            Category = 1,
            Family = 2,
            ElementType = 3,
            Instance = 4,
            Invalid = -1
        };

    }
}
