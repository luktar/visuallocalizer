﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.OLE.Interop;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.VisualStudio;

namespace VisualLocalizer.Library {

    /// <summary>
    /// Provides easy-to-implement wrapper for undo stack unit
    /// </summary>
    public abstract class AbstractUndoUnit : IOleUndoUnit {

        protected bool isUndo;
        protected static int globalid = 1;
        protected int id;
        
        public AbstractUndoUnit() {
            id = globalid;
            globalid++;
            isUndo = true;
            PrependUnits = new List<IOleUndoUnit>();
            AppendUnits = new List<IOleUndoUnit>();
        }

        /// <summary>
        /// Called when user selects Undo action
        /// </summary>
        public abstract void Undo();

        /// <summary>
        /// Called when user selects Redo action
        /// </summary>
        public abstract void Redo();

        /// <summary>
        /// Should return text that appears in the undo list
        /// </summary>        
        public abstract string GetUndoDescription();

        /// <summary>
        /// Should return text that appears in the redo list
        /// </summary>        
        public abstract string GetRedoDescription();
      
        /// <summary>
        /// IOleUndoUnit member - called when user selects to undo/redo an action
        /// </summary>        
        public void Do(IOleUndoManager pUndoManager) {
            // do all PrependUnits
            List<IOleUndoUnit> prepunits = handleUnits(PrependUnits, pUndoManager);

            if (isUndo) {
                Undo();
            } else {
                Redo();
            }

            // do all AppendUnits
            List<IOleUndoUnit> appunits = handleUnits(AppendUnits, pUndoManager);

            this.AppendUnits.Clear();
            this.PrependUnits.Clear();

            this.AppendUnits.AddRange(appunits);
            this.PrependUnits.AddRange(prepunits);

            // add this to the opposite stack
            pUndoManager.Add(this);

            // undo unit becomes redo unit
            isUndo = !isUndo;
        }

        /// <summary>
        /// Performs given units and removes them from the stack
        /// </summary>        
        private List<IOleUndoUnit> handleUnits(List<IOleUndoUnit> units, IOleUndoManager pUndoManager) {
            foreach (IOleUndoUnit unit in units)
                unit.Do(pUndoManager);
            
            if (isUndo) {
                return pUndoManager.RemoveTopFromRedoStack(units.Count);
            } else {
                return pUndoManager.RemoveTopFromUndoStack(units.Count);
            }
        }

        /// <summary>
        /// IOleUndoUnit member
        /// </summary>
        public void GetDescription(out string pBstr) {
            if (isUndo)
                pBstr = GetUndoDescription();
            else
                pBstr = GetRedoDescription();
        }

        /// <summary>
        /// IOleUndoUnit member
        /// </summary>
        public void GetUnitType(out Guid pClsid, out int plID) {
            pClsid = GetType().GUID;
            plID = id;
        }

        /// <summary>
        /// IOleUndoUnit member
        /// </summary>
        public void OnNextAdd() {            
        }

        /// <summary>
        /// Units to perform before this item
        /// </summary>
        public List<IOleUndoUnit> PrependUnits {
            get;
            private set;
        }

        /// <summary>
        /// Units to perform after this item
        /// </summary>
        public List<IOleUndoUnit> AppendUnits {
            get;
            private set;
        }

    }
}
