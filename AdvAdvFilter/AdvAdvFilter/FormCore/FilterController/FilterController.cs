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
        #region Fields

        private RevitController revit;

        private Dictionary<Depth, HashSet<string>> fieldBlackList;

        private Dictionary<Depth, HashSet<string>> persistentBlacklist;

        private HashSet<ElementId> cachedBlackList;

        private HashSet<ElementId> cachedWhiteList;

        #endregion Fields

        #region Parameters

        public Dictionary<Depth, HashSet<string>> FieldBlackList
        {
            get { return this.fieldBlackList; }
        }

        public Dictionary<Depth, HashSet<string>> PersistentBlackList
        {
            get { return this.persistentBlacklist; }
        }

        public HashSet<ElementId> CachedBlackList
        {
            get { return this.cachedBlackList; }
        }

        public HashSet<ElementId> CachedWhiteList
        {
            get { return this.cachedWhiteList; }
        }

        #endregion Parameters

        #region Initializer

        public FilterController(RevitController revit)
        {
            // Store the given RevitController for future use
            this.revit = revit;

            // Set persistentBlackList by calling a method to get its innate blacklisted parameter names
            this.persistentBlacklist = this.GetPersistentBlackList();

            // Initialize fieldBlackList
            this.fieldBlackList = new Dictionary<Depth, HashSet<string>>();

            // Initialize cache
            this.cachedBlackList = new HashSet<ElementId>();
            this.cachedWhiteList = new HashSet<ElementId>();
        }

        private Dictionary<Depth, HashSet<string>> GetPersistentBlackList()
        {
            Dictionary<Depth, HashSet<string>> output = new Dictionary<Depth, HashSet<string>>();

            output.Add(Depth.CategoryType, new HashSet<string>()
            {
                "No CategoryType"
            });
            output.Add(Depth.Category, new HashSet<string>()
            {
                "Elevations",
                "Views",
                "Dimensions",
                "Cameras",
                "Sum Path",
                "<Sketch>",
                "Project Information",
                "Project Base Point",
                "Profiles",
                "No Category"
            });

            return output;
        }

        #endregion Initializer

        #region Manage BlackList Values

        /// <summary>
        /// Clears out all filters within the controller object
        /// (EXCEPT for persistent blacklist! Should ALWAYS be active)
        /// </summary>
        public void ClearAllFilters()
        {
            // Clear out fieldBlackList
            this.fieldBlackList.Clear();

            // Clear out cache
            this.cachedBlackList.Clear();
            this.cachedWhiteList.Clear();
        }
        
        /// <summary>
        /// Takes the given set of blacklisted parameters and uses it to update all applicable filters
        /// </summary>
        /// <param name="set"></param>
        /// <param name="depth"></param>
        public void SetBlackList(HashSet<string> set, Depth depth)
        {
            HashSet<string> oldSet;

            if (!this.fieldBlackList.ContainsKey(depth))
            {
                // If the fieldblacklist doesn't have an entry to {depth}, get an empty hashset as a substitute
                oldSet = new HashSet<string>();
            }
            else
            {
                // If the fieldblacklist has an entry to {depth}, get its hashset from the dictionary
                oldSet = this.fieldBlackList[depth];
            }

            // addSet = set - oldSet
            HashSet<string> addSet = new HashSet<string>(set.Except(oldSet));
            // remSet = oldSet - set
            HashSet<string> remSet = new HashSet<string>(oldSet.Except(set));

            // Adds and removes the parameter names from the blacklist
            AddToFieldBlackList(addSet, depth);
            RemoveFromFieldBlackList(remSet, depth);

            // TODO: Must develop more efficient removal of ElementIds from the cache
            if (addSet.Count != 0)
                this.cachedWhiteList.Clear();
            if (remSet.Count != 0)
                this.cachedBlackList.Clear();
        }

        #endregion Manage BlackList Values

        #region Manage Field BlackList Values

        /// <summary>
        /// Add the set of string to fieldBlackList in the corresponding entry to depth
        /// </summary>
        /// <param name="addset"></param>
        /// <param name="depth"></param>
        public void AddToFieldBlackList(HashSet<string> addset, Depth depth)
        {
            // If the set of parameter names to remove is empty, simply return
            if (addset.Count == 0) return;

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
            // If the set of parameter names to remove is empty, simply return
            if (remset.Count == 0) return;

            // If this.fieldBlackList doesn't already contain an entry of the blacklist, then return
            if (!this.fieldBlackList.ContainsKey(depth)) return;

            // Remove the elements from this.fieldBlackList[depth] that remset has
            this.fieldBlackList[depth].ExceptWith(remset);
            // this.fieldBlackList[depth].UnionWith(this.persistentBlacklist[depth]);

            // If the entry corresponding to depth is empty, then remove it completely
            if (this.fieldBlackList[depth].Count == 0) this.fieldBlackList.Remove(depth);
        }

        #endregion Manage Field BlackList Values

        #region Manage Cached BlackList Values

        /// <summary>
        /// Adds the given elementIds to the cached filter
        /// </summary>
        /// <param name="elementIds"></param>
        public void AddToCacheBlackList(IEnumerable<ElementId> elementIds)
        {
            this.cachedBlackList.UnionWith(elementIds);
        }

        /// <summary>
        /// Adds the given elementIds to the cached filter
        /// </summary>
        /// <param name="elementIds"></param>
        public void AddToCacheWhiteList(IEnumerable<ElementId> elementIds)
        {
            this.cachedWhiteList.UnionWith(elementIds);
        }

        #endregion Manage Cached BlackList Values

        #region General Filter

        public HashSet<ElementId> FilterS(HashSet<ElementId> input)
        {
            IEnumerable<NodeData> filteredData;

            HashSet<ElementId> beforeFilterIds;
            HashSet<ElementId> afterFilterIds;

            beforeFilterIds = new HashSet<ElementId>(input);

            HashSet<ElementId> idsPreBlackListed = FilterByCache(beforeFilterIds, this.cachedBlackList);
            HashSet<ElementId> idsPreWhiteListed = FilterByCache(beforeFilterIds, this.cachedWhiteList);

            beforeFilterIds = FilterOutInvalidIds(beforeFilterIds);

            filteredData = ConvertToNodeData(beforeFilterIds);

            filteredData = FilterByField(filteredData, this.fieldBlackList);
            filteredData = FilterByField(filteredData, this.persistentBlacklist);

            afterFilterIds = new HashSet<ElementId>(ConvertToElementId(filteredData));
            /*
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Filter Statistics");
            sb.AppendFormat("this.cachedBlackList: {0}\n", this.cachedBlackList.Count);
            sb.AppendFormat("this.cachedWhiteList: {0}\n", this.cachedWhiteList.Count);
            sb.AppendFormat("idsPreBlackListed: {0}\n", idsPreBlackListed.Count);
            sb.AppendFormat("idsPreWhiteListed: {0}\n", idsPreWhiteListed.Count);
            sb.AppendFormat("input: {0}\n", input.Count);
            sb.AppendFormat("beforeFilteredIds: {0}\n", beforeFilterIds.Count);
            sb.AppendFormat("afterFilteredIds: {0}\n", afterFilterIds.Count);
            System.Windows.Forms.MessageBox.Show(sb.ToString());
            */
            AddToCacheBlackList(beforeFilterIds.Except(afterFilterIds));
            AddToCacheWhiteList(afterFilterIds);

            afterFilterIds.UnionWith(idsPreWhiteListed);

            return afterFilterIds;

        }

        #endregion General Filter

        #region Filter Out InvalidIds

        /// <summary>
        /// Filters out Invalid ElementIds, like ids that cannot be converted to elements with revit's doc object
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public HashSet<ElementId> FilterOutInvalidIds(IEnumerable<ElementId> input)
        {
            IEnumerable<ElementId> validElementIds
                = from ElementId id in input
                  where (this.revit.Doc.GetElement(id) != null)
                  select id;

            return new HashSet<ElementId>(validElementIds);
        }

        #endregion Filter Out InvalidIds

        #region Filter By Cache

        public HashSet<ElementId> FilterByCache(
            HashSet<ElementId> filteredIds, 
            HashSet<ElementId> cachedList)
        {
            if (cachedList.Count == 0) { return new HashSet<ElementId>(); }

            HashSet<ElementId> idsToBeFiltered = new HashSet<ElementId>(filteredIds.Intersect(cachedList));

            filteredIds.ExceptWith(idsToBeFiltered);

            return idsToBeFiltered;
        }

        #endregion Filter By Cache

        #region Filter Out Field Parameters

        /// <summary>
        /// Filter out field parameters from the given input using the given dictionary of blacklisted field parameters
        /// </summary>
        /// <param name="input"></param>
        /// <param name="blackList"></param>
        /// <returns></returns>
        public IEnumerable<NodeData> FilterByField(IEnumerable<NodeData> input, Dictionary<Depth, HashSet<string>> blackList)
        {
            // Pre-operation checks
            if (input == null) throw new ArgumentNullException("input");
            if (blackList == null) throw new ArgumentNullException("blackList");

            // If the input is empty or the blacklist is empty,
            // then return immediately, as nothing significant is going to come from this.
            if ((input.Count() == 0) || (blackList.Count == 0))
            {
                return input;
            }

            HashSet<string> list;
            // Converts the enum into an iterable object
            foreach (Depth depth in (Depth[])Enum.GetValues(typeof(Depth)))
            {
                // Quick skip the iteration if the given conditions are not met
                if ((depth == Depth.Invalid) || (!blackList.ContainsKey(depth))) continue;
                
                // Get blackList to be its own variable to make the next statement slightly shorter
                list = blackList[depth];

                // For each node data in input, select all of those that DOES NOT
                // have the same field value as what is within the blacklist
                input = from NodeData data in input
                        where (!list.Contains(data.GetParameter(depth.ToString())))
                        select data;
            }

            return input;
        }

        #endregion Filter Out Field Parameters
        
        #region Conversion Methods

        /// <summary>
        /// Convert the given list of ElementIds into a list of NodeDat
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public IEnumerable<NodeData> ConvertToNodeData(IEnumerable<ElementId> input)
        {
            // Pre-operation checks
            if (input == null) throw new ArgumentNullException("input");

            Document doc = revit.Doc;

            // Get every elementId in the given input and convert each of them into node data via GenerateNodeData
            IEnumerable<NodeData> output
                = from ElementId id in input
                  select GenerateNodeData(id, doc);

            return output;
        }

        /// <summary>
        /// Converts the given list of NodeData into a list of ElementIds
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public IEnumerable<ElementId> ConvertToElementId(IEnumerable<NodeData> input)
        {
            // Pre-operation check
            if (input == null) throw new ArgumentNullException("input");

            // Get every NodeData in the list of NodeData (input), and extract their ElementIds via their Id field
            IEnumerable<ElementId> output
                = from NodeData data in input
                  select data.Id;

            return output;
        }

        #endregion Conversion Methods

        #region Auxiliary Methods

        /// <summary>
        /// Uses the given ElementId and Document object to create a NodeData object,
        /// which has values like ElementId, CategoryType, Category, Family, ElementType, etc.
        /// </summary>
        /// <param name="elementId"></param>
        /// <param name="doc"></param>
        /// <returns></returns>
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
