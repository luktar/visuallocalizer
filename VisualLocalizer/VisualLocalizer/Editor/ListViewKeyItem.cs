﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Resources;
using System.Drawing;
using VisualLocalizer.Library;
using EnvDTE;
using VisualLocalizer.Components;

namespace VisualLocalizer.Editor {
    internal class ListViewKeyItem : ListViewItem, IReferencableKeyValueSource {

        protected Color ErrorColor = Color.FromArgb(255, 213, 213);

        public ListViewKeyItem(AbstractListView parent) {
            _ItemsWithSameKey = new List<IKeyValueSource>();
            _ConflictRows = new HashSet<IKeyValueSource>();
            _ErrorSet = new HashSet<string>();
            FileRefOk = true;
            CodeReferences = new List<CodeReferenceResultItem>();
            this.AbstractListView = parent;
        }

        public ResXDataNode DataNode { get; set; }
        public string BeforeEditValue { get; set; }
        public string AfterEditValue { get; set; }
        public AbstractListView AbstractListView { get; private set; }

        private bool _FileRefOk;
        public bool FileRefOk {
            get {
                return _FileRefOk;
            }
            set {
                _FileRefOk = value;
                UpdateErrorSetDisplay();
            }
        }       

        public string Key {
            get {
                return this.Text;
            }
        }

        public string Value {
            get { return null; }
        }

        public int IndexAtDeleteTime { get; set; }
        public REMOVEKIND RemoveKind { get; set; }
        public ProjectItems NeighborItems { get; set; }

        private List<IKeyValueSource> _ItemsWithSameKey;
        public List<IKeyValueSource> ItemsWithSameKey {
            get {
                return _ItemsWithSameKey;
            }
            set {
                _ItemsWithSameKey = value;
            }
        }

        private HashSet<IKeyValueSource> _ConflictRows;
        public HashSet<IKeyValueSource> ConflictItems {
            get {
                return _ConflictRows;
            }
        }

        private HashSet<string> _ErrorSet;
        public HashSet<string> ErrorMessages {
            get {
                return _ErrorSet;
            }
        }

        public void UpdateErrorSetDisplay() {
            if (!FileRefOk) {
                this.BackColor = ErrorColor;
                this.ToolTipText = string.Format("Referenced file \"{0}\" does not exist", DataNode.FileRef != null ? DataNode.FileRef.FileName : "(null)");
            } else {
                if (ErrorMessages.Count > 0) {
                    this.ToolTipText = ErrorMessages.First();
                    this.BackColor = ErrorColor;
                } else {
                    this.ToolTipText = null;
                    this.BackColor = Color.White;
                }
            }          
        }

        public List<CodeReferenceResultItem> CodeReferences {
            get;
            set;
        }

        public void UpdateReferenceCount(bool determinated) {
            ListView.Invoke(new Action<string>((s) => SubItems["References"].Text = s), 
                ErrorMessages.Count == 0 && determinated ? CodeReferences.Count.ToString():"?");            
        }
    }
}
