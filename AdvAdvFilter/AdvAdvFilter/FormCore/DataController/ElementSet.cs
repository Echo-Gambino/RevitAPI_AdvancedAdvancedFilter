namespace AdvAdvFilter
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Autodesk.Revit.DB;

    public class ElementSet
    {
        private Dictionary<string, ElementSet> branch;
        private HashSet<ElementId> set;

        public HashSet<ElementId> Set
        {
            get { return this.set; }
            set { this.set = value; }
        }

        public Dictionary<string, ElementSet> Branch
        {
            get { return this.branch; }
        }

        public ElementSet()
        {
            branch = new Dictionary<string, ElementSet>();
            set = new HashSet<ElementId>();
        }

        /// <summary>
        /// Get a specific element set from branch
        /// </summary>
        public ElementSet GetElementSet(string key)
        {
            if (!branch.ContainsKey(key))
                return null;
            return branch[key];
        }

        public void AppendList(List<ElementId> list)
        {
            foreach (ElementId id in list)
                this.set.Add(id);
        }

        public void RemoveList(List<ElementId> list)
        {
            foreach (ElementId id in list)
                this.set.Remove(id);
        }

        /// <summary>
        /// Add a 'branch' onto the dictionary
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public ElementSet AddBranch(string key)
        {
            if (key == null)
                throw new ArgumentNullException();
            else if (branch.ContainsKey(key))
                throw new ArgumentException();

            ElementSet newBranch = new ElementSet();

            branch.Add(key, newBranch);

            return newBranch;
        }

        public bool RemoveBranch(string key)
        {
            if (key == null)
                throw new ArgumentNullException();
            else if (!branch.ContainsKey(key))
                return false;

            return branch.Remove(key);
        }

        /// <summary>
        /// Recursively get a union set of their children
        /// </summary>
        public void RecursiveUpdateCache()
        {
            if (branch.Count == 0) return;

            HashSet<ElementId> tmpSet = new HashSet<ElementId>();

            foreach (KeyValuePair<string, ElementSet> kvp in branch)
            {
                kvp.Value.RecursiveUpdateCache();
                tmpSet.UnionWith(kvp.Value.Set);
            }

            this.set = tmpSet;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            StringBuilder sb0 = new StringBuilder();

            foreach (ElementId id in this.Set)
                sb0.AppendFormat("{0} ", id.ToString());

            sb.AppendFormat("> {0}\n", sb0.ToString());

            if (this.branch.Count != 0)
            {
                foreach (KeyValuePair<string, ElementSet> kvp in this.branch)
                {
                    sb.AppendFormat("{0} | {1} \n", kvp.Key, kvp.Value.ToString());
                }
            }

            return sb.ToString();
        }

    }

}
