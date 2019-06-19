namespace AdvAdvFilter
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Autodesk.Revit.DB;

    using Depth = AdvAdvFilter.Common.Depth;

    public class FilterController
    {
        #region Field

        RevitController revit;

        Dictionary<Depth, HashSet<string>> fieldBlackList;

        #endregion Field

        public FilterController(RevitController revit)
        {
            this.revit = revit;

            this.fieldBlackList = new Dictionary<Depth, HashSet<string>>();
        }

        public void ClearAllFilters()
        {
            this.fieldBlackList.Clear();
        }

        public void SetFieldBlackList(HashSet<string> set, Depth depth)
        {
            HashSet<string> oldSet;

            if (!this.fieldBlackList.ContainsKey(depth))
                oldSet = new HashSet<string>();
            else
                oldSet = this.fieldBlackList[depth];

            HashSet<string> addSet = new HashSet<string>(set.Except(oldSet));
            HashSet<string> remSet = new HashSet<string>(oldSet.Except(set));

            AddToFieldBlackList(addSet, depth);
            RemoveFromFieldBlackList(remSet, depth);
        }

        /// <summary>
        /// Add the set of string to fieldBlackList in the corresponding entry to depth
        /// </summary>
        /// <param name="addset"></param>
        /// <param name="depth"></param>
        public void AddToFieldBlackList(HashSet<string> addset, Depth depth)
        {
            // If this.fieldBlackList doesn't already contain an entry of the blacklist,
            // add an entry with addset as its value
            if (!this.fieldBlackList.ContainsKey(depth))
            {
                this.fieldBlackList.Add(depth, addset);
                return;
            }

            // Add the elements from addset into fieldBlackList[depth]
            this.fieldBlackList[depth].UnionWith(addset);
        }

        /// <summary>
        /// Remove the set of strings from fieldBlackList in the corresponding entry to depth
        /// </summary>
        /// <param name="remset"></param>
        /// <param name="depth"></param>
        public void RemoveFromFieldBlackList(HashSet<string> remset, Depth depth)
        {
            // If this.fieldBlackList doesn't already contain an entry of the blacklist, then return
            if (!this.fieldBlackList.ContainsKey(depth)) return;

            // Remove the elements from this.fieldBlackList[depth] that remset has
            this.fieldBlackList[depth].ExceptWith(remset);

            // If the entry corresponding to depth is empty, then remove it completely
            if (this.fieldBlackList[depth].Count == 0) this.fieldBlackList.Remove(depth);
        }

        #region Filter ElementIds

        public HashSet<ElementId> Filter(HashSet<ElementId> input)
        {
            // Convert the list of ElementId into a list of NodeData
            IEnumerable<NodeData> inputData = ConvertToNodeData(input);

            inputData = FilterByField(inputData);

            IEnumerable<ElementId> outputData = ConvertToElementId(inputData);

            return new HashSet<ElementId>(outputData);
        }

        public IEnumerable<NodeData> FilterByField(IEnumerable<NodeData> input)
        {
            if (input == null) throw new ArgumentNullException("input");

            HashSet<string> blackList;

            // Converts the enum into an iterable object
            foreach (Depth depth in (Depth[])Enum.GetValues(typeof(Depth)))
            {
                if (depth == Depth.Invalid)
                    continue;
                else if (!this.fieldBlackList.ContainsKey(depth))
                    continue;

                // Get blackList to be its own variable to make the next statement slightly shorter
                blackList = this.fieldBlackList[depth];

                // For each node data in input, select all of those that DOES NOT
                // have the same field value as what is within the blacklist
                input = from NodeData data in input
                        where (!blackList.Contains(data.GetParameter(depth.ToString())))
                        select data;
            }

            return input;
        }

        #endregion Filter ElementIds

        #region Auxiliary Methods

        public IEnumerable<NodeData> ConvertToNodeData(IEnumerable<ElementId> input)
        {
            if (input == null) throw new ArgumentNullException("input");

            Document doc = revit.Doc;

            IEnumerable<NodeData> output
                = from ElementId id in input
                  where (doc.GetElement(id) != null)
                  select GenerateNodeData(id, doc);

            return output;
        }

        public IEnumerable<ElementId> ConvertToElementId(IEnumerable<NodeData> input)
        {
            if (input == null) throw new ArgumentNullException("input");

            IEnumerable<ElementId> output
                = from NodeData data in input
                  select data.Id;

            return output;
        }

        private NodeData GenerateNodeData(ElementId elementId, Document doc)
        {
            // Fail the execution if GenerateNodeData has elementId or doc as null
            if ((elementId == null) || (doc == null))
                throw new ArgumentNullException();

            // Get elementId's element
            Element element = doc.GetElement(elementId);

            // If resulting element is null, fail the process, as it should always return not null
            if (element == null) throw new InvalidOperationException("element is null!");

            // Generate NodeData
            NodeData data = new NodeData();

            // Set ElementId
            data.Id = elementId;

            // Set OwnerViewId
            data.OwnerViewId = element.OwnerViewId;

            // Set fields related to category
            Category category = element.Category;
            if (category != null)
            {
                data.CategoryType = category.CategoryType.ToString();
                data.Category = category.Name;
            }

            // Set fields related to elementType
            ElementId typeId = element.GetTypeId();
            if (typeId != null)
            {
                ElementType elementType = doc.GetElement(typeId) as ElementType;
                if (elementType != null)
                {
                    data.Family = elementType.FamilyName;
                    data.ElementType = elementType.Name;
                }
            }

            return data;
        }

        #endregion Auxiliary Methods

    }
}
