namespace AdvAdvFilter
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;
    using Autodesk.Revit.UI.Events;

    using MessageBox = System.Windows.Forms.MessageBox;

    /// <summary>
    /// RevitController performs any complex tasks regarding the 
    ///     Revit API in the execution of the ModelessForm.
    /// </summary>
    class RevitController
    {
        #region Fields

        // Essential fields, often used and should never be null
        private ExternalCommandData commandData;
        private UIApplication uiApp;
        private UIDocument uiDoc;
        private Document doc;

        // Specialized fields, used on specific methods
        private View view;

        #endregion Fields

        #region Parameters

        public ExternalCommandData CommandData
        {
            get { return this.commandData; }
        }

        public UIApplication UiApp
        {
            get { return this.uiApp; }
        }

        public UIDocument UiDoc
        {
            get { return this.uiDoc; }
        }

        public Document Doc
        {
            get { return this.doc; }
        }

        public View View
        {
            get { return this.view; }
        }

        #endregion Parameters

        #region Public Methods

        public RevitController(ExternalCommandData commandData)
        {
            this.commandData = commandData;
            this.uiApp = commandData.Application;
            this.uiDoc = this.uiApp.ActiveUIDocument;
            this.doc = this.uiDoc.Document;

            this.view = null;
        }

        #region View Related Tasks

        /// <summary>
        /// Updates View 'this.view' with a new view that is of
        /// ViewType 'type' from the Document 'this.doc'
        /// unless special settings are enforced.
        /// </summary>
        /// <param name="type">If newView.ViewType is equal to type, then result is Valid and vice versa</param>
        /// <param name="force">If true, sets this.view to the latest view of this.doc.ActiveView whether or not its valid</param>
        /// <returns>Returns true if the result is valid and vice versa</returns>
        public bool UpdateView(
            ViewType type = ViewType.FloorPlan,
            bool force = false
            )
        {
            // Get doc.ActiveView
            View newView = this.doc.ActiveView;
            // Start optimistic
            bool resultValid = true;

            // Check if the result given is valid or not
            if (newView == null)
                resultValid = false;
            else if (newView.ViewType != type)
                resultValid = false;

            // If its forced or the result is valid, then set newView to this.View
            if (force || resultValid)
                this.view = newView;

            return resultValid;
        }

        /// <summary>
        /// Get all element ids from this.view, returns null if unsuccessful
        /// </summary>
        /// <returns>All elements within the view</returns>
        public List<ElementId> GetElementsFromView()
        {
            List<ElementId> output = null;

            if (this.view == null)
                return output;

            try
            {
                FilteredElementCollector collector = new FilteredElementCollector(this.doc, this.View.Id);
                output = collector.ToElementIds().ToList<ElementId>();
            }
            catch (ArgumentNullException ex)
            {
                string extraText = "Could be due to 'new FilteredElementCollector(...)'";
                MessageBox.Show(
                    string.Format("Message:\n {0}\n{1}", ex.Message, extraText),
                    "Error: " + ex.ToString());
            }
            catch (ArgumentException ex)
            {
                string extraText = "Could be due to 'new FilteredElementCollector(...)'";
                MessageBox.Show(
                    string.Format("Message:\n {0}\n{1}", ex.Message, extraText),
                    "Error: " + ex.ToString());
            }
            catch (InvalidOperationException ex)
            {
                string extraText = "Could be due to 'collector.ToElements()'";
                MessageBox.Show(
                    string.Format("Message:\n {0}\n{1}", ex.Message, extraText),
                    "Error: " + ex.ToString());
            }
            catch (Exception ex)
            {
                string extraText = "Unknown";
                MessageBox.Show(
                    string.Format("Message:\n {0}\n{1}", ex.Message, extraText),
                    "Error: " + ex.ToString());
            }

            return output;
        }

        #endregion View Related Tasks

        #region Selection Related Tasks

        public void MakeNewSelection(List<ElementId> selection)
        {
            this.uiDoc.Selection.SetElementIds(selection);
        }

        public void IsSelectionChanged(List<ElementId> selection)
        {
            ICollection<ElementId> elementIds = this.uiDoc.Selection.GetElementIds();

            elementIds.Equals(selection);

        }

        public ICollection<ElementId> GetSelectedElementIds()
        {
            return this.uiDoc.Selection.GetElementIds();
        }

        public void HideUnselectedElementIds(List<ElementId> selection)
        {
            ICollection<ElementId> ids = new FilteredElementCollector(this.doc).OfClass(typeof(FamilyInstance)).ToElementIds();

            List<ElementId> hideIds = new List<ElementId>();
            foreach (var id in ids)
            {
                if (!selection.Contains(id))
                {
                    hideIds.Add(id);
                }
            }

            using (var tran = new Transaction(doc, "Test"))
            {
                tran.Start();

                View view = this.uiDoc.ActiveView;
                if (view != null)
                {
                    view.HideElements(hideIds);
                }

                tran.Commit();
            }

        }

            #endregion Selection Related Tasks

        #region Get ElementId Grouping

        public SortedDictionary<string, List<ElementId>> GroupElementIdsBy(Type type, List<ElementId> elementIds)
        {
            string GetKey(Type t, ElementId eId)
            {
                // Based on the type and the nullity of the object recieved, return an output
                string tmpKey = "null";

                // If Type t isn't null, then test t against the given types and nullity
                // to set the value of tmpKey based on those results
                if (t != null)
                {
                    if (t == "CategoryType".GetType())
                    {
                        tmpKey = this.GetCategoryType(this.GetCategory(this.GetElement(eId))).ToString();
                    }
                    else if (t == typeof(Category))
                    {
                        Category cat = this.GetCategory(this.GetElement(eId));
                        if (cat != null)
                            tmpKey = cat.Name;
                        else
                            tmpKey = "No Category";
                    }
                    else if (t == typeof(Family))
                    {
                        tmpKey = this.GetFamilyName(this.GetElementType(this.GetElement(eId)));
                        if (tmpKey == "")
                            tmpKey = "No Family";
                    }
                    else if (t == typeof(ElementType))
                    {
                        ElementType eType = this.GetElementType(this.GetElement(eId));
                        if (eType != null)
                            tmpKey = eType.Name;
                        else
                            tmpKey = "No Type";
                    }
                    else if (t == typeof(ElementId))
                    {
                        Element element = this.GetElement(eId);
                        if (element != null)
                            tmpKey = element.UniqueId;
                        else
                            tmpKey = "None";
                    }
                    else
                    {
                        tmpKey = "null";
                    }
                }

                return tmpKey;
            }

            SortedDictionary<string, List<ElementId>> output = new SortedDictionary<string, List<ElementId>>();
            string key = string.Empty;

            foreach(ElementId eId in elementIds)
            {
                // get an object based on type
                key = GetKey(type, eId);

                // If the output doesn't have a category for elementCatType, make a new one
                if (!output.ContainsKey(key))
                    output.Add(key, new List<ElementId>());

                // Add eId to the elementCatType
                output[key].Add(eId);
            }

            return output;
        }

        public Dictionary<string, List<ElementId>> GroupElementIdsByCategoryType(List<ElementId> elementIds)
        {
            Dictionary<string, List<ElementId>> output = new Dictionary<string, List<ElementId>>();
            CategoryType elementCatType = CategoryType.Invalid;
            string key = null;

            foreach (ElementId eId in elementIds)
            {
                // Get element Category Type
                elementCatType = this.GetCategoryType(this.GetCategory(this.GetElement(eId)));

                // Turn the object to a string key
                key = elementCatType.ToString();

                // If the output doesn't have a category for elementCatType, make a new one
                if (!output.ContainsKey(key))
                    output.Add(key, new List<ElementId>());

                // Add eId to the elementCatType
                output[key].Add(eId);
            }

            return output;
        }

        public Dictionary<string, List<ElementId>> GroupElementIdsByCategories(List<ElementId> elementIds)
        {
            Dictionary<string, List<ElementId>> output = new Dictionary<string, List<ElementId>>();
            Category elementCat = null;
            string key = null;

            foreach (ElementId eId in elementIds)
            {
                // Get element Category Type
                elementCat = this.GetCategory(this.GetElement(eId));

                // Turn the object into a string key
                if (elementCat == null)
                    key = "No Category";
                else
                    key = elementCat.ToString();

                // If the output doesn't have a category for elementCatType, make a new one
                if (!output.ContainsKey(key))
                    output.Add(key, new List<ElementId>());

                // Add eId to the elementCatType
                output[key].Add(eId);
            }

            return output;
        }

        // public Dictionary<string, List<ElementId>> 

        public List<CategoryType> GetAllCategoryTypes()
        {
            List<CategoryType> output = new List<CategoryType>();
            Categories categories = this.doc.Settings.Categories;

            // Add in Invalid just in case
            output.Add(CategoryType.Invalid);
            // Construct a list of unique categoryType from categories
            foreach (Category c in categories)
            {
                if (output.Contains(c.CategoryType))
                    continue;
                output.Add(c.CategoryType);
            }

            return output;
        }


        #endregion Get ElementId Grouping

        #region Get Element Information

        public CategoryType GetCategoryType(Category category)
        {
            CategoryType categoryType = CategoryType.Invalid;

            if (category == null)
                return categoryType;

            categoryType = category.CategoryType;

            return categoryType;
        }

        public Category GetCategory(Element element)
        {
            Category category = null;

            if (element == null)
                return category;

            category = element.Category;

            return category;
        }

        public string GetFamilyName(ElementType elementType)
        {
            string familyName = "";

            if (elementType == null)
                return familyName;

            familyName = elementType.FamilyName;

            return familyName;
        }
        
        public ElementType GetElementType(Element element)
        {
            ElementType type = null;

            ElementId typeId = element.GetTypeId();

            if (typeId == null)
                return type;

            type = this.doc.GetElement(typeId) as ElementType;

            return type;
        }

        public Element GetElement(ElementId eId)
        {
            Element element = null;

            if (eId == null) return element;

            element = this.doc.GetElement(eId);

            return element;
        }

        #endregion Get ElementInformation

        #endregion Public Methods

    }
}
