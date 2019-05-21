namespace AdvAdvFilter
{
    using System;
    using System.Collections.Generic;

    using Autodesk.Revit.DB;

    /// <summary>
    /// A class to prevent modification of certain Revit <see cref="Autodesk.Revit.DB.Element"/>s.
    /// </summary>
    /// <remarks>
    /// Adapted from Jeremy Tammik's DeletionUpdater, provided under the terms of the MIT License.
    ///     http://thebuildingcoder.typepad.com/blog/2011/11/lock-the-model-eg-prevent-deletion.html
    ///     http://opensource.org/licenses/MIT
    /// </remarks>
    public class ElementProtectionUpdater : IUpdater
    {
        #region Fields

        private static AddInId appId;
        private UpdaterId updaterId;
        private FailureDefinitionId failureId = null;
        private List<ElementId> protectedElementIds;

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes a new instance of the <see cref="ElementProtectionUpdater"/> class.
        /// </summary>
        /// <param name="addInId">The AddinId of the active addin.</param>
        public ElementProtectionUpdater(AddInId addInId)
        {
            ElementProtectionUpdater.appId = addInId;

            this.updaterId = new UpdaterId(appId, Guid.NewGuid());
            this.failureId = new FailureDefinitionId(Guid.NewGuid());
            this.protectedElementIds = new List<ElementId>();

            FailureDefinition failureDefinition
                = FailureDefinition.CreateFailureDefinition(
                                                            this.failureId,
                                                            FailureSeverity.Error,
                                                            "PreventModification: Panel elements cannot currently be modified.\n" +
                                                                "Please make all element modifications before running framing creation.");
        }

        /// <summary>
        /// The list of <see cref="Autodesk.Revit.DB.ElementId"/>s of currently protected <see cref="Autodesk.Revit.DB.Element"/>s.
        /// </summary>
        public List<ElementId> ProtectedElementIds
        {
            get { return this.protectedElementIds; }
            set { this.protectedElementIds = value; }
        }

        /// <summary>
        /// Executed when the <see cref="ElementProtectionUpdater"/> is fired.
        /// This happens when an <see cref="Autodesk.Revit.DB.Element"/> of a category in the UpdaterRegistry is modified.
        /// </summary>
        /// <param name="data">The <see cref="Autodesk.Revit.DB.UpdaterData"/>.</param>
        public void Execute(UpdaterData data)
        {
            Document doc = data.GetDocument();

            foreach (ElementId id in data.GetModifiedElementIds())
            {
                if (this.protectedElementIds.Contains(id))
                {
                    // Protected element modified. Post a failure to reverse changes.
                    FailureMessage failureMessage = new FailureMessage(this.failureId);

                    failureMessage.SetFailingElement(id);
                    doc.PostFailure(failureMessage);
                }
            }
        }

        /// <summary>
        /// Gets additional information on the <see cref="ElementProtectionUpdater"/>.
        /// Required by <see cref="Autodesk.Revit.DB.IUpdater"/>.
        /// </summary>
        /// <returns>A description of the <see cref="ElementProtectionUpdater"/>.</returns>
        public string GetAdditionalInformation()
        {
            return "Prevent modification of protected elements.";
        }

        /// <summary>
        /// Gets the updater's <see cref="Autodesk.Revit.DB.ChangePriority"/>.
        /// Required by <see cref="Autodesk.Revit.DB.IUpdater"/>.
        /// </summary>
        /// <returns>The <see cref="ElementProtectionUpdater"/>'s <see cref="Autodesk.Revit.DB.ChangePriority"/>.</returns>
        public ChangePriority GetChangePriority()
        {
            return ChangePriority.FloorsRoofsStructuralWalls;
        }

        /// <summary>
        /// Gets the Autodesk.Revit.DB.UpdaterId.
        /// Required by Autodesk.Revit.DB.IUpdater.
        /// </summary>
        /// <returns>The Autodesk.Revit.DB.UpdaterID.</returns>
        public UpdaterId GetUpdaterId()
        {
            return this.updaterId;
        }

        /// <summary>
        /// Gets the <see cref="ElementProtectionUpdater"/>'s name.
        /// Required by <see cref="Autodesk.Revit.DB.IUpdater"/>.
        /// </summary>
        /// <returns>The <see cref="ElementProtectionUpdater"/>'s name.</returns>
        public string GetUpdaterName()
        {
            return "ElementProtectionUpdater";
        }

        #endregion
    }
}
