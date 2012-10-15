﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using VisualLocalizer.Library;
using System.Windows.Forms;
using System.Resources;

namespace VisualLocalizer.Library {
    public abstract class AbstractKeyValueGridView<ItemType>:AbstractCheckedGridView<ItemType> where ItemType:class {

        protected KeyValueConflictResolver ConflictResolver;

        public AbstractKeyValueGridView() : this(new KeyValueConflictResolver()) { }

        public AbstractKeyValueGridView(KeyValueConflictResolver resolver) {            
            this.ConflictResolver = resolver;            
        }        

        protected override void OnCellEndEdit(DataGridViewCellEventArgs e) {
            base.OnCellEndEdit(e);

            if (Columns[e.ColumnIndex].Name != KeyColumnName) {
                Rows[e.RowIndex].Cells[KeyColumnName].Tag = Rows[e.RowIndex].Cells[KeyColumnName].Value;
            }
            if (Columns[e.ColumnIndex].Name != CheckBoxColumnName) {
                Validate(e.RowIndex);
            }       
        }

        protected virtual void Validate(DataGridViewKeyValueRow<ItemType> row) {
            string key = row.Key;
            string value = row.Value;                                 

            string originalValue = (string)row.Cells[KeyColumnName].Tag;
            ConflictResolver.TryAdd(originalValue, key, row);
            if (originalValue == null) row.Cells[KeyColumnName].Tag = key;

            row.ErrorSetUpdate();
        }              

        protected void Validate(int rowIndex) {
            Validate(Rows[rowIndex] as DataGridViewKeyValueRow<ItemType>);
        }

        public override void SetData(List<ItemType> list) {
            ConflictResolver.Clear();
            errorRows.Clear();
        }

        public abstract string KeyColumnName { get; }
        public abstract string ValueColumnName { get; }
    }
}
